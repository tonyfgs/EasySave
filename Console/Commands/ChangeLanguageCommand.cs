using Application.Services;
using EasySave.UI;
using Shared;

namespace EasySave.Commands;

public class ChangeLanguageCommand : ICommand
{
    private readonly LanguageApplicationService _languageService;
    private readonly LanguageManager _languageManager;
    private readonly TextWriter _output;

    public ChangeLanguageCommand(LanguageApplicationService languageService, LanguageManager languageManager, TextWriter? output = null)
    {
        _languageService = languageService;
        _languageManager = languageManager;
        _output = output ?? Console.Out;
    }

    public CommandResult Execute(List<string> args)
    {
        try
        {
            var language = Enum.Parse<Language>(args[0], ignoreCase: true);
            _languageService.ChangeLanguage(language);
            _output.WriteLine(_languageManager.GetString("success.language_changed"));
            return CommandResult.Ok();
        }
        catch (Exception ex) when (ex is ArgumentException or IndexOutOfRangeException
            or ArgumentOutOfRangeException)
        {
            return CommandResult.Fail(ex.Message);
        }
    }
}
