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

    public DynamicTransferLogger(AppConfiguration config, IEasyLogger easyLogger)
    {
        _config = config;
        _easyLogger = easyLogger;
    }

    public void LogTransfer(TransferLog transfer)
    {
        var currentFormat = _config.GetLogFormat();
        var logDir = _config.GetLogDirectory();

        Directory.CreateDirectory(logDir);

        ITransferLogger logger = currentFormat switch
        {
            Shared.LogFormat.JSON => new JsonTransferLogger(logDir),
            Shared.LogFormat.XML => new XmlTransferLogger(logDir, _easyLogger),
            _ => throw new ArgumentOutOfRangeException(nameof(currentFormat))
        };

        logger.LogTransfer(transfer);
    }
}
