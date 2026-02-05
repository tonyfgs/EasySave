using Application.DTOs;
using Application.Ports;
using Infrastructure;
using Logger.Service;

namespace InfrastructureTest;

public class JsonTransferLoggerTests : IDisposable
{
    private readonly string _testDir;

    public JsonTransferLoggerTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"easysave_jsonlog_test_{Guid.NewGuid()}");
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
        FileSize = 1024,
        TransferTimeMs = 50
    };

    [Fact]
    public void LogTransfer_ShouldCreateDailyLogFile()
    {
        var easyLogger = new DailyLogsService();
        ITransferLogger logger = new JsonTransferLogger(_testDir, easyLogger);

        logger.LogTransfer(CreateLog());

        var expectedFile = Path.Combine(_testDir, $"{DateTime.Now:yyyy-MM-dd}.json");
        Assert.True(File.Exists(expectedFile));
    }

    [Fact]
    public void LogTransfer_ShouldContainTransferLogFields()
    {
        var easyLogger = new DailyLogsService();
        ITransferLogger logger = new JsonTransferLogger(_testDir, easyLogger);

        logger.LogTransfer(CreateLog("BackupAlpha"));

        var expectedFile = Path.Combine(_testDir, $"{DateTime.Now:yyyy-MM-dd}.json");
        var content = File.ReadAllText(expectedFile);
        Assert.Contains("BackupAlpha", content);
        Assert.Contains("1024", content);
        Assert.Contains("/src/file.txt", content);
    }

    [Fact]
    public void LogTransfer_MultipleCalls_ShouldAppendToSameFile()
    {
        var easyLogger = new DailyLogsService();
        ITransferLogger logger = new JsonTransferLogger(_testDir, easyLogger);

        logger.LogTransfer(CreateLog("Job1"));
        logger.LogTransfer(CreateLog("Job2"));

        var expectedFile = Path.Combine(_testDir, $"{DateTime.Now:yyyy-MM-dd}.json");
        var content = File.ReadAllText(expectedFile);
        Assert.Contains("Job1", content);
        Assert.Contains("Job2", content);
    }
}
