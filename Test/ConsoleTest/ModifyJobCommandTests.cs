using Application.Ports;
using Application.Services;
using EasySave.Commands;
using EasySave.UI;
using Model;
using Moq;
using Shared;

namespace ConsoleTest;

public class ModifyJobCommandTests
{
    private readonly Mock<IJobRepository> _mockRepo;
    private readonly Mock<ILanguageConfig> _mockConfig;
    private readonly LanguageManager _languageManager;
    private readonly ModifyJobCommand _command;

    public ModifyJobCommandTests()
    {
        _mockRepo = new Mock<IJobRepository>();
        _mockConfig = new Mock<ILanguageConfig>();
        _mockConfig.Setup(c => c.GetLanguage()).Returns(Language.EN);
        var languageService = new LanguageApplicationService(_mockConfig.Object);
        _languageManager = new LanguageManager(languageService);
        var domainService = new BackupDomainService();
        var jobService = new JobManagementService(_mockRepo.Object, domainService);
        _command = new ModifyJobCommand(jobService, _languageManager, TextWriter.Null);
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

    [Fact]
    public void Execute_ValidArgs_FR_ShouldOutputFrenchMessage()
    {
        _mockConfig.Setup(c => c.GetLanguage()).Returns(Language.FR);
        var existingJob = new BackupJob(1, "OldJob", "/old/src", "/old/dst", BackupType.Full);
        _mockRepo.Setup(r => r.GetById(1)).Returns(existingJob);
        var output = new StringWriter();
        var domainService = new BackupDomainService();
        var jobService = new JobManagementService(_mockRepo.Object, domainService);
        var command = new ModifyJobCommand(jobService, _languageManager, output);

        command.Execute(new List<string> { "1", "NewJob", "/new/src", "/new/dst", "Differential" });

        Assert.Equal("Travail 1 modifie.", output.ToString().Trim());
    }
}
