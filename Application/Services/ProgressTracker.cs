using Application.DTOs;
using Model;

namespace Application.Services;

public class ProgressTracker
{
    private int _totalFiles;
    private int _processedFiles;
    private long _totalBytes;
    private long _processedBytes;
    private string _currentSourceFile = string.Empty;
    private string _currentDestFile = string.Empty;
    private JobState _state = JobState.Inactive;

    public void Initialize(List<FileDescriptor> files)
    {
        _totalFiles = files.Count;
        _totalBytes = files.Sum(f => f.Size);
        _processedFiles = 0;
        _processedBytes = 0;
        _state = JobState.Active;
    }

    public void FileProcessed(FileDescriptor file)
    {
        _processedFiles++;
        _processedBytes += file.Size;
    }

    public void SetCurrentFile(string sourcePath, string destPath)
    {
        _currentSourceFile = sourcePath;
        _currentDestFile = destPath;
    }

    public void ClearCurrentFile()
    {
        _currentSourceFile = string.Empty;
        _currentDestFile = string.Empty;
    }

    public void SetState(JobState state)
    {
        _state = state;
    }

    public StateSnapshot BuildSnapshot(string jobName)
    {
        return new StateSnapshot
        {
            Name = jobName,
            Timestamp = DateTime.Now,
            State = _state,
            TotalFiles = _totalFiles,
            TotalSize = _totalBytes,
            Progress = GetProgress(),
            FilesRemaining = _totalFiles - _processedFiles,
            SizeRemaining = _totalBytes - _processedBytes,
            CurrentSourceFile = _currentSourceFile,
            CurrentDestFile = _currentDestFile
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
        _currentSourceFile = string.Empty;
        _currentDestFile = string.Empty;
        _state = JobState.Inactive;
    }
}
