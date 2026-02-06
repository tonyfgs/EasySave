using System.Text.Json;
using Application.DTOs;

namespace InfrastructureTest;

public class TransferLogSerializationTests
{
    [Fact]
    public void TransferLog_SerializesToSpecFieldNames()
    {
        var log = new TransferLog
        {
            Timestamp = new DateTime(2024, 1, 26, 14, 30, 0),
            BackupName = "Sauvegarde Photos",
            SourcePath = "\\\\PC\\D$\\Photos\\chat.jpg",
            DestPath = "\\\\PC\\E$\\Backup\\Photos\\chat.jpg",
            FileSize = 2048576,
            TransferTimeMs = 150
        };

        var json = JsonSerializer.Serialize(log, new JsonSerializerOptions { WriteIndented = true });

        Assert.Contains("\"BackupName\"", json);
        Assert.Contains("\"SourcePath\"", json);
        Assert.Contains("\"DestPath\"", json);
        Assert.DoesNotContain("\"BackupJobName\"", json);
        Assert.DoesNotContain("\"SourceFilePath\"", json);
        Assert.DoesNotContain("\"TargetFilePath\"", json);
    }

    [Fact]
    public void TransferLog_DeserializesFromSpecFieldNames()
    {
        var json = """
        {
            "Timestamp": "2024-01-26T14:30:00",
            "BackupName": "Test",
            "SourcePath": "\\\\PC\\src",
            "DestPath": "\\\\PC\\dst",
            "FileSize": 1024,
            "TransferTimeMs": 50
        }
        """;

        var log = JsonSerializer.Deserialize<TransferLog>(json);

        Assert.NotNull(log);
        Assert.Equal("Test", log.BackupName);
        Assert.Equal("\\\\PC\\src", log.SourcePath);
        Assert.Equal("\\\\PC\\dst", log.DestPath);
    }

    [Fact]
    public void TransferLog_SerializedJson_HasExactly6SpecFields()
    {
        var log = new TransferLog
        {
            Timestamp = new DateTime(2024, 1, 26, 14, 30, 0),
            BackupName = "Photos",
            SourcePath = "\\\\PC\\D$\\Photos\\chat.jpg",
            DestPath = "\\\\PC\\E$\\Backup\\chat.jpg",
            FileSize = 2048576,
            TransferTimeMs = 150
        };

        var json = JsonSerializer.Serialize(log);
        using var doc = JsonDocument.Parse(json);
        var propertyNames = doc.RootElement.EnumerateObject().Select(p => p.Name).ToList();

        Assert.Equal(6, propertyNames.Count);
        Assert.Contains("Timestamp", propertyNames);
        Assert.Contains("BackupName", propertyNames);
        Assert.Contains("SourcePath", propertyNames);
        Assert.Contains("DestPath", propertyNames);
        Assert.Contains("FileSize", propertyNames);
        Assert.Contains("TransferTimeMs", propertyNames);
    }

    [Fact]
    public void TransferLog_RoundTrip_PreservesAllValues()
    {
        var original = new TransferLog
        {
            Timestamp = new DateTime(2024, 1, 26, 14, 30, 0),
            BackupName = "Photos Backup",
            SourcePath = "\\\\PC\\D$\\Photos\\chat.jpg",
            DestPath = "\\\\PC\\E$\\Backup\\Photos\\chat.jpg",
            FileSize = 2048576,
            TransferTimeMs = 150
        };

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<TransferLog>(json);

        Assert.NotNull(restored);
        Assert.Equal(original.Timestamp, restored.Timestamp);
        Assert.Equal(original.BackupName, restored.BackupName);
        Assert.Equal(original.SourcePath, restored.SourcePath);
        Assert.Equal(original.DestPath, restored.DestPath);
        Assert.Equal(original.FileSize, restored.FileSize);
        Assert.Equal(original.TransferTimeMs, restored.TransferTimeMs);
    }
}
