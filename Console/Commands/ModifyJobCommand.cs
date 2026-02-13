using Application.Services;
using EasySave.UI;
using Model;

namespace EasySave.Commands;

public class ModifyJobCommand : ICommand
{
    private readonly JobManagementService _jobService;
    private readonly LanguageManager _languageManager;
    private readonly TextWriter _output;

    public ModifyJobCommand(JobManagementService jobService, LanguageManager languageManager, TextWriter? output = null)
    {
        _jobService = jobService;
        _languageManager = languageManager;
        _output = output ?? Console.Out;
    }

    public CommandResult Execute(List<string> args)
    {
        try
        {
            var id = int.Parse(args[0]);
            var name = args[1];
            var source = args[2];
            var target = args[3];
            var type = Enum.Parse<BackupType>(args[4], ignoreCase: true);

            _jobService.ModifyJob(id, name, source, target, type);
            _output.WriteLine(_languageManager.GetFormattedString("success.job_modified", id));
            return CommandResult.Ok();
        }
        catch (Exception ex) when (ex is DomainException or FormatException
            or IndexOutOfRangeException or ArgumentOutOfRangeException or ArgumentException)
        {
            return CommandResult.Fail(ex.Message);
        }
    }
}
