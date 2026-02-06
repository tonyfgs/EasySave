using Application.Ports;
using Application.Services;
using Moq;
using Shared;

namespace ApplicationTest;

public class LanguageApplicationServiceTests
{
    [Fact]
    public void GetCurrentLanguage_ShouldDelegateToConfig()
    {
        var mockConfig = new Mock<ILanguageConfig>();
        mockConfig.Setup(c => c.GetLanguage()).Returns(Language.FR);
        var service = new LanguageApplicationService(mockConfig.Object);

        var result = service.GetCurrentLanguage();

        Assert.Equal(Language.FR, result);
    }

    [Fact]
    public void ChangeLanguage_ShouldDelegateToConfig()
    {
        var mockConfig = new Mock<ILanguageConfig>();
        var service = new LanguageApplicationService(mockConfig.Object);

        service.ChangeLanguage(Language.EN);

        mockConfig.Verify(c => c.SetLanguage(Language.EN), Times.Once);
    }
}
