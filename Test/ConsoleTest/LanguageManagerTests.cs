using Application.Ports;
using Application.Services;
using EasySave.UI;
using Moq;
using Shared;

namespace ConsoleTest;

public class LanguageManagerTests
{
    private readonly Mock<ILanguageConfig> _mockConfig;
    private readonly LanguageManager _manager;

    public LanguageManagerTests()
    {
        _mockConfig = new Mock<ILanguageConfig>();
        _mockConfig.Setup(c => c.GetLanguage()).Returns(Language.EN);
        var languageService = new LanguageApplicationService(_mockConfig.Object);
        _manager = new LanguageManager(languageService);
    }

    [Fact]
    public void GetCurrentLanguage_ShouldDelegateToService()
    {
        var result = _manager.GetCurrentLanguage();

        Assert.Equal(Language.EN, result);
    }

    [Fact]
    public void GetString_EnglishKey_ShouldReturnEnglishValue()
    {
        _mockConfig.Setup(c => c.GetLanguage()).Returns(Language.EN);

        var result = _manager.GetString("menu.title");

        Assert.Equal("EasySave - Backup Manager", result);
    }

    [Fact]
    public void GetString_FrenchKey_ShouldReturnFrenchValue()
    {
        _mockConfig.Setup(c => c.GetLanguage()).Returns(Language.FR);

        var result = _manager.GetString("menu.title");

        Assert.Equal("EasySave - Gestionnaire de Sauvegardes", result);
    }

    [Fact]
    public void GetString_UnknownKey_ShouldReturnBracketedKey()
    {
        var result = _manager.GetString("nonexistent.key");

        Assert.Equal("[nonexistent.key]", result);
    }

    [Theory]
    [InlineData("menu.title")]
    [InlineData("menu.create")]
    [InlineData("menu.list")]
    [InlineData("menu.modify")]
    [InlineData("menu.delete")]
    [InlineData("menu.execute")]
    [InlineData("menu.language")]
    [InlineData("menu.exit")]
    [InlineData("prompt.choice")]
    [InlineData("prompt.name")]
    [InlineData("prompt.source")]
    [InlineData("prompt.target")]
    [InlineData("prompt.type")]
    [InlineData("prompt.id")]
    [InlineData("prompt.language")]
    [InlineData("prompt.execute_input")]
    [InlineData("error.invalid_choice")]
    [InlineData("error.generic")]
    [InlineData("success.job_created")]
    [InlineData("success.job_deleted")]
    [InlineData("success.job_modified")]
    [InlineData("success.language_changed")]
    [InlineData("info.no_jobs")]
    [InlineData("info.goodbye")]
    public void GetString_AllRequiredKeys_ShouldExistInBothLanguages(string key)
    {
        _mockConfig.Setup(c => c.GetLanguage()).Returns(Language.EN);
        var enResult = _manager.GetString(key);
        Assert.DoesNotContain("[", enResult);

        _mockConfig.Setup(c => c.GetLanguage()).Returns(Language.FR);
        var frResult = _manager.GetString(key);
        Assert.DoesNotContain("[", frResult);
    }

    [Fact]
    public void GetString_AfterLanguageChange_ShouldReflectNewLanguage()
    {
        _mockConfig.Setup(c => c.GetLanguage()).Returns(Language.EN);
        var enResult = _manager.GetString("menu.title");
        Assert.Equal("EasySave - Backup Manager", enResult);

        _mockConfig.Setup(c => c.GetLanguage()).Returns(Language.FR);
        var frResult = _manager.GetString("menu.title");
        Assert.Equal("EasySave - Gestionnaire de Sauvegardes", frResult);
    }
}
