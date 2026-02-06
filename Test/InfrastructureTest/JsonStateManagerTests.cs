using System.Text.Json;
using Application.DTOs;
using Application.Ports;
using Infrastructure;
using Model;

namespace InfrastructureTest;

public class JsonStateManagerTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _filePath;

    public JsonStateManagerTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"easysave_state_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
        _filePath = Path.Combine(_testDir, "state.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    private StateSnapshot CreateSnapshot(string name, JobState state = JobState.Active) =>
        new()
        {
            Name = name,
            Timestamp = DateTime.Now,
            State = state,
            TotalFiles = 10,
            TotalSize = 1024,
            Progress = 50,
            FilesRemaining = 5,
            SizeRemaining = 512,
            CurrentSourceFile = "/src/file.txt",
            CurrentDestFile = "/dst/file.txt"
        };

    [Fact]
    public void UpdateState_ShouldAddSnapshot()
    {
        IStateManager manager = new JsonStateManager(_filePath);

        manager.UpdateState(CreateSnapshot("Job1"));

        var states = manager.GetAllStates();
        Assert.Single(states);
        Assert.Equal("Job1", states[0].Name);
    }

    [Fact]
    public void UpdateState_SameJobName_ShouldReplaceSnapshot()
    {
        IStateManager manager = new JsonStateManager(_filePath);

        manager.UpdateState(CreateSnapshot("Job1", JobState.Active));
        manager.UpdateState(CreateSnapshot("Job1", JobState.End));

        var states = manager.GetAllStates();
        Assert.Single(states);
        Assert.Equal(JobState.End, states[0].State);
    }

    [Fact]
    public void GetAllStates_ShouldReturnAllSnapshots()
    {
        IStateManager manager = new JsonStateManager(_filePath);

        manager.UpdateState(CreateSnapshot("Job1"));
        manager.UpdateState(CreateSnapshot("Job2"));

        var states = manager.GetAllStates();
        Assert.Equal(2, states.Count);
    }

    [Fact]
    public void ClearAll_ShouldEmptyStates()
    {
        IStateManager manager = new JsonStateManager(_filePath);
        manager.UpdateState(CreateSnapshot("Job1"));

        manager.ClearAll();

        Assert.Empty(manager.GetAllStates());
    }

    [Fact]
    public void UpdateState_ShouldPersistToDiskImmediately()
    {
        var manager = new JsonStateManager(_filePath);
        manager.UpdateState(CreateSnapshot("Job1"));

        Assert.True(File.Exists(_filePath));
        var content = File.ReadAllText(_filePath);
        Assert.Contains("Job1", content);
    }

    [Fact]
    public void Persistence_DataSurvivesNewInstance()
    {
        var manager1 = new JsonStateManager(_filePath);
        manager1.UpdateState(CreateSnapshot("Persistent"));

        var manager2 = new JsonStateManager(_filePath);

        var states = manager2.GetAllStates();
        Assert.Single(states);
        Assert.Equal("Persistent", states[0].Name);
    }

    [Fact]
    public void WrittenFile_IsSpecCompliantStateArray()
    {
        var manager = new JsonStateManager(_filePath);
        manager.UpdateState(CreateSnapshot("Photos", JobState.Active));

        var content = File.ReadAllText(_filePath);
        using var doc = JsonDocument.Parse(content);

        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(1, doc.RootElement.GetArrayLength());

        var expectedFields = new HashSet<string>
        {
            "Name", "Timestamp", "State", "TotalFiles", "TotalSize",
            "Progress", "FilesRemaining", "SizeRemaining",
            "CurrentSourceFile", "CurrentDestFile"
        };
        var element = doc.RootElement[0];
        var actualFields = element.EnumerateObject().Select(p => p.Name).ToHashSet();
        Assert.True(expectedFields.SetEquals(actualFields));

        Assert.Equal("ACTIVE", element.GetProperty("State").GetString());
    }
}
