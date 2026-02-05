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
            e => e.Transfer.SourceFilePath.Contains("\\\\"))), Times.Once);
    }
}
