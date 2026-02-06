using Application.Events;
using Application.Ports;
using Application.Services;
using Model;
using Moq;

namespace ApplicationTest;

public class BackupExecutorTests
{
    private readonly Mock<IFileSystemGateway> _mockFileSystem;
    private readonly Mock<IPathAdapter> _mockPathAdapter;
    private readonly Mock<IEventBus> _mockEventBus;
    private readonly BackupDomainService _domainService;
    private readonly ProgressTracker _tracker;
    private readonly BackupExecutor _executor;

    public BackupExecutorTests()
    {
        _mockFileSystem = new Mock<IFileSystemGateway>();
        _mockPathAdapter = new Mock<IPathAdapter>();
        _mockEventBus = new Mock<IEventBus>();
        _domainService = new BackupDomainService();
        _tracker = new ProgressTracker();

        _mockPathAdapter.Setup(p => p.ToUNC(It.IsAny<string>())).Returns<string>(s => s);

        _executor = new BackupExecutor(
            _mockFileSystem.Object,
            _mockPathAdapter.Object,
            _mockEventBus.Object,
            _domainService,
            _tracker);
    }

    [Fact]
    public void Execute_FullBackup_ShouldCopyAllFiles()
    {
        var job = new BackupJob(1, "TestJob", "/src", "/dst", BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new("/src/file1.txt", 100, DateTime.Now),
            new("/src/file2.txt", 200, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles("/src")).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFile(It.IsAny<string>(), It.IsAny<string>())).Returns(100);

        var result = _executor.Execute(job, strategy);

        Assert.True(result.Success);
        Assert.Equal(2, result.FilesProcessed);
        _mockFileSystem.Verify(fs => fs.CopyFile(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
    }

    [Fact]
    public void Execute_ShouldPublishTransferCompletedEventPerFile()
    {
        var job = new BackupJob(1, "TestJob", "/src", "/dst", BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new("/src/file1.txt", 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles("/src")).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFile(It.IsAny<string>(), It.IsAny<string>())).Returns(100);

        _executor.Execute(job, strategy);

        _mockEventBus.Verify(bus => bus.Publish(It.IsAny<TransferCompletedEvent>()), Times.Once);
    }

    [Fact]
    public void Execute_ShouldPublishStateChangedEvents()
    {
        var job = new BackupJob(1, "TestJob", "/src", "/dst", BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new("/src/file1.txt", 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles("/src")).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFile(It.IsAny<string>(), It.IsAny<string>())).Returns(100);

        _executor.Execute(job, strategy);

        _mockEventBus.Verify(bus => bus.Publish(It.IsAny<StateChangedEvent>()), Times.AtLeastOnce);
    }

    [Fact]
    public void Execute_ShouldEnsureTargetDirectoryExists()
    {
        var job = new BackupJob(1, "TestJob", "/src", "/dst", BackupType.Full);
        var strategy = new FullBackupStrategy();

        _mockFileSystem.Setup(fs => fs.EnumerateFiles("/src")).Returns(new List<FileDescriptor>());

        _executor.Execute(job, strategy);

        _mockFileSystem.Verify(fs => fs.EnsureDirectory("/dst"), Times.Once);
    }

    [Fact]
    public void Execute_NoFiles_ShouldReturnSuccessWithZeroFiles()
    {
        var job = new BackupJob(1, "TestJob", "/src", "/dst", BackupType.Full);
        var strategy = new FullBackupStrategy();

        _mockFileSystem.Setup(fs => fs.EnumerateFiles("/src")).Returns(new List<FileDescriptor>());

        var result = _executor.Execute(job, strategy);

        Assert.True(result.Success);
        Assert.Equal(0, result.FilesProcessed);
    }

    [Fact]
    public void Execute_CopyFails_ShouldReturnFailureResult()
    {
        var job = new BackupJob(1, "TestJob", "/src", "/dst", BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new("/src/file1.txt", 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles("/src")).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFile(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new IOException("Copy failed"));

        var result = _executor.Execute(job, strategy);

        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Execute_ShouldConvertPathsToUNC()
    {
        var job = new BackupJob(1, "TestJob", "/src", "/dst", BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new("/src/file1.txt", 100, DateTime.Now)
        };

        _mockPathAdapter.Setup(p => p.ToUNC("/src/file1.txt")).Returns("\\\\server\\src\\file1.txt");
        _mockPathAdapter.Setup(p => p.ToUNC("/dst/file1.txt")).Returns("\\\\server\\dst\\file1.txt");
        _mockFileSystem.Setup(fs => fs.EnumerateFiles("/src")).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFile(It.IsAny<string>(), It.IsAny<string>())).Returns(100);

        _executor.Execute(job, strategy);

        _mockEventBus.Verify(bus => bus.Publish(It.Is<TransferCompletedEvent>(
            e => e.Transfer.SourcePath.Contains("\\\\"))), Times.Once);
    }

    [Fact]
    public void Execute_ShouldIncludeCurrentFileInStateSnapshot()
    {
        var job = new BackupJob(1, "TestJob", "/src", "/dst", BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new("/src/file1.txt", 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles("/src")).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFile(It.IsAny<string>(), It.IsAny<string>())).Returns(100);

        StateChangedEvent? capturedEvent = null;
        _mockEventBus.Setup(bus => bus.Publish(It.IsAny<StateChangedEvent>()))
            .Callback<StateChangedEvent>(e =>
            {
                if (e.Snapshot.State == JobState.Active)
                    capturedEvent = e;
            });

        _executor.Execute(job, strategy);

        Assert.NotNull(capturedEvent);
        Assert.NotEqual(string.Empty, capturedEvent.Snapshot.CurrentSourceFile);
        Assert.NotEqual(string.Empty, capturedEvent.Snapshot.CurrentDestFile);
    }

    [Fact]
    public void Execute_ShouldPublishEndStateAfterCompletion()
    {
        var job = new BackupJob(1, "TestJob", "/src", "/dst", BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new("/src/file1.txt", 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles("/src")).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFile(It.IsAny<string>(), It.IsAny<string>())).Returns(100);

        var capturedEvents = new List<StateChangedEvent>();
        _mockEventBus.Setup(bus => bus.Publish(It.IsAny<StateChangedEvent>()))
            .Callback<StateChangedEvent>(e => capturedEvents.Add(e));

        _executor.Execute(job, strategy);

        var lastSnapshot = capturedEvents.Last().Snapshot;
        Assert.Equal(JobState.End, lastSnapshot.State);
        Assert.Equal(string.Empty, lastSnapshot.CurrentSourceFile);
    }

    [Fact]
    public void Execute_CopyFails_ShouldPublishTransferEventWithNegativeTime()
    {
        var job = new BackupJob(1, "TestJob", "/src", "/dst", BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new("/src/file1.txt", 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles("/src")).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFile(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new IOException("Disk full"));

        TransferCompletedEvent? capturedEvent = null;
        _mockEventBus.Setup(bus => bus.Publish(It.IsAny<TransferCompletedEvent>()))
            .Callback<TransferCompletedEvent>(e => capturedEvent = e);

        _executor.Execute(job, strategy);

        Assert.NotNull(capturedEvent);
        Assert.True(capturedEvent.Transfer.TransferTimeMs < 0);
    }

    [Fact]
    public void Execute_SourcePathWithTrailingSlash_ShouldConstructCorrectTargetPath()
    {
        var job = new BackupJob(1, "TestJob", "/src/", "/dst/", BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new("/src/subdir/file1.txt", 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles("/src/")).Returns(files);

        string? capturedTarget = null;
        _mockFileSystem.Setup(fs => fs.CopyFile(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((src, tgt) => capturedTarget = tgt)
            .Returns(100);

        _executor.Execute(job, strategy);

        Assert.NotNull(capturedTarget);
        Assert.Contains("subdir", capturedTarget);
        Assert.Contains("file1.txt", capturedTarget);
    }

    [Fact]
    public void Execute_SourcePathWithoutTrailingSlash_ShouldConstructCorrectTargetPath()
    {
        var job = new BackupJob(1, "TestJob", "/src", "/dst", BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new("/src/file1.txt", 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles("/src")).Returns(files);

        string? capturedTarget = null;
        _mockFileSystem.Setup(fs => fs.CopyFile(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((src, tgt) => capturedTarget = tgt)
            .Returns(100);

        _executor.Execute(job, strategy);

        Assert.NotNull(capturedTarget);
        Assert.EndsWith("file1.txt", capturedTarget);
    }

    [Fact]
    public void Execute_EmptySourceDir_ShouldPublishEndStateEvent()
    {
        var job = new BackupJob(1, "TestJob", "/src", "/dst", BackupType.Full);
        var strategy = new FullBackupStrategy();

        _mockFileSystem.Setup(fs => fs.EnumerateFiles("/src")).Returns(new List<FileDescriptor>());

        var capturedEvents = new List<StateChangedEvent>();
        _mockEventBus.Setup(bus => bus.Publish(It.IsAny<StateChangedEvent>()))
            .Callback<StateChangedEvent>(e => capturedEvents.Add(e));

        var result = _executor.Execute(job, strategy);

        Assert.True(result.Success);
        Assert.NotEmpty(capturedEvents);
        Assert.Equal(JobState.End, capturedEvents.Last().Snapshot.State);
    }

    [Fact]
    public void Execute_CopyFails_TransferTimeMs_ShouldBeExactlyMinusOne()
    {
        var job = new BackupJob(1, "TestJob", "/src", "/dst", BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new("/src/file1.txt", 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles("/src")).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFile(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new IOException("Disk full"));

        TransferCompletedEvent? capturedEvent = null;
        _mockEventBus.Setup(bus => bus.Publish(It.IsAny<TransferCompletedEvent>()))
            .Callback<TransferCompletedEvent>(e => capturedEvent = e);

        _executor.Execute(job, strategy);

        Assert.NotNull(capturedEvent);
        Assert.Equal(-1, capturedEvent.Transfer.TransferTimeMs);
    }

    [Fact]
    public void Execute_FirstFileFails_ShouldContinueProcessingSecondFile()
    {
        var job = new BackupJob(1, "TestJob", "/src", "/dst", BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new("/src/file1.txt", 100, DateTime.Now),
            new("/src/file2.txt", 200, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles("/src")).Returns(files);
        _mockFileSystem.SetupSequence(fs => fs.CopyFile(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new IOException("File locked"))
            .Returns(200);

        var transferEvents = new List<TransferCompletedEvent>();
        _mockEventBus.Setup(bus => bus.Publish(It.IsAny<TransferCompletedEvent>()))
            .Callback<TransferCompletedEvent>(e => transferEvents.Add(e));

        var result = _executor.Execute(job, strategy);

        _mockFileSystem.Verify(fs => fs.CopyFile(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
        Assert.Equal(2, transferEvents.Count);
        Assert.Equal(-1, transferEvents[0].Transfer.TransferTimeMs);
        Assert.True(transferEvents[1].Transfer.TransferTimeMs >= 0);
    }

    [Fact]
    public void Execute_CopyFails_ErrorLog_ShouldContainCorrectPathsAndSize()
    {
        var job = new BackupJob(1, "TestJob", "/src", "/dst", BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new("/src/data.bin", 4096, DateTime.Now)
        };

        _mockPathAdapter.Setup(p => p.ToUNC("/src/data.bin")).Returns("\\\\server\\src\\data.bin");
        _mockPathAdapter.Setup(p => p.ToUNC(It.Is<string>(s => s.Contains("/dst")))).Returns("\\\\server\\dst\\data.bin");
        _mockFileSystem.Setup(fs => fs.EnumerateFiles("/src")).Returns(files);
        _mockFileSystem.Setup(fs => fs.CopyFile(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new IOException("Access denied"));

        TransferCompletedEvent? capturedEvent = null;
        _mockEventBus.Setup(bus => bus.Publish(It.IsAny<TransferCompletedEvent>()))
            .Callback<TransferCompletedEvent>(e => capturedEvent = e);

        _executor.Execute(job, strategy);

        Assert.NotNull(capturedEvent);
        Assert.Equal("\\\\server\\src\\data.bin", capturedEvent.Transfer.SourcePath);
        Assert.Equal("\\\\server\\dst\\data.bin", capturedEvent.Transfer.DestPath);
        Assert.Equal(4096, capturedEvent.Transfer.FileSize);
        Assert.Equal(-1, capturedEvent.Transfer.TransferTimeMs);
    }

    [Fact]
    public void Execute_NestedSubdir_ShouldPreserveExactRelativePath()
    {
        var job = new BackupJob(1, "TestJob", "/src", "/dst", BackupType.Full);
        var strategy = new FullBackupStrategy();
        var files = new List<FileDescriptor>
        {
            new("/src/a/b/c.txt", 100, DateTime.Now)
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles("/src")).Returns(files);

        string? capturedTarget = null;
        _mockFileSystem.Setup(fs => fs.CopyFile(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((src, tgt) => capturedTarget = tgt)
            .Returns(100);

        _executor.Execute(job, strategy);

        Assert.NotNull(capturedTarget);
        var expectedTarget = Path.Combine("/dst", "a", "b", "c.txt");
        Assert.Equal(expectedTarget, capturedTarget);
    }
}
