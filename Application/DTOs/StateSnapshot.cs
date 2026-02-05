using Model;

namespace Application.DTOs;

public record StateSnapshot
{
    public string Name { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public JobState State { get; init; }
    public int TotalFilesCount { get; init; }
    public long TotalFilesSize { get; init; }
    public int Progress { get; init; }
    public int RemainingFilesCount { get; init; }
    public long RemainingFilesSize { get; init; }
    public string CurrentSourceFilePath { get; init; } = string.Empty;
    public string CurrentTargetFilePath { get; init; } = string.Empty;
}
