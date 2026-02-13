using Application.Ports;
using Application.Services;
using EasySave.Commands;
using Model;
using Moq;

namespace ConsoleTest;

public class ModifyJobCommandTests
{
    private readonly Mock<IJobRepository> _mockRepo;
    private readonly ModifyJobCommand _command;

    public ModifyJobCommandTests()
    {
        _mockRepo = new Mock<IJobRepository>();
        var jobService = new JobManagementService(_mockRepo.Object);
        _command = new ModifyJobCommand(jobService, TextWriter.Null);
    }

    [Fact]
    public void Execute_ValidArgs_ShouldReturnSuccess()
    {
        var existingJob = new BackupJob(1, "OldJob", "/old/src", "/old/dst", BackupType.Full);
        _mockRepo.Setup(r => r.GetById(1)).Returns(existingJob);

        var args = new List<string> { "1", "NewJob", "/new/src", "/new/dst", "Differential" };

        var result = _command.Execute(args);

        Assert.True(result.IsSuccess());
    }

    [Fact]
    public void Execute_ValidArgs_ShouldCallUpdateOnRepository()
    {
        var existingJob = new BackupJob(1, "OldJob", "/old/src", "/old/dst", BackupType.Full);
        _mockRepo.Setup(r => r.GetById(1)).Returns(existingJob);

        _command.Execute(new List<string> { "1", "NewJob", "/new/src", "/new/dst", "Differential" });

        _mockRepo.Verify(r => r.Update(It.Is<BackupJob>(j =>
            j.Name == "NewJob" && j.Type == BackupType.Differential)), Times.Once);
    }

    [Fact]
    public void Execute_NonexistentJob_ShouldReturnFailure()
    {
        _mockRepo.Setup(r => r.GetById(99)).Returns((BackupJob?)null);

        var args = new List<string> { "99", "Name", "/src", "/dst", "Full" };
        var result = _command.Execute(args);

        Assert.False(result.IsSuccess());
    }

    [Fact]
    public void Execute_InsufficientArgs_ShouldReturnFailure()
    {
        var result = _command.Execute(new List<string> { "1", "Name" });

        Assert.False(result.IsSuccess());
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    public void Execute_NumericBackupType_ShouldReturnFailure(string numericType)
    {
        var args = new List<string> { "1", "NewJob", "/src", "/dst", numericType };

        var result = _command.Execute(args);

        Assert.False(result.IsSuccess());
    }

    [Theory]
    [InlineData("differential")]
    [InlineData("DIFFERENTIAL")]
    [InlineData("Differential")]
    public void Execute_CaseInsensitiveBackupType_ShouldReturnSuccess(string type)
    {
        var existingJob = new BackupJob(1, "OldJob", "/old/src", "/old/dst", BackupType.Full);
        _mockRepo.Setup(r => r.GetById(1)).Returns(existingJob);

        var args = new List<string> { "1", "NewJob", "/new/src", "/new/dst", type };

        var result = _command.Execute(args);

        Assert.True(result.IsSuccess());
    }
}
