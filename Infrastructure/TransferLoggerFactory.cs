using Application.Ports;
using Logger.Interface;
using Shared;

namespace Infrastructure;

public class TransferLoggerFactory
{
    private readonly LogFormat _logFormat;
    private readonly IEasyLogger _easyLogger;
    private readonly string _logDirectory;

    public TransferLoggerFactory(LogFormat logFormat, IEasyLogger easyLogger, string logDirectory)
    {
        _logFormat = logFormat;
        _easyLogger = easyLogger;
        _logDirectory = logDirectory;
    }

    public ITransferLogger Create()
    {
        return _logFormat switch
        {
            LogFormat.JSON => new JsonTransferLogger(_logDirectory, _easyLogger),
            LogFormat.XML => new XmlTransferLogger(_logDirectory, _easyLogger),
            _ => throw new ArgumentOutOfRangeException(nameof(_logFormat), _logFormat, "Unsupported log format")
        };
    }
}
