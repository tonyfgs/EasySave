using Logger.Interface;
using Logger.Service;

namespace LoggerTest;

public class DailyLogsServiceTests : IDisposable
{
    private readonly string _testDir;

    public DailyLogsServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"easysave_log_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [Fact]
    public void WriteInFile_ShouldCreateDailyLogFile()
    {
        IEasyLogger service = new DailyLogsService();
        var logData = new { Message = "test", Value = 42 };
        var path = Path.Combine(_testDir, "placeholder.json");

        service.WriteInFile(path, logData);

        var expectedFile = Path.Combine(_testDir, $"{DateTime.Now:yyyy-MM-dd}.json");
        Assert.True(File.Exists(expectedFile));
    }

    [Fact]
    public void WriteInFile_ShouldSerializeDataToJson()
    {
        IEasyLogger service = new DailyLogsService();
        var logData = new { BackupJobName = "TestJob", FileSize = 1024L };
        var path = Path.Combine(_testDir, "placeholder.json");

        service.WriteInFile(path, logData);

        var expectedFile = Path.Combine(_testDir, $"{DateTime.Now:yyyy-MM-dd}.json");
        var content = File.ReadAllText(expectedFile);
        Assert.Contains("TestJob", content);
        Assert.Contains("1024", content);
    }

    [Fact]
    public void WriteInFile_ShouldAppendMultipleEntries()
    {
        IEasyLogger service = new DailyLogsService();
        var path = Path.Combine(_testDir, "placeholder.json");

        service.WriteInFile(path, new { Entry = 1 });
        service.WriteInFile(path, new { Entry = 2 });

        var expectedFile = Path.Combine(_testDir, $"{DateTime.Now:yyyy-MM-dd}.json");
        var content = File.ReadAllText(expectedFile);
        Assert.Contains("1", content);
        Assert.Contains("2", content);
    }

    [Fact]
    public void WriteInFile_WithAnonymousType_ShouldWork()
    {
        IEasyLogger service = new DailyLogsService();
        var path = Path.Combine(_testDir, "placeholder.json");
        var logData = new { Timestamp = DateTime.Now, Custom = "value" };

        var exception = Record.Exception(() => service.WriteInFile(path, logData));

        Assert.Null(exception);
    }

    [Fact]
    public void WriteInFile_ShouldCreateDirectoryIfMissing()
    {
        IEasyLogger service = new DailyLogsService();
        var nestedDir = Path.Combine(_testDir, "nested", "dir");
        var path = Path.Combine(nestedDir, "placeholder.json");

        service.WriteInFile(path, new { Data = "test" });

        Assert.True(Directory.Exists(nestedDir));
    }

    [Fact]
    public void WriteInFile_MultipleEntries_IsNotValidJsonArray()
    {
        IEasyLogger service = new DailyLogsService();
        var path = Path.Combine(_testDir, "placeholder.json");

        service.WriteInFile(path, new { Entry = 1 });
        service.WriteInFile(path, new { Entry = 2 });

        var expectedFile = Path.Combine(_testDir, $"{DateTime.Now:yyyy-MM-dd}.json");
        var content = File.ReadAllText(expectedFile);
        var ex = Record.Exception(() =>
            System.Text.Json.JsonSerializer.Deserialize<List<object>>(content));
        Assert.NotNull(ex);
    }
}
