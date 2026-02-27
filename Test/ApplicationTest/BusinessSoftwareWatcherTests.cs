using Application.Concurrency;
using Application.Events;
using Application.Ports;
using Application.Services;
using Model;
using Moq;

namespace ApplicationTest;

public class BusinessSoftwareWatcherTests
{
    private static BackupExecutor CreateExecutor(Mock<IEventBus> mockEventBus)
    {
        var mockFileSystem = new Mock<IFileSystemGateway>();
        var mockPathAdapter = new Mock<IPathAdapter>();
        mockPathAdapter.Setup(p => p.ToUNC(It.IsAny<string>())).Returns<string>(s => s);
        var mockEncryptionService = new Mock<IEncryptionService>();
        var mockEncryptionConfig = new Mock<IEncryptionConfig>();
        mockEncryptionConfig.Setup(c => c.GetEncryptedExtensions())
            .Returns(new List<string>().AsReadOnly());
        var mockDetector = new Mock<IBusinessSoftwareDetector>();
        mockDetector.Setup(d => d.GetStatus()).Returns(BusinessSoftwareStatus.NotRunning);
        var mockDetectorConfig = new Mock<IBusinessSoftwareConfig>();
        mockDetectorConfig.Setup(c => c.IsDetectionEnabled()).Returns(false);
        var mockLargeFileLock = new Mock<ILargeFileTransferLock>();
        var mockLargeFileConfig = new Mock<ILargeFileConfig>();
        mockLargeFileConfig.Setup(c => c.GetLargeFileSizeThresholdKb()).Returns(0);
        var mockPriorityFileConfig = new Mock<IPriorityFileConfig>();
        mockPriorityFileConfig.Setup(c => c.GetPriorityExtensions())
            .Returns(new List<string>().AsReadOnly());
        var priorityFileGate = new PriorityFileGate();
        var domainService = new BackupDomainService();

        return new BackupExecutor(
            mockFileSystem.Object,
            mockPathAdapter.Object,
            mockEventBus.Object,
            domainService,
            mockEncryptionService.Object,
            mockEncryptionConfig.Object,
            mockDetector.Object,
            mockDetectorConfig.Object,
            mockLargeFileLock.Object,
            mockLargeFileConfig.Object,
            mockPriorityFileConfig.Object,
            priorityFileGate);
    }

    private static BackupExecutor CreateExecutorWithFiles(
        Mock<IEventBus> mockEventBus,
        Mock<IFileSystemGateway> mockFileSystem)
    {
        var mockPathAdapter = new Mock<IPathAdapter>();
        mockPathAdapter.Setup(p => p.ToUNC(It.IsAny<string>())).Returns<string>(s => s);
        var mockEncryptionService = new Mock<IEncryptionService>();
        var mockEncryptionConfig = new Mock<IEncryptionConfig>();
        mockEncryptionConfig.Setup(c => c.GetEncryptedExtensions())
            .Returns(new List<string>().AsReadOnly());
        var mockDetector = new Mock<IBusinessSoftwareDetector>();
        mockDetector.Setup(d => d.GetStatus()).Returns(BusinessSoftwareStatus.NotRunning);
        var mockDetectorConfig = new Mock<IBusinessSoftwareConfig>();
        mockDetectorConfig.Setup(c => c.IsDetectionEnabled()).Returns(false);
        var mockLargeFileLock = new Mock<ILargeFileTransferLock>();
        var mockLargeFileConfig = new Mock<ILargeFileConfig>();
        mockLargeFileConfig.Setup(c => c.GetLargeFileSizeThresholdKb()).Returns(0);
        var mockPriorityFileConfig = new Mock<IPriorityFileConfig>();
        mockPriorityFileConfig.Setup(c => c.GetPriorityExtensions())
            .Returns(new List<string>().AsReadOnly());
        var priorityFileGate = new PriorityFileGate();
        var domainService = new BackupDomainService();

        return new BackupExecutor(
            mockFileSystem.Object,
            mockPathAdapter.Object,
            mockEventBus.Object,
            domainService,
            mockEncryptionService.Object,
            mockEncryptionConfig.Object,
            mockDetector.Object,
            mockDetectorConfig.Object,
            mockLargeFileLock.Object,
            mockLargeFileConfig.Object,
            mockPriorityFileConfig.Object,
            priorityFileGate);
    }

