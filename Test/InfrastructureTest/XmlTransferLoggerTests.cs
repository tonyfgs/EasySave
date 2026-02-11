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
        BackupName = jobName,
        SourcePath = "/src/file.txt",
        DestPath = "/dst/file.txt",
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
        Assert.Contains("<EncryptionTimeMs>0</EncryptionTimeMs>", content);
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

    [Fact]
    public void LogTransfer_ShouldSerializeAllEncryptionTimeMsSemanticsInXml()
    {
        var easyLogger = new DailyLogsService();
        ITransferLogger logger = new XmlTransferLogger(_testDir, easyLogger);

        logger.LogTransfer(new TransferLog
        {
            Timestamp = DateTime.Now,
            BackupName = "NotEncrypted",
            SourcePath = "/src/a.txt",
            DestPath = "/dst/a.txt",
            FileSize = 1024,
            TransferTimeMs = 50,
            EncryptionTimeMs = 0
        });
        logger.LogTransfer(new TransferLog
        {
            Timestamp = DateTime.Now,
            BackupName = "Encrypted",
            SourcePath = "/src/b.txt",
            DestPath = "/dst/b.txt",
            FileSize = 2048,
            TransferTimeMs = 100,
            EncryptionTimeMs = 350
        });
        logger.LogTransfer(new TransferLog
        {
            Timestamp = DateTime.Now,
            BackupName = "EncryptionFailed",
            SourcePath = "/src/c.txt",
            DestPath = "/dst/c.txt",
            FileSize = 4096,
            TransferTimeMs = 200,
            EncryptionTimeMs = -1
        });

        var expectedFile = Path.Combine(_testDir, $"{DateTime.Now:yyyy-MM-dd}.xml");
        var content = File.ReadAllText(expectedFile);
        Assert.Contains("<EncryptionTimeMs>0</EncryptionTimeMs>", content);
        Assert.Contains("<EncryptionTimeMs>350</EncryptionTimeMs>", content);
        Assert.Contains("<EncryptionTimeMs>-1</EncryptionTimeMs>", content);
    }
}
