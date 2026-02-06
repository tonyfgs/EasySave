using Application.Ports;
using Application.Services;
using EasySave.Commands;
using Model;
using Moq;

namespace ConsoleTest;

public class ListJobsCommandTests
{
    private readonly Mock<IJobRepository> _mockRepo;
    private readonly JobManagementService _jobService;

    public ListJobsCommandTests()
    {
        _mockRepo = new Mock<IJobRepository>();
        var domainService = new BackupDomainService();
        _jobService = new JobManagementService(_mockRepo.Object, domainService);
    }

    [Fact]
    public void Execute_WithJobs_ShouldReturnSuccess()
    {
        _mockRepo.Setup(r => r.GetAll()).Returns(new List<BackupJob>
        {
            new(1, "Job1", "/src", "/dst", BackupType.Full)
        });
        var command = new ListJobsCommand(_jobService, TextWriter.Null);

        var result = command.Execute(new List<string>());

        Assert.True(result.IsSuccess());
    }

    [Fact]
    public void Execute_WithNoJobs_ShouldReturnSuccess()
    {
        _mockRepo.Setup(r => r.GetAll()).Returns(new List<BackupJob>());
        var command = new ListJobsCommand(_jobService, TextWriter.Null);

        var result = command.Execute(new List<string>());

        Assert.True(result.IsSuccess());
    }

    [Fact]
    public void Execute_WithJobs_ShouldWriteJobInfoToOutput()
    {
        _mockRepo.Setup(r => r.GetAll()).Returns(new List<BackupJob>
        {
            new(1, "Backup1", "/src", "/dst", BackupType.Full),
            new(2, "Backup2", "/src2", "/dst2", BackupType.Differential)
        });
        var writer = new StringWriter();
        var command = new ListJobsCommand(_jobService, writer);

        command.Execute(new List<string>());

        var output = writer.ToString();
        Assert.Contains("Backup1", output);
        Assert.Contains("Backup2", output);
        Assert.Contains("Full", output);
        Assert.Contains("Differential", output);
    }

    [Fact]
    public void Execute_WithNoJobs_ShouldOutputNoJobsMessage()
    {
        _mockRepo.Setup(r => r.GetAll()).Returns(new List<BackupJob>());
        var writer = new StringWriter();
        var command = new ListJobsCommand(_jobService, writer);

        command.Execute(new List<string>());

        var output = writer.ToString();
        Assert.False(string.IsNullOrWhiteSpace(output),
            "Expected a 'no jobs' message but output was empty");
    }
}
