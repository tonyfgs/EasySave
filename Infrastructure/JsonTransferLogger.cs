using Application.DTOs;
using Application.Ports;
using Logger.Interface;

namespace Infrastructure;

public class JsonTransferLogger : ITransferLogger
{
    private readonly string _logDirectory;
    private readonly IEasyLogger _easyLogger;

    public JsonTransferLogger(string logDirectory, IEasyLogger easyLogger)
    {
        _logDirectory = logDirectory;
        _easyLogger = easyLogger;
    }

    public void LogTransfer(TransferLog log)
    {
        var dailyLogPath = GetDailyLogPath();
        _easyLogger.WriteInFile(dailyLogPath, log);
    }

    private string GetDailyLogPath()
    {
        return Path.Combine(_logDirectory, "placeholder.json");
    }
}
