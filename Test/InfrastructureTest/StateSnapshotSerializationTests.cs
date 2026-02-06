using System.Text.Json;
using Application.DTOs;
using Model;

namespace InfrastructureTest;

public class StateSnapshotSerializationTests
{
    [Fact]
    public void StateSnapshot_SerializesToSpecFieldNames()
    {
        var snapshot = new StateSnapshot
        {
            Name = "Sauvegarde Photos",
            Timestamp = new DateTime(2024, 1, 26, 14, 32, 16),
            State = JobState.Active,
            TotalFiles = 150,
            TotalSize = 524288000,
            Progress = 45,
            FilesRemaining = 82,
            SizeRemaining = 288358400,
            CurrentSourceFile = "\\\\PC\\D$\\Photos\\vacances.jpg",
            CurrentDestFile = "\\\\PC\\E$\\Backup\\Photos\\vacances.jpg"
        };

        var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });

        Assert.Contains("\"TotalFiles\"", json);
        Assert.Contains("\"TotalSize\"", json);
        Assert.Contains("\"FilesRemaining\"", json);
        Assert.Contains("\"SizeRemaining\"", json);
        Assert.Contains("\"CurrentSourceFile\"", json);
        Assert.Contains("\"CurrentDestFile\"", json);
        Assert.DoesNotContain("\"TotalFilesCount\"", json);
        Assert.DoesNotContain("\"TotalFilesSize\"", json);
        Assert.DoesNotContain("\"RemainingFilesCount\"", json);
        Assert.DoesNotContain("\"RemainingFilesSize\"", json);
        Assert.DoesNotContain("\"CurrentSourceFilePath\"", json);
        Assert.DoesNotContain("\"CurrentTargetFilePath\"", json);
    }

    [Fact]
    public void StateSnapshot_DeserializesFromSpecFieldNames()
    {
        var json = """
        {
            "Name": "Test",
            "Timestamp": "2024-01-26T14:32:16",
            "TotalFiles": 10,
            "TotalSize": 1024,
            "Progress": 50,
            "FilesRemaining": 5,
            "SizeRemaining": 512,
            "CurrentSourceFile": "\\\\PC\\src",
            "CurrentDestFile": "\\\\PC\\dst"
        }
        """;

        var snapshot = JsonSerializer.Deserialize<StateSnapshot>(json);

        Assert.NotNull(snapshot);
        Assert.Equal(10, snapshot.TotalFiles);
        Assert.Equal(1024, snapshot.TotalSize);
        Assert.Equal(5, snapshot.FilesRemaining);
        Assert.Equal(512, snapshot.SizeRemaining);
        Assert.Equal("\\\\PC\\src", snapshot.CurrentSourceFile);
        Assert.Equal("\\\\PC\\dst", snapshot.CurrentDestFile);
    }

    [Fact]
    public void StateSnapshot_SerializedJson_HasExactly10SpecFields()
    {
        var snapshot = new StateSnapshot
        {
            Name = "Photos",
            Timestamp = new DateTime(2024, 1, 26, 14, 32, 16),
            State = JobState.Active,
            TotalFiles = 150,
            TotalSize = 524288000,
            Progress = 45,
            FilesRemaining = 82,
            SizeRemaining = 288358400,
            CurrentSourceFile = "\\\\PC\\D$\\Photos\\vacances.jpg",
            CurrentDestFile = "\\\\PC\\E$\\Backup\\vacances.jpg"
        };

        var json = JsonSerializer.Serialize(snapshot);
        using var doc = JsonDocument.Parse(json);
        var propertyNames = doc.RootElement.EnumerateObject().Select(p => p.Name).ToList();

        Assert.Equal(10, propertyNames.Count);
        Assert.Contains("Name", propertyNames);
        Assert.Contains("Timestamp", propertyNames);
        Assert.Contains("State", propertyNames);
        Assert.Contains("TotalFiles", propertyNames);
        Assert.Contains("TotalSize", propertyNames);
        Assert.Contains("Progress", propertyNames);
        Assert.Contains("FilesRemaining", propertyNames);
        Assert.Contains("SizeRemaining", propertyNames);
        Assert.Contains("CurrentSourceFile", propertyNames);
        Assert.Contains("CurrentDestFile", propertyNames);
    }

    [Fact]
    public void StateSnapshot_RoundTrip_PreservesAllValues()
    {
        var original = new StateSnapshot
        {
            Name = "Photos",
            Timestamp = new DateTime(2024, 1, 26, 14, 32, 16),
            State = JobState.Active,
            TotalFiles = 150,
            TotalSize = 524288000,
            Progress = 45,
            FilesRemaining = 82,
            SizeRemaining = 288358400,
            CurrentSourceFile = "\\\\PC\\D$\\Photos\\vacances.jpg",
            CurrentDestFile = "\\\\PC\\E$\\Backup\\vacances.jpg"
        };

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<StateSnapshot>(json);

        Assert.NotNull(restored);
        Assert.Equal(original.Name, restored.Name);
        Assert.Equal(original.Timestamp, restored.Timestamp);
        Assert.Equal(original.State, restored.State);
        Assert.Equal(original.TotalFiles, restored.TotalFiles);
        Assert.Equal(original.TotalSize, restored.TotalSize);
        Assert.Equal(original.Progress, restored.Progress);
        Assert.Equal(original.FilesRemaining, restored.FilesRemaining);
        Assert.Equal(original.SizeRemaining, restored.SizeRemaining);
        Assert.Equal(original.CurrentSourceFile, restored.CurrentSourceFile);
        Assert.Equal(original.CurrentDestFile, restored.CurrentDestFile);
    }

    [Fact]
    public void StateSnapshot_ActiveState_SerializesAsACTIVE_InParentObject()
    {
        var snapshot = new StateSnapshot
        {
            Name = "Test",
            State = JobState.Active
        };

        var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });

        Assert.Contains("\"ACTIVE\"", json);
        Assert.DoesNotContain("\"Active\"", json);
    }

    [Fact]
    public void StateSnapshot_InactiveState_SerializesNumericFieldsAsZero()
    {
        var snapshot = new StateSnapshot
        {
            Name = "Idle Job",
            State = JobState.Inactive
        };

        var json = JsonSerializer.Serialize(snapshot);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("INACTIVE", root.GetProperty("State").GetString());
        Assert.Equal(0, root.GetProperty("TotalFiles").GetInt32());
        Assert.Equal(0, root.GetProperty("TotalSize").GetInt64());
        Assert.Equal(0, root.GetProperty("Progress").GetInt32());
        Assert.Equal(0, root.GetProperty("FilesRemaining").GetInt32());
        Assert.Equal(0, root.GetProperty("SizeRemaining").GetInt64());
        Assert.Equal(string.Empty, root.GetProperty("CurrentSourceFile").GetString());
        Assert.Equal(string.Empty, root.GetProperty("CurrentDestFile").GetString());
    }
}
