using Application.Ports;
using Shared;

namespace Application.Services;

public class LanguageApplicationService
{
    private readonly ILanguageConfig _languageConfig;

    public LanguageApplicationService(ILanguageConfig languageConfig)
    {
        _languageConfig = languageConfig;
    }

    public Language GetCurrentLanguage()
    {
        return _languageConfig.GetLanguage();
    }

    public void ChangeLanguage(Language lang)
    {
        _languageConfig.SetLanguage(lang);
    }
}
