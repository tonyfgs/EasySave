using Shared;

namespace Application.Ports;

public interface ILanguageConfig
{
    Language GetLanguage();
    void SetLanguage(Language lang);
}
