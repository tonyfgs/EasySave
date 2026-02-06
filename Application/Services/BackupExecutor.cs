using System.Diagnostics;
using Application.DTOs;
using Application.Events;
using Application.Ports;
using Model;

namespace Application.Services;

public class BackupExecutor
{
    private readonly IFileSystemGateway _fileSystem;
    private readonly IPathAdapter _pathAdapter;
    private readonly IEventBus _eventBus;
    private readonly BackupDomainService _domainService;
    private readonly ProgressTracker _tracker;

    public BackupExecutor(
        IFileSystemGateway fileSystem,
        IPathAdapter pathAdapter,
        IEventBus eventBus,
        BackupDomainService domainService,
        ProgressTracker tracker)
    {
        _fileSystem = fileSystem;
        _pathAdapter = pathAdapter;
        _eventBus = eventBus;
        _domainService = domainService;
        _tracker = tracker;
    }

    public BackupResult Execute(BackupJob job, IBackupStrategy strategy)
    {
        var stopwatch = Stopwatch.StartNew();
        var errors = new List<string>();
        int filesProcessed = 0;
        long bytesTransferred = 0;

        try
        {
            var sourcePath = Path.GetFullPath(job.SourcePath);
            var targetPath = Path.GetFullPath(job.TargetPath);

            _fileSystem.EnsureDirectory(targetPath);

            var allFiles = _fileSystem.EnumerateFiles(sourcePath);
            var filesToCopy = _domainService.SelectFilesForBackup(job, allFiles, strategy);

            _tracker.Initialize(filesToCopy.ToList());

            foreach (var file in filesToCopy)
            {
                var relativePath = Path.GetRelativePath(sourcePath, file.Path);
                var targetFilePath = Path.Combine(targetPath, relativePath);

                _tracker.SetCurrentFile(
                    _pathAdapter.ToUNC(file.Path),
                    _pathAdapter.ToUNC(targetFilePath));

                try
                {
                    var transferStopwatch = Stopwatch.StartNew();
                    var bytesCopied = _fileSystem.CopyFile(file.Path, targetFilePath);
                    transferStopwatch.Stop();

                    filesProcessed++;
                    bytesTransferred += bytesCopied;

                    _tracker.FileProcessed(file);

                    var transferLog = new TransferLog
                    {
                        Timestamp = DateTime.Now,
                        BackupName = job.Name,
                        SourcePath = _pathAdapter.ToUNC(file.Path),
                        DestPath = _pathAdapter.ToUNC(targetFilePath),
                        FileSize = file.Size,
                        TransferTimeMs = transferStopwatch.ElapsedMilliseconds
                    };
                    _eventBus.Publish(new TransferCompletedEvent(transferLog));

                    var snapshot = _tracker.BuildSnapshot(job.Name);
                    _eventBus.Publish(new StateChangedEvent(snapshot));
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to copy {file.Path}: {ex.Message}");

                    var errorLog = new TransferLog
                    {
                        Timestamp = DateTime.Now,
                        BackupName = job.Name,
                        SourcePath = _pathAdapter.ToUNC(file.Path),
                        DestPath = _pathAdapter.ToUNC(targetFilePath),
                        FileSize = file.Size,
                        TransferTimeMs = -1
                    };
                    _eventBus.Publish(new TransferCompletedEvent(errorLog));

                    _tracker.FileProcessed(file);
                    var snapshot = _tracker.BuildSnapshot(job.Name);
                    _eventBus.Publish(new StateChangedEvent(snapshot));
                }
            }

            _tracker.SetState(errors.Count > 0 ? JobState.Error : JobState.End);
            _tracker.ClearCurrentFile();
            var endSnapshot = _tracker.BuildSnapshot(job.Name);
            _eventBus.Publish(new StateChangedEvent(endSnapshot));

            if (strategy is FullBackupStrategy)
            {
                job.MarkFullBackupCompleted(DateTime.Now);
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Backup execution failed: {ex.Message}");
        }

        stopwatch.Stop();

        if (errors.Count > 0)
            return BackupResult.Fail(errors, stopwatch.Elapsed);

        return BackupResult.Ok(filesProcessed, bytesTransferred, stopwatch.Elapsed);
    }
}
