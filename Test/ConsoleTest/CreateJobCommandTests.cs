using Application.Ports;
using Application.Services;
using EasySave.Commands;
using EasySave.UI;
using Model;
using Moq;
using Shared;

namespace ConsoleTest;

public class CreateJobCommandTests
{
    private readonly Mock<IJobRepository> _mockRepo;
    private readonly Mock<ILanguageConfig> _mockConfig;
    private readonly LanguageManager _languageManager;
    private readonly CreateJobCommand _command;

    public CreateJobCommandTests()
    {
        _mockRepo = new Mock<IJobRepository>();
        _mockRepo.Setup(r => r.Count()).Returns(0);
        _mockConfig = new Mock<ILanguageConfig>();
        _mockConfig.Setup(c => c.GetLanguage()).Returns(Language.EN);
        var languageService = new LanguageApplicationService(_mockConfig.Object);
        _languageManager = new LanguageManager(languageService);
        var domainService = new BackupDomainService();
        var jobService = new JobManagementService(_mockRepo.Object, domainService);
        _command = new CreateJobCommand(jobService, _languageManager, TextWriter.Null);
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

    [Fact]
    public void Execute_ValidArgs_FR_ShouldOutputFrenchMessage()
    {
        _mockConfig.Setup(c => c.GetLanguage()).Returns(Language.FR);
        var output = new StringWriter();
        var domainService = new BackupDomainService();
        var jobService = new JobManagementService(_mockRepo.Object, domainService);
        var command = new CreateJobCommand(jobService, _languageManager, output);

        command.Execute(new List<string> { "MyBackup", "/src", "/dst", "Full" });

        Assert.Contains("Travail 'MyBackup' cree avec l'ID", output.ToString());
    }
}
