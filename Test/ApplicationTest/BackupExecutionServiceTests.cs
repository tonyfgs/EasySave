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
        var domainService = new BackupDomainService();
        var tracker = new ProgressTracker();

        mockPathAdapter.Setup(p => p.ToUNC(It.IsAny<string>())).Returns<string>(s => s);
        mockFileSystem.Setup(fs => fs.EnumerateFiles(It.IsAny<string>()))
            .Returns(new List<FileDescriptor>());
        mockEncryptionConfig.Setup(c => c.GetEncryptedExtensions())
            .Returns(new List<string>().AsReadOnly());
        _mockDetector.Setup(d => d.GetStatus()).Returns(BusinessSoftwareStatus.NotRunning);
        _mockDetectorConfig.Setup(c => c.IsDetectionEnabled()).Returns(false);

        var executor = new BackupExecutor(
            mockFileSystem.Object,
            mockPathAdapter.Object,
            _mockEventBus.Object,
            domainService,
            tracker,
            mockEncryptionService.Object,
            mockEncryptionConfig.Object,
            _mockDetector.Object);

        _service = new BackupExecutionService(
            _mockRepo.Object, executor, strategyFactory,
            _mockDetector.Object, _mockDetectorConfig.Object, _mockEventBus.Object);
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
    public void ExecuteJobs_ShouldCallRepositoryUpdateAfterExecution()
    {
        var job = new BackupJob(1, "Job1", "/src1", "/dst1", BackupType.Full);
        _mockRepo.Setup(r => r.GetById(1)).Returns(job);

        _service.ExecuteJobs(new List<int> { 1 });

        _mockRepo.Verify(r => r.Update(job), Times.Once);
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

    // --- Pre-flight business software detection tests ---

    [Fact]
    public void ExecuteJobs_DetectionEnabled_Running_ShouldSkipJobWithFailResult()
    {
        var job = new BackupJob(1, "Job1", "/src1", "/dst1", BackupType.Full);
        _mockRepo.Setup(r => r.GetById(1)).Returns(job);
        _mockDetectorConfig.Setup(c => c.IsDetectionEnabled()).Returns(true);
        _mockDetector.Setup(d => d.GetStatus()).Returns(BusinessSoftwareStatus.Running);

        var results = _service.ExecuteJobs(new List<int> { 1 });

        Assert.Single(results);
        Assert.False(results[0].Result.Success);
        Assert.Contains(results[0].Result.Errors, e => e.Contains("Business software"));
    }

    [Fact]
    public void ExecuteJobs_DetectionEnabled_Running_ShouldPublishBusinessSoftwareDetectedEvent()
    {
        var job = new BackupJob(1, "Job1", "/src1", "/dst1", BackupType.Full);
        _mockRepo.Setup(r => r.GetById(1)).Returns(job);
        _mockDetectorConfig.Setup(c => c.IsDetectionEnabled()).Returns(true);
        _mockDetector.Setup(d => d.GetStatus()).Returns(BusinessSoftwareStatus.Running);

        _service.ExecuteJobs(new List<int> { 1 });

        _mockEventBus.Verify(bus => bus.Publish(It.Is<BusinessSoftwareDetectedEvent>(
            e => e.JobName == "Job1" && e.Status == BusinessSoftwareStatus.Running)), Times.Once);
    }

    [Fact]
    public void ExecuteJobs_DetectionEnabled_NotRunning_ShouldExecuteNormally()
    {
        var job = new BackupJob(1, "Job1", "/src1", "/dst1", BackupType.Full);
        _mockRepo.Setup(r => r.GetById(1)).Returns(job);
        _mockDetectorConfig.Setup(c => c.IsDetectionEnabled()).Returns(true);
        _mockDetector.Setup(d => d.GetStatus()).Returns(BusinessSoftwareStatus.NotRunning);

        var results = _service.ExecuteJobs(new List<int> { 1 });

        Assert.Single(results);
        Assert.True(results[0].Result.Success);
    }

    [Fact]
    public void ExecuteJobs_DetectionDisabled_ShouldNotCheckDetector()
    {
        var job = new BackupJob(1, "Job1", "/src1", "/dst1", BackupType.Full);
        _mockRepo.Setup(r => r.GetById(1)).Returns(job);
        _mockDetectorConfig.Setup(c => c.IsDetectionEnabled()).Returns(false);

        _service.ExecuteJobs(new List<int> { 1 });

        _mockDetector.Verify(d => d.GetStatus(), Times.Never);
    }

    [Fact]
    public void ExecuteJobs_DetectionEnabled_Unknown_ShouldBlock_FailClosed()
    {
        var job = new BackupJob(1, "Job1", "/src1", "/dst1", BackupType.Full);
        _mockRepo.Setup(r => r.GetById(1)).Returns(job);
        _mockDetectorConfig.Setup(c => c.IsDetectionEnabled()).Returns(true);
        _mockDetector.Setup(d => d.GetStatus()).Returns(BusinessSoftwareStatus.Unknown);

        var results = _service.ExecuteJobs(new List<int> { 1 });

        Assert.Single(results);
        Assert.False(results[0].Result.Success);
    }

    [Fact]
    public void ExecuteJobs_DetectionEnabled_Error_ShouldBlock_FailClosed()
    {
        var job = new BackupJob(1, "Job1", "/src1", "/dst1", BackupType.Full);
        _mockRepo.Setup(r => r.GetById(1)).Returns(job);
        _mockDetectorConfig.Setup(c => c.IsDetectionEnabled()).Returns(true);
        _mockDetector.Setup(d => d.GetStatus()).Returns(BusinessSoftwareStatus.Error);

        var results = _service.ExecuteJobs(new List<int> { 1 });

        Assert.Single(results);
        Assert.False(results[0].Result.Success);
    }
}
