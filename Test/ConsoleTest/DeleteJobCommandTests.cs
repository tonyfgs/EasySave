using Application.Ports;
using Application.Services;
using EasySave.Commands;
using Model;
using Moq;

namespace ConsoleTest;

public class DeleteJobCommandTests
{
    private readonly Mock<IJobRepository> _mockRepo;
    private readonly DeleteJobCommand _command;

    public DeleteJobCommandTests()
    {
        _mockRepo = new Mock<IJobRepository>();
        var domainService = new BackupDomainService();
        var jobService = new JobManagementService(_mockRepo.Object, domainService);
        _command = new DeleteJobCommand(jobService, TextWriter.Null);
    }

    [Fact]
    public void Execute_ValidId_ShouldReturnSuccess()
    {
        var result = _command.Execute(new List<string> { "1" });

        Assert.True(result.IsSuccess());
    }

    [Fact]
    public void Execute_NonNumericId_ShouldReturnFailure()
    {
        var result = _command.Execute(new List<string> { "abc" });

        Assert.False(result.IsSuccess());
    }

    [Fact]
    public void Execute_NoArgs_ShouldReturnFailure()
    {
        var result = _command.Execute(new List<string>());

        Assert.False(result.IsSuccess());
    }
}
