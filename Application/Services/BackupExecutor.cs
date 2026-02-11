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
    private readonly IEncryptionService _encryptionService;
    private readonly IEncryptionConfig _encryptionConfig;
    private readonly IBusinessSoftwareDetector _businessSoftwareDetector;

    public BackupExecutor(
        IFileSystemGateway fileSystem,
        IPathAdapter pathAdapter,
        IEventBus eventBus,
        BackupDomainService domainService,
        ProgressTracker tracker,
        IEncryptionService encryptionService,
        IEncryptionConfig encryptionConfig,
        IBusinessSoftwareDetector businessSoftwareDetector)
    {
        _fileSystem = fileSystem;
        _pathAdapter = pathAdapter;
        _eventBus = eventBus;
        _domainService = domainService;
        _tracker = tracker;
        _encryptionService = encryptionService;
        _encryptionConfig = encryptionConfig;
        _businessSoftwareDetector = businessSoftwareDetector;
    }

    public BackupResult Execute(BackupJob job, IBackupStrategy strategy)
    {
        var stopwatch = Stopwatch.StartNew();
        var errors = new List<string>();
        int filesProcessed = 0;
        long bytesTransferred = 0;
        bool blockedByBusinessSoftware = false;

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

                    long encryptionTimeMs = 0;
                    var encryptedExtensions = _encryptionConfig.GetEncryptedExtensions();
                    var fileExtension = Path.GetExtension(file.Path);
                    if (encryptedExtensions.Any(ext =>
                            ext.Equals(fileExtension, StringComparison.OrdinalIgnoreCase)))
                    {
                        var cryptoResult = _encryptionService.EncryptFile(targetFilePath);
                        if (cryptoResult.Success)
                        {
                            encryptionTimeMs = cryptoResult.DurationMs;
                        }
                        else
                        {
                            encryptionTimeMs = -1;
                            errors.Add($"Encryption failed for {file.Path}: {cryptoResult.ErrorMessage}");
                        }
                    }

                    _tracker.FileProcessed(file);

                    var transferLog = new TransferLog
                    {
                        Timestamp = DateTime.Now,
                        BackupName = job.Name,
                        SourcePath = _pathAdapter.ToUNC(file.Path),
                        DestPath = _pathAdapter.ToUNC(targetFilePath),
                        FileSize = file.Size,
                        TransferTimeMs = transferStopwatch.ElapsedMilliseconds,
                        EncryptionTimeMs = encryptionTimeMs
                    };
                    _eventBus.Publish(new TransferCompletedEvent(transferLog));

                    var snapshot = _tracker.BuildSnapshot(job.Name);
                    _eventBus.Publish(new StateChangedEvent(snapshot));

                    // In-flight business software detection
                    var businessStatus = _businessSoftwareDetector.GetStatus();
                    if (businessStatus is BusinessSoftwareStatus.Running
                        or BusinessSoftwareStatus.Unknown
                        or BusinessSoftwareStatus.Error)
                    {
                        var blockReason = "Business software detected";
                        errors.Add(blockReason);
                        blockedByBusinessSoftware = true;

                        _tracker.SetState(JobState.Blocked);
                        _tracker.SetBlockReason(blockReason);
                        _tracker.ClearCurrentFile();
                        var blockedSnapshot = _tracker.BuildSnapshot(job.Name);
                        _eventBus.Publish(new StateChangedEvent(blockedSnapshot));
                        _eventBus.Publish(new BusinessSoftwareDetectedEvent(
                            job.Name, businessStatus, DateTime.Now));
                        break;
                    }
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
                        TransferTimeMs = -1,
                        EncryptionTimeMs = 0
                    };
                    _eventBus.Publish(new TransferCompletedEvent(errorLog));

                    _tracker.FileProcessed(file);
                    var snapshot = _tracker.BuildSnapshot(job.Name);
                    _eventBus.Publish(new StateChangedEvent(snapshot));
                }
            }

            if (!blockedByBusinessSoftware)
            {
                _tracker.SetState(errors.Count > 0 ? JobState.Error : JobState.End);
                _tracker.ClearCurrentFile();
                var endSnapshot = _tracker.BuildSnapshot(job.Name);
                _eventBus.Publish(new StateChangedEvent(endSnapshot));

                if (strategy is FullBackupStrategy)
                {
                    job.MarkFullBackupCompleted(DateTime.Now);
                }
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
