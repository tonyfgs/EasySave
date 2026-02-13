using Application.Services;
using EasySave.UI;
using Model;

namespace EasySave.Commands;

public class DeleteJobCommand : ICommand
{
    private readonly JobManagementService _jobService;
    private readonly LanguageManager _languageManager;
    private readonly TextWriter _output;

    public DeleteJobCommand(JobManagementService jobService, LanguageManager languageManager, TextWriter? output = null)
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
            _jobService.DeleteJob(id);
            _output.WriteLine(_languageManager.GetFormattedString("success.job_deleted", id));
            return CommandResult.Ok();
        }
        catch (Exception ex) when (ex is DomainException or FormatException
            or IndexOutOfRangeException or ArgumentOutOfRangeException)
        {
            return CommandResult.Fail(ex.Message);
        }
    }
}
