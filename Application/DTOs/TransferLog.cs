namespace Application.DTOs;

public record TransferLog
{
    public DateTime Timestamp { get; init; }
    public string BackupJobName { get; init; } = string.Empty;
    public string SourceFilePath { get; init; } = string.Empty;
    public string TargetFilePath { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public long TransferTimeMs { get; init; }
}
