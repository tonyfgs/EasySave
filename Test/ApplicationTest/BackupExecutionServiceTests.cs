using Application.Concurrency;
using Application.Events;
using Application.Ports;
using Application.Services;
using Model;
using Moq;

namespace ApplicationTest;

public class BackupExecutionServiceTests
{
    private readonly Mock<IJobRepository> _mockRepo;
    private readonly Mock<IBusinessSoftwareDetector> _mockDetector;
    private readonly Mock<IBusinessSoftwareConfig> _mockDetectorConfig;
    private readonly Mock<IEventBus> _mockEventBus;
    private readonly BackupExecutionService _service;

    public BackupExecutionServiceTests()
    {
        _mockRepo = new Mock<IJobRepository>();
        _mockDetector = new Mock<IBusinessSoftwareDetector>();
        _mockDetectorConfig = new Mock<IBusinessSoftwareConfig>();
        _mockEventBus = new Mock<IEventBus>();
        var strategyFactory = new BackupStrategyFactory();

        var mockFileSystem = new Mock<IFileSystemGateway>();
        var mockPathAdapter = new Mock<IPathAdapter>();
        var mockEncryptionService = new Mock<IEncryptionService>();
        var mockEncryptionConfig = new Mock<IEncryptionConfig>();
        var mockLargeFileLock = new Mock<ILargeFileTransferLock>();
        var mockLargeFileConfig = new Mock<ILargeFileConfig>();
        var mockPriorityFileConfig = new Mock<IPriorityFileConfig>();
        var priorityFileGate = new PriorityFileGate();
        var domainService = new BackupDomainService();

        mockPathAdapter.Setup(p => p.ToUNC(It.IsAny<string>())).Returns<string>(s => s);
        mockFileSystem.Setup(fs => fs.EnumerateFiles(It.IsAny<string>()))
            .Returns(new List<FileDescriptor>());
        mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);
        mockEncryptionConfig.Setup(c => c.GetEncryptedExtensions())
            .Returns(new List<string>().AsReadOnly());
        _mockDetector.Setup(d => d.GetStatus()).Returns(BusinessSoftwareStatus.NotRunning);
        _mockDetectorConfig.Setup(c => c.IsDetectionEnabled()).Returns(false);
        mockLargeFileConfig.Setup(c => c.GetLargeFileSizeThresholdKb()).Returns(0);
        mockPriorityFileConfig.Setup(c => c.GetPriorityExtensions())
            .Returns(new List<string>().AsReadOnly());

        var executor = new BackupExecutor(
            mockFileSystem.Object,
            mockPathAdapter.Object,
            _mockEventBus.Object,
            domainService,
            mockEncryptionService.Object,
            mockEncryptionConfig.Object,
            _mockDetector.Object,
            _mockDetectorConfig.Object,
            mockLargeFileLock.Object,
            mockLargeFileConfig.Object,
            mockPriorityFileConfig.Object,
            priorityFileGate);

        var watcher = new BusinessSoftwareWatcher(
            _mockDetector.Object, _mockDetectorConfig.Object, executor);
        _service = new BackupExecutionService(
            _mockRepo.Object, executor, strategyFactory, watcher,
            _mockDetector.Object, _mockDetectorConfig.Object);
    }

    [Fact]
    public async Task ExecuteJobsAsync_ShouldExecuteSpecifiedJobs()
    {
        var job1 = new BackupJob(1, "Job1", "/src1", "/dst1", BackupType.Full);
        var job2 = new BackupJob(2, "Job2", "/src2", "/dst2", BackupType.Differential);

        _mockRepo.Setup(r => r.GetById(1)).Returns(job1);
        _mockRepo.Setup(r => r.GetById(2)).Returns(job2);

        var results = await _service.ExecuteJobsAsync(new List<int> { 1, 2 });

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.True(r.Result.Success));
    }

    [Fact]
    public async Task ExecuteJobsAsync_JobNotFound_ShouldReturnFailureResult()
    {
        _mockRepo.Setup(r => r.GetById(99)).Returns((BackupJob?)null);

        var results = await _service.ExecuteJobsAsync(new List<int> { 99 });

        Assert.Single(results);
        Assert.False(results[0].Result.Success);
    }

    [Fact]
    public async Task ExecuteJobsAsync_ShouldCallRepositoryUpdateAfterExecution()
    {
        var job = new BackupJob(1, "Job1", "/src1", "/dst1", BackupType.Full);
        _mockRepo.Setup(r => r.GetById(1)).Returns(job);

        await _service.ExecuteJobsAsync(new List<int> { 1 });

        _mockRepo.Verify(r => r.Update(job), Times.Once);
    }

    [Fact]
    public async Task ExecuteAllJobsAsync_ShouldExecuteAllJobsFromRepository()
    {
        var jobs = new List<BackupJob>
        {
            new(1, "Job1", "/src1", "/dst1", BackupType.Full),
            new(2, "Job2", "/src2", "/dst2", BackupType.Full)
        };
        _mockRepo.Setup(r => r.GetAll()).Returns(jobs);
        _mockRepo.Setup(r => r.GetById(1)).Returns(jobs[0]);
        _mockRepo.Setup(r => r.GetById(2)).Returns(jobs[1]);

        var results = await _service.ExecuteAllJobsAsync();

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task ExecuteJobsAsync_EmptyList_ShouldReturnEmptyResults()
    {
        var results = await _service.ExecuteJobsAsync(new List<int>());

        Assert.Empty(results);
    }
}
