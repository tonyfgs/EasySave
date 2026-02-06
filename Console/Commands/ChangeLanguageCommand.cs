using Application.Services;
using Shared;

namespace EasySave.Commands;

public class ChangeLanguageCommand : ICommand
{
    private readonly LanguageApplicationService _languageService;
    private readonly TextWriter _output;

    public ChangeLanguageCommand(LanguageApplicationService languageService, TextWriter? output = null)
    {
        _languageService = languageService;
        _output = output ?? Console.Out;
    }

    public CommandResult Execute(List<string> args)
    {
        try
        {
            var language = Enum.Parse<Language>(args[0], ignoreCase: true);
            _languageService.ChangeLanguage(language);
            _output.WriteLine($"Language changed to {language}.");
            return CommandResult.Ok();
        }
        catch (Exception ex) when (ex is ArgumentException or IndexOutOfRangeException
            or ArgumentOutOfRangeException)
        {
            return CommandResult.Fail(ex.Message);
        }
    }
}
