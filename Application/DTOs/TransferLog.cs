namespace Application.DTOs;

public record TransferLog
{
    public DateTime Timestamp { get; init; }
    public string BackupName { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;
    public string DestPath { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public long TransferTimeMs { get; init; }
}