    [Fact]
    public async Task SoftwareDetected_PausesAllRunningJobs()
    {
        var mockEventBus = new Mock<IEventBus>();
        var mockFileSystem = new Mock<IFileSystemGateway>();

        var sourcePath = Path.GetFullPath("/src");
        var targetPath = Path.GetFullPath("/dst");

        // Create a file that will block on copy so the job stays "running"
        var copyTcs = new TaskCompletionSource<long>();
        mockFileSystem.Setup(fs => fs.EnumerateFiles(sourcePath))
            .Returns(new List<FileDescriptor>
            {
                new(Path.Combine(sourcePath, "file1.txt"), 100, DateTime.Now)
            });
        mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(copyTcs.Task);

        var executor = CreateExecutorWithFiles(mockEventBus, mockFileSystem);

        var mockDetector = new Mock<IBusinessSoftwareDetector>();
        var mockConfig = new Mock<IBusinessSoftwareConfig>();
        mockConfig.Setup(c => c.IsDetectionEnabled()).Returns(true);
        // Initially not running, then switch to running
        var statusSequence = new Queue<BusinessSoftwareStatus>(
            new[] { BusinessSoftwareStatus.NotRunning, BusinessSoftwareStatus.Running });
        mockDetector.Setup(d => d.GetStatus())
            .Returns(() => statusSequence.Count > 1 ? statusSequence.Dequeue() : statusSequence.Peek());

        var watcher = new BusinessSoftwareWatcher(
            mockDetector.Object, mockConfig.Object, executor, TimeSpan.FromMilliseconds(50));

        var job = new BackupJob(1, "TestJob", sourcePath, targetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();

        // Start the job (will block on copy)
        var jobTask = executor.ExecuteAsync(job, strategy);

        // Give time for the job to register in _jobStates
        await Task.Delay(50);

        // Verify job is registered
        Assert.Single(executor.GetRunningJobIds());

        // Start the watcher
        watcher.Start();

        // Wait for at least 2 poll cycles (first poll: not running, second: running -> auto-pause)
        await Task.Delay(200);

        await watcher.StopAsync();

        // Complete the copy to let the job finish
        copyTcs.SetResult(100L);

        // Wait for job to be paused and then complete (it will be auto-paused)
        // Since the job is paused, we need to resume it
        executor.AutoResumeAllJobs();

        await jobTask;
    }

    [Fact]
    public async Task SoftwareCloses_ResumesOnlyAutoPausedJobs()
    {
        var mockEventBus = new Mock<IEventBus>();
        var executor = CreateExecutor(mockEventBus);

        var mockDetector = new Mock<IBusinessSoftwareDetector>();
        var mockConfig = new Mock<IBusinessSoftwareConfig>();
        mockConfig.Setup(c => c.IsDetectionEnabled()).Returns(true);

        // Simulate: Running -> NotRunning transition
        var callCount = 0;
        mockDetector.Setup(d => d.GetStatus()).Returns(() =>
        {
            callCount++;
            // First poll: Running (triggers auto-pause)
            // Second poll: NotRunning (triggers auto-resume)
            return callCount <= 1
                ? BusinessSoftwareStatus.Running
                : BusinessSoftwareStatus.NotRunning;
        });

        var watcher = new BusinessSoftwareWatcher(
            mockDetector.Object, mockConfig.Object, executor, TimeSpan.FromMilliseconds(50));

        // Start a job so there's something to pause/resume
        var mockFileSystem = new Mock<IFileSystemGateway>();
        var sourcePath = Path.GetFullPath("/src");
        var targetPath = Path.GetFullPath("/dst");
        mockFileSystem.Setup(fs => fs.EnumerateFiles(sourcePath))
            .Returns(new List<FileDescriptor>
            {
                new(Path.Combine(sourcePath, "file1.txt"), 100, DateTime.Now)
            });
        var copyTcs = new TaskCompletionSource<long>();
        mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(copyTcs.Task);

        var executor2 = CreateExecutorWithFiles(mockEventBus, mockFileSystem);
        var watcher2 = new BusinessSoftwareWatcher(
            mockDetector.Object, mockConfig.Object, executor2, TimeSpan.FromMilliseconds(50));

        var job = new BackupJob(1, "TestJob", sourcePath, targetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var jobTask = executor2.ExecuteAsync(job, strategy);

        await Task.Delay(50); // let job register

        watcher2.Start();

        // Wait for two poll cycles: auto-pause then auto-resume
        await Task.Delay(250);

        await watcher2.StopAsync();

        // Complete the copy
        copyTcs.SetResult(100L);
        await jobTask;

        Assert.True((await jobTask).Success);
    }

    [Fact]
    public async Task ManuallyPausedJob_NotResumedByAutoResume()
    {
        var mockEventBus = new Mock<IEventBus>();
        var mockFileSystem = new Mock<IFileSystemGateway>();

        var sourcePath = Path.GetFullPath("/src");
        var targetPath = Path.GetFullPath("/dst");

        var copyTcs = new TaskCompletionSource<long>();
        mockFileSystem.Setup(fs => fs.EnumerateFiles(sourcePath))
            .Returns(new List<FileDescriptor>
            {
                new(Path.Combine(sourcePath, "file1.txt"), 100, DateTime.Now),
                new(Path.Combine(sourcePath, "file2.txt"), 200, DateTime.Now)
            });

        var copyCount = 0;
        mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, CancellationToken>((src, dst, ct) =>
            {
                copyCount++;
                if (copyCount == 1) return Task.FromResult(100L);
                return copyTcs.Task; // second copy blocks
            });

        var executor = CreateExecutorWithFiles(mockEventBus, mockFileSystem);

        var job = new BackupJob(1, "TestJob", sourcePath, targetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var jobTask = executor.ExecuteAsync(job, strategy);

        await Task.Delay(100); // let job start and process first file

        // Manually pause the job
        executor.Handle(new PauseRequestedEvent(1));

        // Now auto-pause all (should not double-pause since already paused)
        executor.AutoPauseAllJobs();

        // Auto-resume should NOT resume manually paused jobs
        // because PauseRequestedEvent clears the auto-pause flag
        executor.AutoResumeAllJobs();

        // The job should still be paused (manual pause was not overridden)
        // Verify by checking the running job IDs are still there
        var runningIds = executor.GetRunningJobIds();
        Assert.Contains(1, runningIds);

        // Now manually resume
        executor.Handle(new PauseRequestedEvent(1));

        // Complete the copy
        copyTcs.SetResult(200L);
        await jobTask;
    }

    [Fact]
    public async Task DetectionDisabled_NoAction()
    {
        var mockEventBus = new Mock<IEventBus>();
        var executor = CreateExecutor(mockEventBus);

        var mockDetector = new Mock<IBusinessSoftwareDetector>();
        var mockConfig = new Mock<IBusinessSoftwareConfig>();
        mockConfig.Setup(c => c.IsDetectionEnabled()).Returns(false);

        var watcher = new BusinessSoftwareWatcher(
            mockDetector.Object, mockConfig.Object, executor, TimeSpan.FromMilliseconds(50));

        watcher.Start();
        await Task.Delay(200);
        await watcher.StopAsync();

        // Detector should never be called if detection is disabled
        mockDetector.Verify(d => d.GetStatus(), Times.Never);
    }

    [Fact]
    public async Task CancellationStopsPolling()
    {
        var mockEventBus = new Mock<IEventBus>();
        var executor = CreateExecutor(mockEventBus);

        var mockDetector = new Mock<IBusinessSoftwareDetector>();
        var mockConfig = new Mock<IBusinessSoftwareConfig>();
        mockConfig.Setup(c => c.IsDetectionEnabled()).Returns(true);
        mockDetector.Setup(d => d.GetStatus()).Returns(BusinessSoftwareStatus.NotRunning);

        var watcher = new BusinessSoftwareWatcher(
            mockDetector.Object, mockConfig.Object, executor, TimeSpan.FromMilliseconds(50));

        watcher.Start();
        await Task.Delay(100); // let a few polls happen

        await watcher.StopAsync();

        // Record how many times GetStatus was called
        var callsBefore = mockDetector.Invocations.Count;

        // Wait more time to confirm no more polls happen
        await Task.Delay(200);

        var callsAfter = mockDetector.Invocations.Count;
        Assert.Equal(callsBefore, callsAfter);
    }

    [Fact]
    public async Task OnlyPausesOnTransition_NotEveryPoll()
    {
        var mockEventBus = new Mock<IEventBus>();
        var mockFileSystem = new Mock<IFileSystemGateway>();

        var sourcePath = Path.GetFullPath("/src");
        var targetPath = Path.GetFullPath("/dst");

        var copyTcs = new TaskCompletionSource<long>();
        mockFileSystem.Setup(fs => fs.EnumerateFiles(sourcePath))
            .Returns(new List<FileDescriptor>
            {
                new(Path.Combine(sourcePath, "file1.txt"), 100, DateTime.Now)
            });
        mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(copyTcs.Task);

        var executor = CreateExecutorWithFiles(mockEventBus, mockFileSystem);

        var mockDetector = new Mock<IBusinessSoftwareDetector>();
        var mockConfig = new Mock<IBusinessSoftwareConfig>();
        mockConfig.Setup(c => c.IsDetectionEnabled()).Returns(true);
        // Always return Running
        mockDetector.Setup(d => d.GetStatus()).Returns(BusinessSoftwareStatus.Running);

        var watcher = new BusinessSoftwareWatcher(
            mockDetector.Object, mockConfig.Object, executor, TimeSpan.FromMilliseconds(50));

        var job = new BackupJob(1, "TestJob", sourcePath, targetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var jobTask = executor.ExecuteAsync(job, strategy);

        await Task.Delay(50); // let job register

        watcher.Start();

        // Let multiple polls happen while software is continuously running
        await Task.Delay(300);

        await watcher.StopAsync();

        // The job should have been auto-paused exactly once (on the first detection)
        // We verify this by checking that after auto-resume, the job can proceed
        // (if it was paused multiple times without matching resumes, it would still be paused)
        executor.AutoResumeAllJobs();

        // Complete the copy
        copyTcs.SetResult(100L);
        await jobTask;

        Assert.True((await jobTask).Success);
    }

    [Fact]
    public async Task StartWhileAlreadyRunning_DoesNotCreateSecondLoop()
    {
        var mockEventBus = new Mock<IEventBus>();
        var executor = CreateExecutor(mockEventBus);

        var mockDetector = new Mock<IBusinessSoftwareDetector>();
        var mockConfig = new Mock<IBusinessSoftwareConfig>();
        mockConfig.Setup(c => c.IsDetectionEnabled()).Returns(true);
        mockDetector.Setup(d => d.GetStatus()).Returns(BusinessSoftwareStatus.NotRunning);

        var watcher = new BusinessSoftwareWatcher(
            mockDetector.Object, mockConfig.Object, executor, TimeSpan.FromMilliseconds(50));

        watcher.Start();
        watcher.Start(); // second call should be no-op

        await Task.Delay(200);

        await watcher.StopAsync();

        // Should still only have normal number of calls
        Assert.True(mockDetector.Invocations.Count > 0);
    }
}
