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
        var jobService = new JobManagementService(_mockRepo.Object);
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

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    public void Execute_NumericBackupType_ShouldReturnFailure(string numericType)
    {
        var args = new List<string> { "MyBackup", "/src", "/dst", numericType };

        var result = _command.Execute(args);

        Assert.False(result.IsSuccess());
    }

    [Theory]
    [InlineData("full")]
    [InlineData("FULL")]
    [InlineData("Full")]
    public void Execute_CaseInsensitiveBackupType_ShouldReturnSuccess(string type)
    {
        var args = new List<string> { "MyBackup", "/src", "/dst", type };

        var result = _command.Execute(args);

        Assert.True(result.IsSuccess());
    }
}
