using Application.DTOs;
using Application.Ports;
using Logger.Interface;

namespace Infrastructure;

/// <summary>
/// A transfer logger that dynamically switches between JSON and XML formats
/// based on the current configuration. This allows format changes to take
/// effect immediately without restarting the application.
/// </summary>
public class DynamicTransferLogger : ITransferLogger
{
    private readonly AppConfiguration _config;
    private readonly IEasyLogger _easyLogger;

    public DynamicTransferLogger(AppConfiguration config, IEasyLogger easyLogger, string logDirectory)
    {
        _config = config;
        _easyLogger = easyLogger;
    }

    public void LogTransfer(TransferLog transfer)
    {
        try
        {
            // Get the current format and create the appropriate logger
            var currentFormat = _config.GetLogFormat();
            var logDir = _config.GetLogDirectory();

            // Debug: Print to console
            Console.WriteLine($"[DynamicTransferLogger] Logging transfer: {transfer.BackupName} - {transfer.SourcePath}");
            Console.WriteLine($"[DynamicTransferLogger] Format: {currentFormat}, Directory: {logDir}");

            // Ensure directory exists
            Directory.CreateDirectory(logDir);

            // Create a new logger with the current format
            ITransferLogger logger = currentFormat switch
            {
                Shared.LogFormat.JSON => new JsonTransferLogger(logDir),
                Shared.LogFormat.XML => new XmlTransferLogger(logDir, _easyLogger),
                _ => throw new ArgumentOutOfRangeException(nameof(currentFormat))
            };

            // Use the logger to log the transfer
            logger.LogTransfer(transfer);

            var expectedFile = Path.Combine(logDir, $"{DateTime.Now:yyyy-MM-dd}.{(currentFormat == Shared.LogFormat.JSON ? "json" : "xml")}");
            Console.WriteLine($"[DynamicTransferLogger] ✓ Log written to: {expectedFile}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DynamicTransferLogger] ❌ ERROR: {ex.Message}");
            Console.WriteLine($"[DynamicTransferLogger] Stack: {ex.StackTrace}");
        }
    }
}
