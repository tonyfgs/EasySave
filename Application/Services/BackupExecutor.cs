using System.Collections.Concurrent;
using System.Diagnostics;
using Application.Concurrency;
using Application.DTOs;
using Application.Events;
using Application.Ports;
using Model;

namespace Application.Services;

public class BackupExecutor :
    IEventHandler<PauseRequestedEvent>,
    IEventHandler<StopRequestedEvent>
{
    private readonly IFileSystemGateway _fileSystem;
    private readonly IPathAdapter _pathAdapter;
    private readonly IEventBus _eventBus;
    private readonly BackupDomainService _domainService;
    private readonly IEncryptionService _encryptionService;
    private readonly IEncryptionConfig _encryptionConfig;
    private readonly IBusinessSoftwareDetector _businessSoftwareDetector;
    private readonly IBusinessSoftwareConfig _businessSoftwareConfig;
    private readonly ILargeFileTransferLock _largeFileLock;
    private readonly ILargeFileConfig _largeFileConfig;

    // Per-job execution state: keyed by job ID, safe for concurrent ExecuteAsync calls
    private sealed class PerJobState
    {
        public AsyncManualResetEvent PauseGate { get; } = new(initialState: true); // open = running
        public CancellationTokenSource StopCts { get; } = new();
        public volatile bool StopRequested;
    }

    private readonly ConcurrentDictionary<int, PerJobState> _jobStates = new();

    public BackupExecutor(
        IFileSystemGateway fileSystem,
        IPathAdapter pathAdapter,
        IEventBus eventBus,
        BackupDomainService domainService,
        IEncryptionService encryptionService,
        IEncryptionConfig encryptionConfig,
        IBusinessSoftwareDetector businessSoftwareDetector,
        IBusinessSoftwareConfig businessSoftwareConfig,
        ILargeFileTransferLock largeFileLock,
        ILargeFileConfig largeFileConfig)
    {
        _fileSystem = fileSystem;
        _pathAdapter = pathAdapter;
        _eventBus = eventBus;
        _domainService = domainService;
        _encryptionService = encryptionService;
        _encryptionConfig = encryptionConfig;
        _businessSoftwareDetector = businessSoftwareDetector;
        _businessSoftwareConfig = businessSoftwareConfig;
        _largeFileLock = largeFileLock;
        _largeFileConfig = largeFileConfig;

        _eventBus.Subscribe<PauseRequestedEvent>(this);
        _eventBus.Subscribe<StopRequestedEvent>(this);
    }

    public void Handle(PauseRequestedEvent e)
    {
        if (!_jobStates.TryGetValue(e.JobId, out var state)) return;
        // Toggle: gate IsSet = running, !IsSet = paused
        if (state.PauseGate.IsSet)
            state.PauseGate.Reset();
        else
            state.PauseGate.Set();
    }

    public void Handle(StopRequestedEvent e)
    {
        if (!_jobStates.TryGetValue(e.JobId, out var state)) return;
        state.StopRequested = true;
        state.StopCts.Cancel();  // cancel in-progress async I/O
        state.PauseGate.Set();   // unblock if currently waiting on pause
    }

    public async Task<BackupResult> ExecuteAsync(
        BackupJob job,
        IBackupStrategy strategy,
        CancellationToken ct = default)
    {
        var perJobState = new PerJobState();
        _jobStates[job.Id] = perJobState;

        // jobCt fires on either outer cancellation OR per-job stop
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, perJobState.StopCts.Token);
        var jobCt = linkedCts.Token;

        var tracker = new ProgressTracker();
        var stopwatch = Stopwatch.StartNew();
        var errors = new List<string>();
        int filesProcessed = 0;
        long bytesTransferred = 0;
        bool blockedByBusinessSoftware = false;

        try
        {
            ct.ThrowIfCancellationRequested();

            var sourcePath = Path.GetFullPath(job.SourcePath);
            var targetPath = Path.GetFullPath(job.TargetPath);

            _fileSystem.EnsureDirectory(targetPath);

            var allFiles = _fileSystem.EnumerateFiles(sourcePath);
            var filesToCopy = _domainService.SelectFilesForBackup(job, allFiles, strategy);

            tracker.Initialize(filesToCopy.ToList());

            foreach (var file in filesToCopy)
            {
                // --- Stop check ---
                if (perJobState.StopRequested)
                {
                    tracker.SetState(JobState.Stopping);
                    tracker.ClearCurrentFile();
                    _eventBus.Publish(new StateChangedEvent(tracker.BuildSnapshot(job.Name)));
                    break;
                }

                // --- Pause gate (async, cancellable) ---
                if (!perJobState.PauseGate.IsSet)
                {
                    tracker.SetState(JobState.Paused);
                    tracker.ClearCurrentFile();
                    _eventBus.Publish(new StateChangedEvent(tracker.BuildSnapshot(job.Name)));

                    await perJobState.PauseGate.WaitAsync(jobCt).ConfigureAwait(false);

                    if (perJobState.StopRequested)
                    {
                        tracker.SetState(JobState.Stopping);
                        _eventBus.Publish(new StateChangedEvent(tracker.BuildSnapshot(job.Name)));
                        break;
                    }

                    tracker.SetState(JobState.Active);
                    _eventBus.Publish(new StateChangedEvent(tracker.BuildSnapshot(job.Name)));
                }

                var relativePath = Path.GetRelativePath(sourcePath, file.Path);
                var targetFilePath = Path.Combine(targetPath, relativePath);

                tracker.SetCurrentFile(
                    _pathAdapter.ToUNC(file.Path),
                    _pathAdapter.ToUNC(targetFilePath));

                // --- Large file throttle ---
                bool acquiredLargeFileLock = false;
                var thresholdBytes = _largeFileConfig.GetLargeFileSizeThresholdKb() * 1024L;
                if (thresholdBytes > 0 && file.Size > thresholdBytes)
                {
                    tracker.SetState(JobState.Blocked);
                    tracker.SetBlockReason("Waiting for large file transfer slot");
                    _eventBus.Publish(new StateChangedEvent(tracker.BuildSnapshot(job.Name)));
                    await _largeFileLock.AcquireAsync(jobCt).ConfigureAwait(false);
                    acquiredLargeFileLock = true; // set only after successful acquire
                    tracker.SetState(JobState.Active);
                    tracker.SetBlockReason(null);
                }

                try
                {
                    var transferStopwatch = Stopwatch.StartNew();
                    var bytesCopied = await _fileSystem.CopyFileAsync(file.Path, targetFilePath, jobCt).ConfigureAwait(false);
                    transferStopwatch.Stop();

                    filesProcessed++;
                    bytesTransferred += bytesCopied;

                    long encryptionTimeMs = 0;
                    var encryptedExtensions = _encryptionConfig.GetEncryptedExtensions();
                    var fileExtension = Path.GetExtension(file.Path);
                    if (encryptedExtensions.Any(ext =>
                            ext.Equals(fileExtension, StringComparison.OrdinalIgnoreCase)))
                    {
                        try
                        {
                            var cryptoResult = _encryptionService.EncryptFile(targetFilePath);
                            if (cryptoResult.Success)
                            {
                                encryptionTimeMs = cryptoResult.DurationMs;
                            }
                            else
                            {
                                encryptionTimeMs = -((long)cryptoResult.ErrorCode + 1);
                                errors.Add($"Encryption failed for {file.Path}: {cryptoResult.ErrorMessage}");
                            }
                        }
                        catch (Exception ex)
                        {
                            encryptionTimeMs = -((long)CryptoErrorCode.Unknown + 1);
                            errors.Add($"Encryption failed for {file.Path}: {ex.Message}");
                        }
                    }

                    tracker.FileProcessed(file);

                    var transferLog = new TransferLog
                    {
                        Timestamp = DateTime.UtcNow,
                        BackupName = job.Name,
                        SourcePath = _pathAdapter.ToUNC(file.Path),
                        DestPath = _pathAdapter.ToUNC(targetFilePath),
                        FileSize = file.Size,
                        TransferTimeMs = transferStopwatch.ElapsedMilliseconds,
                        EncryptionTimeMs = encryptionTimeMs
                    };
                    _eventBus.Publish(new TransferCompletedEvent(transferLog));

                    var snapshot = tracker.BuildSnapshot(job.Name);
                    _eventBus.Publish(new StateChangedEvent(snapshot));

                    // In-flight business software detection
                    if (_businessSoftwareConfig.IsDetectionEnabled())
                    {
                        var businessStatus = _businessSoftwareDetector.GetStatus();
                        if (businessStatus.IsBlocking())
                        {
                            var blockReason = $"Business software detected ({businessStatus})";
                            errors.Add(blockReason);
                            blockedByBusinessSoftware = true;

                            tracker.SetState(JobState.Blocked);
                            tracker.SetBlockReason(blockReason);
                            tracker.ClearCurrentFile();
                            var blockedSnapshot = tracker.BuildSnapshot(job.Name);
                            _eventBus.Publish(new StateChangedEvent(blockedSnapshot));
                            _eventBus.Publish(new BusinessSoftwareDetectedEvent(
                                job.Name, businessStatus, DateTime.UtcNow));
                            break;
                        }
                    }
                }
                catch (OperationCanceledException) when (perJobState.StopRequested)
                {
                    // Stop was requested mid-copy — break out cleanly without re-throwing
                    break;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    errors.Add($"Failed to copy {file.Path}: {ex.Message}");

                    var errorLog = new TransferLog
                    {
                        Timestamp = DateTime.UtcNow,
                        BackupName = job.Name,
                        SourcePath = _pathAdapter.ToUNC(file.Path),
                        DestPath = _pathAdapter.ToUNC(targetFilePath),
                        FileSize = file.Size,
                        TransferTimeMs = -1,
                        EncryptionTimeMs = 0
                    };
                    _eventBus.Publish(new TransferCompletedEvent(errorLog));

                    tracker.FileProcessed(file);
                    var snapshot = tracker.BuildSnapshot(job.Name);
                    _eventBus.Publish(new StateChangedEvent(snapshot));
                }
                finally
                {
                    if (acquiredLargeFileLock)
                        _largeFileLock.Release();
                }
            }

            if (perJobState.StopRequested && !blockedByBusinessSoftware)
            {
                tracker.SetState(JobState.End);
                tracker.ClearCurrentFile();
                _eventBus.Publish(new StateChangedEvent(tracker.BuildSnapshot(job.Name)));
            }
            else if (!blockedByBusinessSoftware)
            {
                tracker.SetState(errors.Count > 0 ? JobState.Error : JobState.End);
                tracker.ClearCurrentFile();
                var endSnapshot = tracker.BuildSnapshot(job.Name);
                _eventBus.Publish(new StateChangedEvent(endSnapshot));

                if (strategy is FullBackupStrategy)
                {
                    job.MarkFullBackupCompleted(DateTime.UtcNow);
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            errors.Add($"Backup execution failed: {ex.Message}");
        }
        finally
        {
            _jobStates.TryRemove(job.Id, out _);
            perJobState.StopCts.Dispose();
        }

        stopwatch.Stop();

        if (errors.Count > 0)
            return BackupResult.Fail(errors, stopwatch.Elapsed);

        return BackupResult.Ok(filesProcessed, bytesTransferred, stopwatch.Elapsed);
    }
}
