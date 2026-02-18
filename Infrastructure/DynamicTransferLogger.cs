using Application.DTOs;
using Application.Ports;
using Logger.Interface;

namespace Infrastructure;

public class DynamicTransferLogger : ITransferLogger
{
    private readonly AppConfiguration _config;
    private readonly IEasyLogger _easyLogger;
    private readonly ILogCentralizationService? _centralizationService;
    private readonly string _userId;

    public DynamicTransferLogger(
        AppConfiguration config,
        IEasyLogger easyLogger,
        string logDirectory,
        ILogCentralizationService? centralizationService = null,
        string? userId = null)
    {
        _config = config;
        _easyLogger = easyLogger;
        _centralizationService = centralizationService;
        _userId = userId ?? Environment.UserName;
    }

    public void LogTransfer(TransferLog transfer)
    {
        try
        {
            var logMode = _centralizationService?.GetLogMode() ?? LogMode.LocalOnly;

            Console.WriteLine($"[DynamicTransferLogger] Logging transfer: {transfer.BackupName} - {transfer.SourcePath}");
            Console.WriteLine($"[DynamicTransferLogger] Mode: {logMode}");

            // Write to local storage if mode includes local
            if (logMode == LogMode.LocalOnly || logMode == LogMode.LocalAndCentralized)
            {
                WriteLocalLog(transfer);
            }

            // Send to centralized server if mode includes centralized
            if (_centralizationService != null &&
                (logMode == LogMode.CentralizedOnly || logMode == LogMode.LocalAndCentralized))
            {
                SendToCentralizedServer(transfer);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DynamicTransferLogger] ❌ ERROR: {ex.Message}");
            Console.WriteLine($"[DynamicTransferLogger] Stack: {ex.StackTrace}");
        }
    }

    private void WriteLocalLog(TransferLog transfer)
    {
        var currentFormat = _config.GetLogFormat();
        var logDir = _config.GetLogDirectory();

        Console.WriteLine($"[DynamicTransferLogger] Format: {currentFormat}, Directory: {logDir}");

        Directory.CreateDirectory(logDir);

        ITransferLogger logger = currentFormat switch
        {
            Shared.LogFormat.JSON => new JsonTransferLogger(logDir),
            Shared.LogFormat.XML => new XmlTransferLogger(logDir, _easyLogger),
            _ => throw new ArgumentOutOfRangeException(nameof(currentFormat))
        };

        logger.LogTransfer(transfer);

        var expectedFile = Path.Combine(logDir, $"{DateTime.Now:yyyy-MM-dd}.{(currentFormat == Shared.LogFormat.JSON ? "json" : "xml")}");
        Console.WriteLine($"[DynamicTransferLogger] ✓ Local log written to: {expectedFile}");
    }

    private void SendToCentralizedServer(TransferLog transfer)
    {
        try
        {
            // Fire and forget - don't block the backup process
            _ = _centralizationService!.SendLogAsync(transfer, _userId);
            Console.WriteLine($"[DynamicTransferLogger] ✓ Log sent to centralized server");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DynamicTransferLogger] ⚠ Failed to send to server: {ex.Message}");
        }
    }
}
