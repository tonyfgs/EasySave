using Application.Events;
using Application.Ports;
using Application.Services;
using EasySave.Commands;
using Model;
using Moq;

namespace ConsoleTest;

public class ExecuteJobCommandTests
{
    private readonly Mock<IJobRepository> _mockRepo;
    private readonly ExecuteJobCommand _command;

    public ExecuteJobCommandTests()
    {
        _mockRepo = new Mock<IJobRepository>();
        var strategyFactory = new BackupStrategyFactory();
        var mockFileSystem = new Mock<IFileSystemGateway>();
        var mockPathAdapter = new Mock<IPathAdapter>();
        var mockEventBus = new Mock<IEventBus>();
        var domainService = new BackupDomainService();
        var tracker = new ProgressTracker();

        mockPathAdapter.Setup(p => p.ToUNC(It.IsAny<string>())).Returns<string>(s => s);
        mockFileSystem.Setup(fs => fs.EnumerateFiles(It.IsAny<string>()))
            .Returns(new List<FileDescriptor>());

        var executor = new BackupExecutor(
            mockFileSystem.Object, mockPathAdapter.Object,
            mockEventBus.Object, domainService, tracker);

        var executionService = new BackupExecutionService(_mockRepo.Object, executor, strategyFactory);
        _command = new ExecuteJobCommand(executionService, TextWriter.Null);
    }

    [Fact]
    public void Execute_ValidJobIds_ShouldReturnSuccess()
    {
        var job = new BackupJob(1, "Job1", "/src", "/dst", BackupType.Full);
        _mockRepo.Setup(r => r.GetById(1)).Returns(job);

        var result = _command.Execute(new List<string> { "1" });

        Assert.True(result.IsSuccess());
    }

    [Fact]
    public void Execute_NonexistentJob_ShouldReturnFailure()
    {
        _mockRepo.Setup(r => r.GetById(99)).Returns((BackupJob?)null);

        var result = _command.Execute(new List<string> { "99" });

        Assert.False(result.IsSuccess());
    }

    [Fact]
    public void Execute_MultipleJobs_AllSucceed_ShouldReturnSuccess()
    {
        _mockRepo.Setup(r => r.GetById(1)).Returns(new BackupJob(1, "J1", "/s1", "/d1", BackupType.Full));
        _mockRepo.Setup(r => r.GetById(2)).Returns(new BackupJob(2, "J2", "/s2", "/d2", BackupType.Full));

        var result = _command.Execute(new List<string> { "1", "2" });

        Assert.True(result.IsSuccess());
    }

    [Fact]
    public void Execute_NonNumericArgs_ShouldReturnFailure()
    {
        var result = _command.Execute(new List<string> { "abc" });

        Assert.False(result.IsSuccess());
    }

    [Fact]
    public void Execute_StarArg_ShouldExecuteAllJobs()
    {
        var job1 = new BackupJob(1, "J1", "/s1", "/d1", BackupType.Full);
        var job2 = new BackupJob(2, "J2", "/s2", "/d2", BackupType.Full);
        _mockRepo.Setup(r => r.GetAll()).Returns(new List<BackupJob> { job1, job2 });
        _mockRepo.Setup(r => r.GetById(1)).Returns(job1);
        _mockRepo.Setup(r => r.GetById(2)).Returns(job2);

        var result = _command.Execute(new List<string> { "*" });

        Assert.True(result.IsSuccess());
    }
}
