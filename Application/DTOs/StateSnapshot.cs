using Model;

namespace Application.DTOs;

public record StateSnapshot
{
    public string Name { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public JobState State { get; init; }
    public int TotalFiles { get; init; }
    public long TotalSize { get; init; }
    public int Progress { get; init; }
    public int FilesRemaining { get; init; }
    public long SizeRemaining { get; init; }
    public string CurrentSourceFile { get; init; } = string.Empty;
    public string CurrentDestFile { get; init; } = string.Empty;
}
