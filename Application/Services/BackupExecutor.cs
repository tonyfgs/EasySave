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
            _fileSystem.EnsureDirectory(job.TargetPath);

            var allFiles = _fileSystem.EnumerateFiles(job.SourcePath);
            var filesToCopy = _domainService.SelectFilesForBackup(job, allFiles, strategy);

            _tracker.Initialize(filesToCopy.ToList());

            foreach (var file in filesToCopy)
            {
                try
                {
                    var relativePath = file.Path.Substring(job.SourcePath.Length);
                    var targetFilePath = job.TargetPath + relativePath;

                    var transferStopwatch = Stopwatch.StartNew();
                    var bytesCopied = _fileSystem.CopyFile(file.Path, targetFilePath);
                    transferStopwatch.Stop();

                    filesProcessed++;
                    bytesTransferred += bytesCopied;

                    _tracker.FileProcessed(file);

                    var transferLog = new TransferLog
                    {
                        Timestamp = DateTime.Now,
                        BackupJobName = job.Name,
                        SourceFilePath = _pathAdapter.ToUNC(file.Path),
                        TargetFilePath = _pathAdapter.ToUNC(targetFilePath),
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
                }
            }

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
