using Application.Ports;
using Application.Services;
using EasySave.Commands;
using EasySave.UI;
using Model;
using Moq;
using Shared;

namespace ConsoleTest;

public class DeleteJobCommandTests
{
    private readonly Mock<IJobRepository> _mockRepo;
    private readonly Mock<ILanguageConfig> _mockConfig;
    private readonly LanguageManager _languageManager;
    private readonly DeleteJobCommand _command;

    public DeleteJobCommandTests()
    {
        _mockRepo = new Mock<IJobRepository>();
        _mockConfig = new Mock<ILanguageConfig>();
        _mockConfig.Setup(c => c.GetLanguage()).Returns(Language.EN);
        var languageService = new LanguageApplicationService(_mockConfig.Object);
        _languageManager = new LanguageManager(languageService);
        var jobService = new JobManagementService(_mockRepo.Object);
        _command = new DeleteJobCommand(jobService, _languageManager, TextWriter.Null);
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

    [Fact]
    public void Execute_ValidId_FR_ShouldOutputFrenchMessage()
    {
        _mockConfig.Setup(c => c.GetLanguage()).Returns(Language.FR);
        var output = new StringWriter();
        var jobService = new JobManagementService(_mockRepo.Object);
        var command = new DeleteJobCommand(jobService, _languageManager, output);

        command.Execute(new List<string> { "1" });

        Assert.Equal("Travail 1 supprime.", output.ToString().Trim());
    }
}
