using Application.DTOs;
using Model;

namespace Application.Services;

public class ProgressTracker
{
    private int _totalFiles;
    private int _processedFiles;
    private long _totalBytes;
    private long _processedBytes;

    public void Initialize(List<FileDescriptor> files)
    {
        _totalFiles = files.Count;
        _totalBytes = files.Sum(f => f.Size);
        _processedFiles = 0;
        _processedBytes = 0;
    }

    public void FileProcessed(FileDescriptor file)
    {
        _processedFiles++;
        _processedBytes += file.Size;
    }

    public StateSnapshot BuildSnapshot(string jobName)
    {
        return new StateSnapshot
        {
            Name = jobName,
            Timestamp = DateTime.Now,
            State = JobState.Active,
            TotalFilesCount = _totalFiles,
            TotalFilesSize = _totalBytes,
            Progress = GetProgress(),
            RemainingFilesCount = _totalFiles - _processedFiles,
            RemainingFilesSize = _totalBytes - _processedBytes,
            CurrentSourceFilePath = string.Empty,
            CurrentTargetFilePath = string.Empty
        };
    }

    public int GetProgress()
    {
        if (_totalFiles == 0) return 0;
        return (int)((double)_processedFiles / _totalFiles * 100);
    }

    public void Reset()
    {
        _totalFiles = 0;
        _processedFiles = 0;
        _totalBytes = 0;
        _processedBytes = 0;
    }
}
