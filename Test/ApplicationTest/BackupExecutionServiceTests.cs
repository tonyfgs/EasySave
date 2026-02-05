using Application.Events;
using Application.Ports;
using Application.Services;
using Model;
using Moq;

namespace ApplicationTest;

public class BackupExecutionServiceTests
{
    private readonly Mock<IJobRepository> _mockRepo;
    private readonly BackupExecutionService _service;

    public BackupExecutionServiceTests()
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
            mockFileSystem.Object,
            mockPathAdapter.Object,
            mockEventBus.Object,
            domainService,
            tracker);

        _service = new BackupExecutionService(_mockRepo.Object, executor, strategyFactory);
    }

    [Fact]
    public void ExecuteJobs_ShouldExecuteSpecifiedJobs()
    {
        var job1 = new BackupJob(1, "Job1", "/src1", "/dst1", BackupType.Full);
        var job2 = new BackupJob(2, "Job2", "/src2", "/dst2", BackupType.Differential);

        _mockRepo.Setup(r => r.GetById(1)).Returns(job1);
        _mockRepo.Setup(r => r.GetById(2)).Returns(job2);

        var results = _service.ExecuteJobs(new List<int> { 1, 2 });

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.True(r.Result.Success));
    }

    [Fact]
    public void ExecuteJobs_JobNotFound_ShouldReturnFailureResult()
    {
        _mockRepo.Setup(r => r.GetById(99)).Returns((BackupJob?)null);

        var results = _service.ExecuteJobs(new List<int> { 99 });

        Assert.Single(results);
        Assert.False(results[0].Result.Success);
    }

    [Fact]
    public void ExecuteAllJobs_ShouldExecuteAllJobsFromRepository()
    {
        var jobs = new List<BackupJob>
        {
            new(1, "Job1", "/src1", "/dst1", BackupType.Full),
            new(2, "Job2", "/src2", "/dst2", BackupType.Full)
        };
        _mockRepo.Setup(r => r.GetAll()).Returns(jobs);
        _mockRepo.Setup(r => r.GetById(1)).Returns(jobs[0]);
        _mockRepo.Setup(r => r.GetById(2)).Returns(jobs[1]);

        var results = _service.ExecuteAllJobs();

        Assert.Equal(2, results.Count);
    }
}
