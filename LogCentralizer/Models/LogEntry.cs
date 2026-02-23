namespace LogCentralizer.Models;

public record LogEntry
{
    public DateTime Timestamp { get; init; }
    
    public string BackupName { get; init; } = string.Empty;
    
    public string SourcePath { get; init; } = string.Empty;

    public string DestPath { get; init; } = string.Empty;

    public long FileSize { get; init; }

    public long TransferTimeMs { get; init; }

    public long EncryptionTimeMs { get; init; }

    public string UserId { get; init; } = string.Empty;

    public string MachineName { get; init; } = string.Empty;

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(BackupName)
               && Timestamp != default;
    }
}

