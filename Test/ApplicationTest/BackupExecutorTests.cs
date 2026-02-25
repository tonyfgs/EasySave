using Application.Concurrency;
using Application.Events;
using Application.Ports;
using Application.Services;
using Model;
using Moq;

namespace ApplicationTest;

public class BackupExecutorTests
{
    private static readonly string SourcePath = Path.GetFullPath("/src");
    private static readonly string TargetPath = Path.GetFullPath("/dst");

    private readonly Mock<IFileSystemGateway> _mockFileSystem;
    private readonly Mock<IPathAdapter> _mockPathAdapter;
    private readonly Mock<IEventBus> _mockEventBus;
    private readonly Mock<IEncryptionService> _mockEncryptionService;
    private readonly Mock<IEncryptionConfig> _mockEncryptionConfig;
    private readonly Mock<IBusinessSoftwareDetector> _mockDetector;
    private readonly Mock<IBusinessSoftwareConfig> _mockDetectorConfig;
    private readonly BackupDomainService _domainService;
    private readonly ProgressTracker _tracker;
    private readonly BackupExecutor _executor;

    public BackupExecutorTests()
    {
        _mockFileSystem = new Mock<IFileSystemGateway>();
        _mockPathAdapter = new Mock<IPathAdapter>();
        _mockEventBus = new Mock<IEventBus>();
        _mockEncryptionService = new Mock<IEncryptionService>();
        _mockEncryptionConfig = new Mock<IEncryptionConfig>();
        _mockDetector = new Mock<IBusinessSoftwareDetector>();
        _mockDetectorConfig = new Mock<IBusinessSoftwareConfig>();
        _domainService = new BackupDomainService();
        _tracker = new ProgressTracker();

        _mockPathAdapter.Setup(p => p.ToUNC(It.IsAny<string>())).Returns<string>(s => s);
        _mockEncryptionConfig.Setup(c => c.GetEncryptedExtensions())
            .Returns(new List<string>().AsReadOnly());
        _mockDetector.Setup(d => d.GetStatus()).Returns(BusinessSoftwareStatus.NotRunning);
        _mockDetectorConfig.Setup(c => c.IsDetectionEnabled()).Returns(false);

        _executor = new BackupExecutor(
            _mockFileSystem.Object,
            _mockPathAdapter.Object,
            _mockEventBus.Object,
            _domainService,
            _tracker,
            _mockEncryptionService.Object,
            _mockEncryptionConfig.Object,
            _mockDetector.Object,
            _mockDetectorConfig.Object);
    }

