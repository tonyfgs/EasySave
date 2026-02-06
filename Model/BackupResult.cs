namespace Model;

public record BackupResult
{
    public bool Success { get; init; }
    public int FilesProcessed { get; init; }
    public long BytesTransferred { get; init; }
    public TimeSpan Duration { get; init; }
    public IReadOnlyList<string> Errors { get; init; }

    private BackupResult(bool success, int filesProcessed, long bytesTransferred,
        TimeSpan duration, IReadOnlyList<string> errors)
    {
        Success = success;
        FilesProcessed = filesProcessed;
        BytesTransferred = bytesTransferred;
        Duration = duration;
        Errors = errors;
    }

    public static BackupResult Ok(int filesProcessed, long bytesTransferred, TimeSpan duration)
    {
        return new BackupResult(true, filesProcessed, bytesTransferred, duration,
            Array.Empty<string>());
    }

    public static BackupResult Fail(List<string> errors, TimeSpan duration)
    {
        return new BackupResult(false, 0, 0, duration, new List<string>(errors).AsReadOnly());
    }
}
