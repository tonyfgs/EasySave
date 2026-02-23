using Application.DTOs;
using Application.Ports;
using Logger.Interface;
using Microsoft.Extensions.Logging;
using Shared;

namespace Infrastructure;

public class DynamicTransferLogger : ITransferLogger
{
    private readonly AppConfiguration _config;
    private readonly IEasyLogger _easyLogger;
    private readonly ILogCentralizationService? _centralizationService;
    private readonly ILogger<DynamicTransferLogger>? _logger;
    private readonly string _userId;

    public DynamicTransferLogger(
        AppConfiguration config,
        IEasyLogger easyLogger,
        string logDirectory,
        ILogCentralizationService? centralizationService = null,
        string? userId = null,
        ILogger<DynamicTransferLogger>? logger = null)
    {
        _config = config;
        _easyLogger = easyLogger;
        _centralizationService = centralizationService;
        _userId = userId ?? Environment.UserName;
        _logger = logger;
    }

    public void LogTransfer(TransferLog transfer)
    {
        try
        {
            var logMode = _centralizationService?.GetLogMode() ?? LogMode.LocalOnly;

            _logger?.LogDebug(
                "Logging transfer: {BackupName} - {SourcePath}, Mode: {Mode}",
                transfer.BackupName,
                transfer.SourcePath,
                logMode);

            // Write to local storage if mode includes local
            if (logMode == LogMode.LocalOnly || logMode == LogMode.LocalAndCentralized)
            {
                WriteLocalLog(transfer);
            }

            // Send to centralized server if mode includes centralized
            if (_centralizationService != null &&
                (logMode == LogMode.CentralizedOnly || logMode == LogMode.LocalAndCentralized))
            {
                // Await the async call with proper error handling
                SendToCentralizedServerAsync(transfer, logMode).GetAwaiter().GetResult();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during transfer logging: {Message}", ex.Message);
        }
    }

    private void WriteLocalLog(TransferLog transfer)
    {
        var currentFormat = _config.GetLogFormat();
        var logDir = _config.GetLogDirectory();

        _logger?.LogDebug("Writing local log - Format: {Format}, Directory: {Dir}", currentFormat, logDir);

        Directory.CreateDirectory(logDir);

        ITransferLogger logger = currentFormat switch
        {
            LogFormat.JSON => new JsonTransferLogger(logDir),
            LogFormat.XML => new XmlTransferLogger(logDir, _easyLogger),
            _ => throw new ArgumentOutOfRangeException(nameof(currentFormat))
        };

        logger.LogTransfer(transfer);

        var expectedFile = Path.Combine(
            logDir,
            $"{DateTime.UtcNow:yyyy-MM-dd}.{(currentFormat == LogFormat.JSON ? "json" : "xml")}");
        _logger?.LogDebug("Local log written to: {FilePath}", expectedFile);
    }

    private async Task SendToCentralizedServerAsync(TransferLog transfer, LogMode logMode)
    {
        try
        {
            await _centralizationService!.SendLogAsync(transfer, _userId);
            _logger?.LogDebug("Log sent to centralized server successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(
                ex,
                "Failed to send log to centralized server: {Message}",
                ex.Message);

            // Fallback: if CentralizedOnly mode, write locally as backup
            if (logMode == LogMode.CentralizedOnly)
            {
                _logger?.LogWarning("Centralized server unavailable, falling back to local storage");
                WriteLocalLog(transfer);
            }
        }
    }
}
