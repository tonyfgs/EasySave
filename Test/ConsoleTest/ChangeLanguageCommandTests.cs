using Application.Ports;
using Application.Services;
using EasySave.Commands;
using Moq;
using Shared;

namespace ConsoleTest;

public class ChangeLanguageCommandTests
{
    private readonly Mock<ILanguageConfig> _mockConfig;
    private readonly ChangeLanguageCommand _command;

    public ChangeLanguageCommandTests()
    {
        _mockConfig = new Mock<ILanguageConfig>();
        _mockConfig.Setup(c => c.GetLanguage()).Returns(Language.EN);
        var languageService = new LanguageApplicationService(_mockConfig.Object);
        _command = new ChangeLanguageCommand(languageService, TextWriter.Null);
    }

    [Fact]
    public void Execute_FR_ShouldReturnSuccess()
    {
        var result = _command.Execute(new List<string> { "FR" });

        Assert.True(result.IsSuccess());
        _mockConfig.Verify(c => c.SetLanguage(Language.FR), Times.Once);
    }

    [Fact]
    public void Execute_EN_ShouldReturnSuccess()
    {
        var result = _command.Execute(new List<string> { "EN" });

        Assert.True(result.IsSuccess());
        _mockConfig.Verify(c => c.SetLanguage(Language.EN), Times.Once);
    }

    [Fact]
    public void Execute_InvalidLanguage_ShouldReturnFailure()
    {
        var result = _command.Execute(new List<string> { "DE" });

        Assert.False(result.IsSuccess());
    }

    [Fact]
    public void Execute_NoArgs_ShouldReturnFailure()
    {
        var result = _command.Execute(new List<string>());

        Assert.False(result.IsSuccess());
    }
}
