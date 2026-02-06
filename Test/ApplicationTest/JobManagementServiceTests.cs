using Application.Ports;
using Application.Services;
using Model;
using Moq;

namespace ApplicationTest;

public class JobManagementServiceTests
{
    private readonly Mock<IJobRepository> _mockRepo;
    private readonly BackupDomainService _domainService;
    private readonly JobManagementService _service;

    public JobManagementServiceTests()
    {
        _mockRepo = new Mock<IJobRepository>();
        _domainService = new BackupDomainService();
        _service = new JobManagementService(_mockRepo.Object, _domainService);
    }

    [Fact]
    public void CreateJob_ShouldValidateAndSave()
    {
        _mockRepo.Setup(r => r.Count()).Returns(0);

        var job = _service.CreateJob("TestJob", "/src", "/dst", BackupType.Full);

        Assert.Equal("TestJob", job.Name);
        _mockRepo.Verify(r => r.Save(It.Is<BackupJob>(j => j.Name == "TestJob")), Times.Once);
    }

    [Fact]
    public void CreateJob_AtMaxJobs_ShouldThrow()
    {
        _mockRepo.Setup(r => r.Count()).Returns(5);

        Assert.Throws<JobLimitExceededException>(
            () => _service.CreateJob("TestJob", "/src", "/dst", BackupType.Full));
    }

    [Fact]
    public void CreateJob_InvalidName_ShouldThrow()
    {
        _mockRepo.Setup(r => r.Count()).Returns(0);

        Assert.Throws<InvalidBackupJobException>(
            () => _service.CreateJob("", "/src", "/dst", BackupType.Full));
    }

    [Fact]
    public void ListJobs_ShouldDelegateToRepository()
    {
        var jobs = new List<BackupJob>
        {
            new(1, "Job1", "/src1", "/dst1", BackupType.Full),
            new(2, "Job2", "/src2", "/dst2", BackupType.Differential)
        };
        _mockRepo.Setup(r => r.GetAll()).Returns(jobs);

        var result = _service.ListJobs();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void DeleteJob_ShouldDelegateToRepository()
    {
        _service.DeleteJob(1);
        _mockRepo.Verify(r => r.Delete(1), Times.Once);
    }

    [Fact]
    public void ModifyJob_ShouldFetchValidateAndUpdate()
    {
        var existingJob = new BackupJob(1, "OldName", "/old/src", "/old/dst", BackupType.Full);
        _mockRepo.Setup(r => r.GetById(1)).Returns(existingJob);

        _service.ModifyJob(1, "NewName", "/new/src", "/new/dst", BackupType.Differential);

        _mockRepo.Verify(r => r.Update(It.Is<BackupJob>(j =>
            j.Name == "NewName" &&
            j.SourcePath == "/new/src" &&
            j.TargetPath == "/new/dst" &&
            j.Type == BackupType.Differential
        )), Times.Once);
    }

    [Fact]
    public void ModifyJob_JobNotFound_ShouldThrow()
    {
        _mockRepo.Setup(r => r.GetById(99)).Returns((BackupJob?)null);

        Assert.Throws<DomainException>(() =>
            _service.ModifyJob(99, "Name", "/src", "/dst", BackupType.Full));
    }
}
