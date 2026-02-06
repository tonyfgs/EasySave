using Application.Ports;
using Application.Services;
using EasySave.Commands;
using Model;
using Moq;

namespace ConsoleTest;

public class CreateJobCommandTests
{
    private readonly Mock<IJobRepository> _mockRepo;
    private readonly CreateJobCommand _command;

    public CreateJobCommandTests()
    {
        _mockRepo = new Mock<IJobRepository>();
        _mockRepo.Setup(r => r.Count()).Returns(0);
        var domainService = new BackupDomainService();
        var jobService = new JobManagementService(_mockRepo.Object, domainService);
        _command = new CreateJobCommand(jobService, TextWriter.Null);
    }

    [Fact]
    public void Execute_ValidArgs_ShouldReturnSuccess()
    {
        var args = new List<string> { "MyBackup", "/src", "/dst", "Full" };

        var result = _command.Execute(args);

        Assert.True(result.IsSuccess());
    }

    [Fact]
    public void Execute_ValidArgs_ShouldCallSaveOnRepository()
    {
        var args = new List<string> { "MyBackup", "/src", "/dst", "Full" };

        _command.Execute(args);

        _mockRepo.Verify(r => r.Save(It.Is<BackupJob>(j => j.Name == "MyBackup")), Times.Once);
    }

    [Fact]
    public void Execute_AtJobLimit_ShouldReturnFailure()
    {
        _mockRepo.Setup(r => r.Count()).Returns(5);
        var args = new List<string> { "MyBackup", "/src", "/dst", "Full" };

        var result = _command.Execute(args);

        Assert.False(result.IsSuccess());
    }

    [Fact]
    public void Execute_WithEmptyName_ShouldReturnFailure()
    {
        var args = new List<string> { "", "/src", "/dst", "Full" };

        var result = _command.Execute(args);

        Assert.False(result.IsSuccess());
    }

    [Fact]
    public void Execute_InsufficientArgs_ShouldReturnFailure()
    {
        var args = new List<string> { "OnlyName" };

        var result = _command.Execute(args);

        Assert.False(result.IsSuccess());
    }
}
