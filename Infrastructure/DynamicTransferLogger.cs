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
        var logMode = _centralizationService?.GetLogMode() ?? LogMode.LocalOnly;

        _logger?.LogDebug(
            "Logging transfer: {BackupName} - {SourcePath}, Mode: {Mode}",
            transfer.BackupName,
            transfer.SourcePath,
            logMode);

        if (logMode == LogMode.LocalOnly || logMode == LogMode.LocalAndCentralized)
        {
            WriteLocalLog(transfer);
        }

        if (_centralizationService != null &&
            (logMode == LogMode.CentralizedOnly || logMode == LogMode.LocalAndCentralized))
        {
            SendToCentralizedServerAsync(transfer, logMode).GetAwaiter().GetResult();
        }
    }

    private void WriteLocalLog(TransferLog transfer)
    {
        var currentFormat = _config.GetLogFormat();
        var logDir = _config.GetLogDirectory();

        Directory.CreateDirectory(logDir);

        ITransferLogger logger = currentFormat switch
        {
            LogFormat.JSON => new JsonTransferLogger(logDir),
            LogFormat.XML => new XmlTransferLogger(logDir, _easyLogger),
            _ => throw new ArgumentOutOfRangeException(nameof(currentFormat))
        };

        logger.LogTransfer(transfer);
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

            if (logMode == LogMode.CentralizedOnly)
            {
                _logger?.LogWarning("Centralized server unavailable, falling back to local storage");
                WriteLocalLog(transfer);
            }
        }
    }
}