    [Fact]
    public async Task ExecuteAsync_FullBackup_ShouldCopyAllFiles()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "file1.txt"), 100, DateTime.Now),
            new(Path.Combine(SourcePath, "file2.txt"), 200, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(100);

        var result = await _executor.ExecuteAsync(job, strategy, _pauseGate);

        Assert.True(result.Success);
        Assert.Equal(2, result.FilesProcessed);
        _mockFileSystem.Verify(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPublishTransferCompletedEventPerFile()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "file1.txt"), 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(100);

        await _executor.ExecuteAsync(job, strategy, _pauseGate);

        _mockEventBus.Verify(bus => bus.Publish(It.IsAny<TransferCompletedEvent>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPublishStateChangedEvents()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "file1.txt"), 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(100);

        await _executor.ExecuteAsync(job, strategy, _pauseGate);

        _mockEventBus.Verify(bus => bus.Publish(It.IsAny<StateChangedEvent>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldEnsureTargetDirectoryExists()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(new List<FileDescriptor>());

        await _executor.ExecuteAsync(job, strategy, _pauseGate);

        _mockFileSystem.Verify(fs => fs.EnsureDirectory(TargetPath), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_NoFiles_ShouldReturnSuccessWithZeroFiles()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(new List<FileDescriptor>());

        var result = await _executor.ExecuteAsync(job, strategy, _pauseGate);

        Assert.True(result.Success);
        Assert.Equal(0, result.FilesProcessed);
    }

    [Fact]
    public async Task ExecuteAsync_CopyFails_ShouldReturnFailureResult()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "file1.txt"), 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Copy failed"));

        var result = await _executor.ExecuteAsync(job, strategy, _pauseGate);

        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldConvertPathsToUNC()
    {
        var srcFile = Path.Combine(SourcePath, "file1.txt");
        var dstFile = Path.Combine(TargetPath, "file1.txt");
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(srcFile, 100, DateTime.Now)
        };

        _mockPathAdapter.Setup(p => p.ToUNC(srcFile)).Returns("\\\\server\\src\\file1.txt");
        _mockPathAdapter.Setup(p => p.ToUNC(dstFile)).Returns("\\\\server\\dst\\file1.txt");
        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(100);

        await _executor.ExecuteAsync(job, strategy, _pauseGate);

        _mockEventBus.Verify(bus => bus.Publish(It.Is<TransferCompletedEvent>(
            e => e.Transfer.SourcePath.Contains("\\\\"))), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldIncludeCurrentFileInStateSnapshot()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "file1.txt"), 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(100);

        StateChangedEvent? capturedEvent = null;
        _mockEventBus.Setup(bus => bus.Publish(It.IsAny<StateChangedEvent>()))
            .Callback<StateChangedEvent>(e =>
            {
                if (e.Snapshot.State == JobState.Active)
                    capturedEvent = e;
            });

        await _executor.ExecuteAsync(job, strategy, _pauseGate);

        Assert.NotNull(capturedEvent);
        Assert.NotEqual(string.Empty, capturedEvent.Snapshot.CurrentSourceFile);
        Assert.NotEqual(string.Empty, capturedEvent.Snapshot.CurrentDestFile);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPublishEndStateAfterCompletion()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "file1.txt"), 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(100);

        var capturedEvents = new List<StateChangedEvent>();
        _mockEventBus.Setup(bus => bus.Publish(It.IsAny<StateChangedEvent>()))
            .Callback<StateChangedEvent>(e => capturedEvents.Add(e));

        await _executor.ExecuteAsync(job, strategy, _pauseGate);

        var lastSnapshot = capturedEvents.Last().Snapshot;
        Assert.Equal(JobState.End, lastSnapshot.State);
        Assert.Equal(string.Empty, lastSnapshot.CurrentSourceFile);
    }

    [Fact]
    public async Task ExecuteAsync_CopyFails_ShouldPublishTransferEventWithNegativeTime()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "file1.txt"), 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Disk full"));

        TransferCompletedEvent? capturedEvent = null;
        _mockEventBus.Setup(bus => bus.Publish(It.IsAny<TransferCompletedEvent>()))
            .Callback<TransferCompletedEvent>(e => capturedEvent = e);

        await _executor.ExecuteAsync(job, strategy, _pauseGate);

        Assert.NotNull(capturedEvent);
        Assert.True(capturedEvent.Transfer.TransferTimeMs < 0);
    }

    [Fact]
    public async Task ExecuteAsync_SourcePathWithTrailingSlash_ShouldConstructCorrectTargetPath()
    {
        var srcWithSlash = SourcePath + Path.DirectorySeparatorChar;
        var dstWithSlash = TargetPath + Path.DirectorySeparatorChar;
        var normalizedSrc = Path.GetFullPath(srcWithSlash);
        var job = new BackupJob(1, "TestJob", srcWithSlash, dstWithSlash, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "subdir", "file1.txt"), 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(normalizedSrc)).Returns(files);

        string? capturedTarget = null;
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((src, tgt, ct) => capturedTarget = tgt)
            .ReturnsAsync(100);

        await _executor.ExecuteAsync(job, strategy, _pauseGate);

        Assert.NotNull(capturedTarget);
        Assert.Contains("subdir", capturedTarget);
        Assert.Contains("file1.txt", capturedTarget);
    }

    [Fact]
    public async Task ExecuteAsync_SourcePathWithoutTrailingSlash_ShouldConstructCorrectTargetPath()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "file1.txt"), 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);

        string? capturedTarget = null;
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((src, tgt, ct) => capturedTarget = tgt)
            .ReturnsAsync(100);

        await _executor.ExecuteAsync(job, strategy, _pauseGate);

        Assert.NotNull(capturedTarget);
        Assert.EndsWith("file1.txt", capturedTarget);
    }

    [Fact]
    public async Task ExecuteAsync_EmptySourceDir_ShouldPublishEndStateEvent()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(new List<FileDescriptor>());

        var capturedEvents = new List<StateChangedEvent>();
        _mockEventBus.Setup(bus => bus.Publish(It.IsAny<StateChangedEvent>()))
            .Callback<StateChangedEvent>(e => capturedEvents.Add(e));

        var result = await _executor.ExecuteAsync(job, strategy, _pauseGate);

        Assert.True(result.Success);
        Assert.NotEmpty(capturedEvents);
        Assert.Equal(JobState.End, capturedEvents.Last().Snapshot.State);
    }

    [Fact]
    public async Task ExecuteAsync_CopyFails_TransferTimeMs_ShouldBeExactlyMinusOne()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "file1.txt"), 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Disk full"));

        TransferCompletedEvent? capturedEvent = null;
        _mockEventBus.Setup(bus => bus.Publish(It.IsAny<TransferCompletedEvent>()))
            .Callback<TransferCompletedEvent>(e => capturedEvent = e);

        await _executor.ExecuteAsync(job, strategy, _pauseGate);

        Assert.NotNull(capturedEvent);
        Assert.Equal(-1, capturedEvent.Transfer.TransferTimeMs);
    }

    [Fact]
    public async Task ExecuteAsync_FirstFileFails_ShouldContinueProcessingSecondFile()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "file1.txt"), 100, DateTime.Now),
            new(Path.Combine(SourcePath, "file2.txt"), 200, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.SetupSequence(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("File locked"))
            .ReturnsAsync(200);

        var transferEvents = new List<TransferCompletedEvent>();
        _mockEventBus.Setup(bus => bus.Publish(It.IsAny<TransferCompletedEvent>()))
            .Callback<TransferCompletedEvent>(e => transferEvents.Add(e));

        var result = await _executor.ExecuteAsync(job, strategy, _pauseGate);

        _mockFileSystem.Verify(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        Assert.Equal(2, transferEvents.Count);
        Assert.Equal(-1, transferEvents[0].Transfer.TransferTimeMs);
        Assert.True(transferEvents[1].Transfer.TransferTimeMs >= 0);
    }

    [Fact]
    public async Task ExecuteAsync_CopyFails_ErrorLog_ShouldContainCorrectPathsAndSize()
    {
        var srcFile = Path.Combine(SourcePath, "data.bin");
        var dstFile = Path.Combine(TargetPath, "data.bin");
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(srcFile, 4096, DateTime.Now)
        };

        _mockPathAdapter.Setup(p => p.ToUNC(srcFile)).Returns("\\\\server\\src\\data.bin");
        _mockPathAdapter.Setup(p => p.ToUNC(dstFile)).Returns("\\\\server\\dst\\data.bin");
        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Access denied"));

        TransferCompletedEvent? capturedEvent = null;
        _mockEventBus.Setup(bus => bus.Publish(It.IsAny<TransferCompletedEvent>()))
            .Callback<TransferCompletedEvent>(e => capturedEvent = e);

        await _executor.ExecuteAsync(job, strategy, _pauseGate);

        Assert.NotNull(capturedEvent);
        Assert.Equal("\\\\server\\src\\data.bin", capturedEvent.Transfer.SourcePath);
        Assert.Equal("\\\\server\\dst\\data.bin", capturedEvent.Transfer.DestPath);
        Assert.Equal(4096, capturedEvent.Transfer.FileSize);
        Assert.Equal(-1, capturedEvent.Transfer.TransferTimeMs);
    }

    [Fact]
    public async Task ExecuteAsync_CopyFails_ShouldPublishErrorState()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "file1.txt"), 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Disk full"));

        var capturedEvents = new List<StateChangedEvent>();
        _mockEventBus.Setup(bus => bus.Publish(It.IsAny<StateChangedEvent>()))
            .Callback<StateChangedEvent>(e => capturedEvents.Add(e));

        await _executor.ExecuteAsync(job, strategy, _pauseGate);

        var lastSnapshot = capturedEvents.Last().Snapshot;
        Assert.Equal(JobState.Error, lastSnapshot.State);
        Assert.Equal(string.Empty, lastSnapshot.CurrentSourceFile);
    }

    [Fact]
    public async Task ExecuteAsync_PartialFailure_ShouldPublishErrorState()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "file1.txt"), 100, DateTime.Now),
            new(Path.Combine(SourcePath, "file2.txt"), 200, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.SetupSequence(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(100)
            .ThrowsAsync(new IOException("Disk full"));

        var capturedEvents = new List<StateChangedEvent>();
        _mockEventBus.Setup(bus => bus.Publish(It.IsAny<StateChangedEvent>()))
            .Callback<StateChangedEvent>(e => capturedEvents.Add(e));

        await _executor.ExecuteAsync(job, strategy, _pauseGate);

        var lastSnapshot = capturedEvents.Last().Snapshot;
        Assert.Equal(JobState.Error, lastSnapshot.State);
    }

    [Fact]
    public async Task ExecuteAsync_NestedSubdir_ShouldPreserveExactRelativePath()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "a", "b", "c.txt"), 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);

        string? capturedTarget = null;
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((src, tgt, ct) => capturedTarget = tgt)
            .ReturnsAsync(100);

        await _executor.ExecuteAsync(job, strategy, _pauseGate);

        Assert.NotNull(capturedTarget);
        var expectedTarget = Path.Combine(TargetPath, "a", "b", "c.txt");
        Assert.Equal(expectedTarget, capturedTarget);
    }

    [Fact]
    public async Task ExecuteAsync_TrailingSlashInPaths_ShouldNormalizeAndCopySuccessfully()
    {
        var sourcePath = Path.GetFullPath("/src/");
        var targetPath = Path.GetFullPath("/dst/");
        var job = new BackupJob(1, "TestJob", "/src/", "/dst/", BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(sourcePath, "file1.txt"), 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(sourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(100);

        var result = await _executor.ExecuteAsync(job, strategy, _pauseGate);

        Assert.True(result.Success);
        Assert.Equal(1, result.FilesProcessed);
    }

    [Fact]
    public async Task ExecuteAsync_PathNormalization_ShouldResolveRelativeSegments()
    {
        var sourcePath = Path.GetFullPath("/src");
        var targetPath = Path.GetFullPath("/dst");
        var job = new BackupJob(1, "TestJob", "/src/../src", "/dst/../dst", BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(sourcePath, "file1.txt"), 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(sourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(100);

        var result = await _executor.ExecuteAsync(job, strategy, _pauseGate);

        Assert.True(result.Success);
        Assert.Equal(1, result.FilesProcessed);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallEnumerateFilesWithNormalizedPath()
    {
        var normalizedSource = Path.GetFullPath("/src/");
        var job = new BackupJob(1, "TestJob", "/src/", "/dst/", BackupType.Full);
        var strategy = new FullBackupStrategy();

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(normalizedSource)).Returns(new List<FileDescriptor>());

        await _executor.ExecuteAsync(job, strategy, _pauseGate);

        _mockFileSystem.Verify(fs => fs.EnumerateFiles(normalizedSource), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallEnsureDirectoryWithNormalizedPath()
    {
        var normalizedTarget = Path.GetFullPath("/dst/");
        var job = new BackupJob(1, "TestJob", "/src/", "/dst/", BackupType.Full);
        var strategy = new FullBackupStrategy();

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(Path.GetFullPath("/src/"))).Returns(new List<FileDescriptor>());

        await _executor.ExecuteAsync(job, strategy, _pauseGate);

        _mockFileSystem.Verify(fs => fs.EnsureDirectory(normalizedTarget), Times.Once);
    }

    // --- Per-file encryption tests ---

    [Fact]
    public async Task ExecuteAsync_EncryptedExtension_ShouldCallEncryptFileAfterCopy()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "secret.docx"), 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(100);
        _mockEncryptionConfig.Setup(c => c.GetEncryptedExtensions())
            .Returns(new List<string> { ".docx" }.AsReadOnly());
        _mockEncryptionService.Setup(s => s.EncryptFile(It.IsAny<string>()))
            .Returns(new CryptoResult { Success = true, DurationMs = 75, ErrorCode = CryptoErrorCode.None });

        await _executor.ExecuteAsync(job, strategy, _pauseGate);

        _mockEncryptionService.Verify(
            s => s.EncryptFile(It.Is<string>(p => p.Contains("secret.docx"))),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_EncryptedExtension_ShouldSetEncryptionTimeMsFromCryptoResult()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "file.pdf"), 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(100);
        _mockEncryptionConfig.Setup(c => c.GetEncryptedExtensions())
            .Returns(new List<string> { ".pdf" }.AsReadOnly());
        _mockEncryptionService.Setup(s => s.EncryptFile(It.IsAny<string>()))
            .Returns(new CryptoResult { Success = true, DurationMs = 200, ErrorCode = CryptoErrorCode.None });

        TransferCompletedEvent? capturedEvent = null;
        _mockEventBus.Setup(bus => bus.Publish(It.IsAny<TransferCompletedEvent>()))
            .Callback<TransferCompletedEvent>(e => capturedEvent = e);

        await _executor.ExecuteAsync(job, strategy, _pauseGate);

        Assert.NotNull(capturedEvent);
        Assert.Equal(200, capturedEvent.Transfer.EncryptionTimeMs);
    }

    [Fact]
    public async Task ExecuteAsync_NonEncryptedExtension_ShouldNotCallEncryptFile()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "readme.txt"), 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(100);
        _mockEncryptionConfig.Setup(c => c.GetEncryptedExtensions())
            .Returns(new List<string> { ".pdf" }.AsReadOnly());

        await _executor.ExecuteAsync(job, strategy, _pauseGate);

        _mockEncryptionService.Verify(s => s.EncryptFile(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_NonEncryptedExtension_EncryptionTimeMsShouldBeZero()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "readme.txt"), 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(100);
        _mockEncryptionConfig.Setup(c => c.GetEncryptedExtensions())
            .Returns(new List<string> { ".pdf" }.AsReadOnly());

        TransferCompletedEvent? capturedEvent = null;
        _mockEventBus.Setup(bus => bus.Publish(It.IsAny<TransferCompletedEvent>()))
            .Callback<TransferCompletedEvent>(e => capturedEvent = e);

        await _executor.ExecuteAsync(job, strategy, _pauseGate);

        Assert.NotNull(capturedEvent);
        Assert.Equal(0, capturedEvent.Transfer.EncryptionTimeMs);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyEncryptedExtensions_ShouldNotCallEncrypt()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "secret.docx"), 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(100);
        _mockEncryptionConfig.Setup(c => c.GetEncryptedExtensions())
            .Returns(new List<string>().AsReadOnly());

        await _executor.ExecuteAsync(job, strategy, _pauseGate);

        _mockEncryptionService.Verify(s => s.EncryptFile(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_EncryptionFails_ShouldSetEncryptionTimeMsToNegativeErrorCode()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "file.pdf"), 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(100);
        _mockEncryptionConfig.Setup(c => c.GetEncryptedExtensions())
            .Returns(new List<string> { ".pdf" }.AsReadOnly());
        _mockEncryptionService.Setup(s => s.EncryptFile(It.IsAny<string>()))
            .Returns(new CryptoResult
            {
                Success = false,
                DurationMs = 0,
                ErrorCode = CryptoErrorCode.IoError,
                ErrorMessage = "CryptoSoft crashed"
            });

        TransferCompletedEvent? capturedEvent = null;
        _mockEventBus.Setup(bus => bus.Publish(It.IsAny<TransferCompletedEvent>()))
            .Callback<TransferCompletedEvent>(e => capturedEvent = e);

        var result = await _executor.ExecuteAsync(job, strategy, _pauseGate);

        Assert.NotNull(capturedEvent);
        // IoError = 3, so EncryptionTimeMs = -(3+1) = -4
        Assert.Equal(-((long)CryptoErrorCode.IoError + 1), capturedEvent.Transfer.EncryptionTimeMs);
        Assert.True(capturedEvent.Transfer.EncryptionTimeMs < 0);
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("CryptoSoft crashed"));
    }

    [Fact]
    public async Task ExecuteAsync_EncryptionExtensionComparison_ShouldBeCaseInsensitive()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "file.PDF"), 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(100);
        _mockEncryptionConfig.Setup(c => c.GetEncryptedExtensions())
            .Returns(new List<string> { ".pdf" }.AsReadOnly());
        _mockEncryptionService.Setup(s => s.EncryptFile(It.IsAny<string>()))
            .Returns(new CryptoResult { Success = true, DurationMs = 50, ErrorCode = CryptoErrorCode.None });

        await _executor.ExecuteAsync(job, strategy, _pauseGate);

        _mockEncryptionService.Verify(s => s.EncryptFile(It.IsAny<string>()), Times.Once);
    }

    // --- In-flight business software detection tests ---

    [Fact]
    public async Task ExecuteAsync_BusinessSoftwareRunning_ShouldStopAfterCurrentFile()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "file1.txt"), 100, DateTime.Now),
            new(Path.Combine(SourcePath, "file2.txt"), 200, DateTime.Now),
            new(Path.Combine(SourcePath, "file3.txt"), 300, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(100);
        _mockDetectorConfig.Setup(c => c.IsDetectionEnabled()).Returns(true);
        _mockDetector.Setup(d => d.GetStatus()).Returns(BusinessSoftwareStatus.Running);

        var result = await _executor.ExecuteAsync(job, strategy, _pauseGate);

        _mockFileSystem.Verify(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_BusinessSoftwareRunning_ShouldSetStateToBlocked()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "file1.txt"), 100, DateTime.Now),
            new(Path.Combine(SourcePath, "file2.txt"), 200, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(100);
        _mockDetectorConfig.Setup(c => c.IsDetectionEnabled()).Returns(true);
        _mockDetector.Setup(d => d.GetStatus()).Returns(BusinessSoftwareStatus.Running);

        var capturedEvents = new List<StateChangedEvent>();
        _mockEventBus.Setup(bus => bus.Publish(It.IsAny<StateChangedEvent>()))
            .Callback<StateChangedEvent>(e => capturedEvents.Add(e));

        await _executor.ExecuteAsync(job, strategy, _pauseGate);

        var lastSnapshot = capturedEvents.Last().Snapshot;
        Assert.Equal(JobState.Blocked, lastSnapshot.State);
    }

    [Fact]
    public async Task ExecuteAsync_BusinessSoftwareRunning_ShouldSetBlockReason()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "file1.txt"), 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(100);
        _mockDetectorConfig.Setup(c => c.IsDetectionEnabled()).Returns(true);
        _mockDetector.Setup(d => d.GetStatus()).Returns(BusinessSoftwareStatus.Running);

        var capturedEvents = new List<StateChangedEvent>();
        _mockEventBus.Setup(bus => bus.Publish(It.IsAny<StateChangedEvent>()))
            .Callback<StateChangedEvent>(e => capturedEvents.Add(e));

        await _executor.ExecuteAsync(job, strategy, _pauseGate);

        var lastSnapshot = capturedEvents.Last().Snapshot;
        Assert.NotNull(lastSnapshot.BlockReason);
        Assert.Contains("Business software", lastSnapshot.BlockReason);
    }

    [Fact]
    public async Task ExecuteAsync_BusinessSoftwareRunning_ShouldPublishBusinessSoftwareDetectedEvent()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "file1.txt"), 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(100);
        _mockDetectorConfig.Setup(c => c.IsDetectionEnabled()).Returns(true);
        _mockDetector.Setup(d => d.GetStatus()).Returns(BusinessSoftwareStatus.Running);

        await _executor.ExecuteAsync(job, strategy, _pauseGate);

        _mockEventBus.Verify(bus => bus.Publish(It.Is<BusinessSoftwareDetectedEvent>(
            e => e.JobName == "TestJob" && e.Status == BusinessSoftwareStatus.Running)), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_BusinessSoftwareUnknown_ShouldBlock_FailClosed()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "file1.txt"), 100, DateTime.Now),
            new(Path.Combine(SourcePath, "file2.txt"), 200, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(100);
        _mockDetectorConfig.Setup(c => c.IsDetectionEnabled()).Returns(true);
        _mockDetector.Setup(d => d.GetStatus()).Returns(BusinessSoftwareStatus.Unknown);

        var result = await _executor.ExecuteAsync(job, strategy, _pauseGate);

        _mockFileSystem.Verify(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_BusinessSoftwareError_ShouldBlock_FailClosed()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "file1.txt"), 100, DateTime.Now),
            new(Path.Combine(SourcePath, "file2.txt"), 200, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(100);
        _mockDetectorConfig.Setup(c => c.IsDetectionEnabled()).Returns(true);
        _mockDetector.Setup(d => d.GetStatus()).Returns(BusinessSoftwareStatus.Error);

        var result = await _executor.ExecuteAsync(job, strategy, _pauseGate);

        _mockFileSystem.Verify(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_BusinessSoftwareNotRunning_ShouldProcessAllFiles()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "file1.txt"), 100, DateTime.Now),
            new(Path.Combine(SourcePath, "file2.txt"), 200, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(100);
        _mockDetectorConfig.Setup(c => c.IsDetectionEnabled()).Returns(true);
        _mockDetector.Setup(d => d.GetStatus()).Returns(BusinessSoftwareStatus.NotRunning);

        var result = await _executor.ExecuteAsync(job, strategy, _pauseGate);

        Assert.True(result.Success);
        _mockFileSystem.Verify(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ExecuteAsync_BusinessSoftwareDisabled_ShouldProcessAllFiles()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "file1.txt"), 100, DateTime.Now),
            new(Path.Combine(SourcePath, "file2.txt"), 200, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(100);
        _mockDetectorConfig.Setup(c => c.IsDetectionEnabled()).Returns(true);
        _mockDetector.Setup(d => d.GetStatus()).Returns(BusinessSoftwareStatus.Disabled);

        var result = await _executor.ExecuteAsync(job, strategy, _pauseGate);

        Assert.True(result.Success);
        _mockFileSystem.Verify(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    // --- P2-B: Encryption exception classification tests ---

    [Fact]
    public async Task ExecuteAsync_EncryptionThrowsException_ShouldLogAsEncryptionFailure()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "file.pdf"), 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(100);
        _mockEncryptionConfig.Setup(c => c.GetEncryptedExtensions())
            .Returns(new List<string> { ".pdf" }.AsReadOnly());
        _mockEncryptionService.Setup(s => s.EncryptFile(It.IsAny<string>()))
            .Throws(new InvalidOperationException("CryptoSoft not found"));

        var result = await _executor.ExecuteAsync(job, strategy, _pauseGate);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("Encryption failed"));
        Assert.DoesNotContain(result.Errors, e => e.Contains("Failed to copy"));
    }

    [Fact]
    public async Task ExecuteAsync_EncryptionThrowsException_ShouldSetEncryptionTimeMsToNegativeUnknown()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "file.pdf"), 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(100);
        _mockEncryptionConfig.Setup(c => c.GetEncryptedExtensions())
            .Returns(new List<string> { ".pdf" }.AsReadOnly());
        _mockEncryptionService.Setup(s => s.EncryptFile(It.IsAny<string>()))
            .Throws(new InvalidOperationException("CryptoSoft not found"));

        TransferCompletedEvent? capturedEvent = null;
        _mockEventBus.Setup(bus => bus.Publish(It.IsAny<TransferCompletedEvent>()))
            .Callback<TransferCompletedEvent>(e => capturedEvent = e);

        await _executor.ExecuteAsync(job, strategy, _pauseGate);

        Assert.NotNull(capturedEvent);
        Assert.Equal(-((long)CryptoErrorCode.Unknown + 1), capturedEvent.Transfer.EncryptionTimeMs);
    }

    // --- P2-C: In-flight detection config guard tests ---

    [Fact]
    public async Task ExecuteAsync_DetectionDisabled_ShouldNotCheckDetectorInFlight()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "file1.txt"), 100, DateTime.Now),
            new(Path.Combine(SourcePath, "file2.txt"), 200, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(100);
        _mockDetectorConfig.Setup(c => c.IsDetectionEnabled()).Returns(false);
        _mockDetector.Setup(d => d.GetStatus()).Returns(BusinessSoftwareStatus.Running);

        var result = await _executor.ExecuteAsync(job, strategy, _pauseGate);

        Assert.True(result.Success);
        _mockFileSystem.Verify(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockDetector.Verify(d => d.GetStatus(), Times.Never);
    }

    // --- ExecuteAsync tests (00-09) ---

    private readonly AsyncManualResetEvent _pauseGate = new(initialState: true);

    [Fact]
    public async Task ExecuteAsync_CancelledToken_ShouldThrowAtFileBoundary()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "file1.txt"), 100, DateTime.Now),
            new(Path.Combine(SourcePath, "file2.txt"), 200, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(100);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _executor.ExecuteAsync(job, strategy, _pauseGate, cts.Token));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPassCancellationTokenToCopyFileAsync()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "file1.txt"), 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(100);

        using var cts = new CancellationTokenSource();
        await _executor.ExecuteAsync(job, strategy, _pauseGate, cts.Token);

        _mockFileSystem.Verify(fs => fs.CopyFileAsync(
            It.IsAny<string>(), It.IsAny<string>(), cts.Token), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_PauseGateBlocked_ShouldNotCopyUntilSet()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "file1.txt"), 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(100);

        var blockedGate = new AsyncManualResetEvent(initialState: false);
        var executeTask = _executor.ExecuteAsync(job, strategy, blockedGate);

        await Task.Delay(200);
        _mockFileSystem.Verify(
            fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        blockedGate.Set();
        var result = await executeTask;

        Assert.True(result.Success);
        _mockFileSystem.Verify(
            fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_CopyFileAsyncThrowsOCE_ShouldPropagate()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "file1.txt"), 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _executor.ExecuteAsync(job, strategy, _pauseGate));
    }

    [Fact]
    public async Task ExecuteAsync_WithDefaultGate_ShouldCopyAllFiles()
    {
        var job = new BackupJob(1, "TestJob", SourcePath, TargetPath, BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new(Path.Combine(SourcePath, "file1.txt"), 100, DateTime.Now),
            new(Path.Combine(SourcePath, "file2.txt"), 200, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(SourcePath)).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(100);

        var result = await _executor.ExecuteAsync(job, strategy, _pauseGate);

        Assert.True(result.Success);
        Assert.Equal(2, result.FilesProcessed);
    }
}
