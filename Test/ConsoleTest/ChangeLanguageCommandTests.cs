using Application.Ports;
using Application.Services;
using EasySave.Commands;
using EasySave.UI;
using Moq;
using Shared;

namespace ConsoleTest;

public class ChangeLanguageCommandTests
{
    private readonly Mock<ILanguageConfig> _mockConfig;
    private readonly LanguageApplicationService _languageService;
    private readonly LanguageManager _languageManager;
    private readonly ChangeLanguageCommand _command;

    public ChangeLanguageCommandTests()
    {
        _mockConfig = new Mock<ILanguageConfig>();
        _mockConfig.Setup(c => c.GetLanguage()).Returns(Language.EN);
        _languageService = new LanguageApplicationService(_mockConfig.Object);
        _languageManager = new LanguageManager(_languageService);
        _command = new ChangeLanguageCommand(_languageService, _languageManager, TextWriter.Null);
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

    [Fact]
    public void Execute_FR_ShouldOutputTranslatedMessage()
    {
        _mockConfig.Setup(c => c.GetLanguage()).Returns(Language.FR);
        var output = new StringWriter();
        var command = new ChangeLanguageCommand(_languageService, _languageManager, output);

        command.Execute(new List<string> { "FR" });

        Assert.Equal("Langue changee avec succes.", output.ToString().Trim());
    }

    [Fact]
    public void Execute_EN_ShouldOutputTranslatedMessage()
    {
        _mockConfig.Setup(c => c.GetLanguage()).Returns(Language.EN);
        var output = new StringWriter();
        var command = new ChangeLanguageCommand(_languageService, _languageManager, output);

        command.Execute(new List<string> { "EN" });

        Assert.Equal("Language changed successfully.", output.ToString().Trim());
    }
}
