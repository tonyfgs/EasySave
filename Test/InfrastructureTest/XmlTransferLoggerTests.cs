using Application.DTOs;
using Application.Ports;
using Infrastructure;
using Logger.Service;

namespace InfrastructureTest;

public class XmlTransferLoggerTests : IDisposable
{
    private readonly string _testDir;

    public XmlTransferLoggerTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"easysave_xmllog_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    private TransferLog CreateLog(string jobName = "TestJob") => new()
    {
        Timestamp = DateTime.Now,
        BackupJobName = jobName,
        SourceFilePath = "/src/file.txt",
        TargetFilePath = "/dst/file.txt",
        FileSize = 2048,
        TransferTimeMs = 75
    };

    [Fact]
    public void LogTransfer_ShouldCreateDailyXmlFile()
    {
        var easyLogger = new DailyLogsService();
        ITransferLogger logger = new XmlTransferLogger(_testDir, easyLogger);

        logger.LogTransfer(CreateLog());

        var expectedFile = Path.Combine(_testDir, $"{DateTime.Now:yyyy-MM-dd}.xml");
        Assert.True(File.Exists(expectedFile));
    }

    [Fact]
    public void LogTransfer_ShouldContainTransferLogFieldsInXml()
    {
        var easyLogger = new DailyLogsService();
        ITransferLogger logger = new XmlTransferLogger(_testDir, easyLogger);

        logger.LogTransfer(CreateLog("XmlBackup"));

        var expectedFile = Path.Combine(_testDir, $"{DateTime.Now:yyyy-MM-dd}.xml");
        var content = File.ReadAllText(expectedFile);
        Assert.Contains("XmlBackup", content);
        Assert.Contains("2048", content);
        Assert.Contains("<TransferLog", content);
    }

    [Fact]
    public void LogTransfer_MultipleCalls_ShouldAppendEntries()
    {
        var easyLogger = new DailyLogsService();
        ITransferLogger logger = new XmlTransferLogger(_testDir, easyLogger);

        logger.LogTransfer(CreateLog("XmlJob1"));
        logger.LogTransfer(CreateLog("XmlJob2"));

        var expectedFile = Path.Combine(_testDir, $"{DateTime.Now:yyyy-MM-dd}.xml");
        var content = File.ReadAllText(expectedFile);
        Assert.Contains("XmlJob1", content);
        Assert.Contains("XmlJob2", content);
    }
}
