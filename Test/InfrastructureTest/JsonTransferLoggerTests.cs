using System.Text.Json;
using Application.DTOs;
using Application.Ports;
using Infrastructure;

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
        BackupName = jobName,
        SourcePath = "/src/file.txt",
        DestPath = "/dst/file.txt",
        FileSize = 1024,
        TransferTimeMs = 50
    };

    [Fact]
    public void LogTransfer_ShouldCreateDailyLogFile()
    {
        ITransferLogger logger = new JsonTransferLogger(_testDir);

        logger.LogTransfer(CreateLog());

        var expectedFile = Path.Combine(_testDir, $"{DateTime.Now:yyyy-MM-dd}.json");
        Assert.True(File.Exists(expectedFile));
    }

    [Fact]
    public void LogTransfer_ShouldContainTransferLogFields()
    {
        ITransferLogger logger = new JsonTransferLogger(_testDir);

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
        ITransferLogger logger = new JsonTransferLogger(_testDir);

        logger.LogTransfer(CreateLog("Job1"));
        logger.LogTransfer(CreateLog("Job2"));

        var expectedFile = Path.Combine(_testDir, $"{DateTime.Now:yyyy-MM-dd}.json");
        var content = File.ReadAllText(expectedFile);
        Assert.Contains("Job1", content);
        Assert.Contains("Job2", content);
    }

    [Fact]
    public void LogTransfer_SingleEntry_ShouldProduceValidJsonArray()
    {
        var logger = new JsonTransferLogger(_testDir);

        logger.LogTransfer(CreateLog("Job1"));

        var expectedFile = Path.Combine(_testDir, $"{DateTime.Now:yyyy-MM-dd}.json");
        var content = File.ReadAllText(expectedFile);
        var array = JsonSerializer.Deserialize<List<TransferLog>>(content);
        Assert.NotNull(array);
        Assert.Single(array);
        Assert.Equal("Job1", array[0].BackupName);
    }

    [Fact]
    public void LogTransfer_MultipleEntries_ShouldProduceValidJsonArray()
    {
        var logger = new JsonTransferLogger(_testDir);

        logger.LogTransfer(CreateLog("Job1"));
        logger.LogTransfer(CreateLog("Job2"));

        var expectedFile = Path.Combine(_testDir, $"{DateTime.Now:yyyy-MM-dd}.json");
        var content = File.ReadAllText(expectedFile);
        var array = JsonSerializer.Deserialize<List<TransferLog>>(content);
        Assert.NotNull(array);
        Assert.Equal(2, array.Count);
        Assert.Equal("Job1", array[0].BackupName);
        Assert.Equal("Job2", array[1].BackupName);
    }

    [Fact]
    public void LogTransfer_ShouldUseDailyFilename()
    {
        var logger = new JsonTransferLogger(_testDir);

        logger.LogTransfer(CreateLog());

        var expectedFilename = $"{DateTime.Now:yyyy-MM-dd}.json";
        var files = Directory.GetFiles(_testDir);
        Assert.Single(files);
        Assert.Equal(expectedFilename, Path.GetFileName(files[0]));
    }

    [Fact]
    public void LogTransfer_WrittenFile_IsSpecCompliantJsonArray()
    {
        var logger = new JsonTransferLogger(_testDir);

        logger.LogTransfer(CreateLog("Job1"));
        logger.LogTransfer(CreateLog("Job2"));

        var filePath = Path.Combine(_testDir, $"{DateTime.Now:yyyy-MM-dd}.json");
        var content = File.ReadAllText(filePath);
        using var doc = JsonDocument.Parse(content);

        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(2, doc.RootElement.GetArrayLength());

        var expectedFields = new HashSet<string>
            { "Timestamp", "BackupName", "SourcePath", "DestPath", "FileSize", "TransferTimeMs" };
        var first = doc.RootElement[0];
        var actualFields = first.EnumerateObject().Select(p => p.Name).ToHashSet();
        Assert.True(expectedFields.SetEquals(actualFields));
    }
}
