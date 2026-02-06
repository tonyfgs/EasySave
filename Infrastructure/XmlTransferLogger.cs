using System.Xml.Serialization;
using Application.DTOs;
using Application.Ports;
using Logger.Interface;

namespace Infrastructure;

public class XmlTransferLogger : ITransferLogger
{
    private readonly string _logDirectory;
    private readonly IEasyLogger _easyLogger;

    public XmlTransferLogger(string logDirectory, IEasyLogger easyLogger)
    {
        _logDirectory = logDirectory;
        _easyLogger = easyLogger;
    }

    public void LogTransfer(TransferLog log)
    {
        var dailyLogPath = GetDailyLogPath();
        var serializer = new XmlSerializer(typeof(TransferLog));

        Directory.CreateDirectory(_logDirectory);

        using var writer = new StreamWriter(dailyLogPath, append: true);
        serializer.Serialize(writer, log);
        writer.WriteLine();
    }

    private string GetDailyLogPath()
    {
        return Path.Combine(_logDirectory, $"{DateTime.Now:yyyy-MM-dd}.xml");
    }
}
