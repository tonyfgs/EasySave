using Application.Services;
using EasySave.UI;
using EasySave.Utilities;
using Model;

namespace EasySave.Commands;

public class CreateJobCommand : ICommand
{
    private readonly JobManagementService _jobService;
    private readonly LanguageManager _languageManager;
    private readonly TextWriter _output;

    public CreateJobCommand(JobManagementService jobService, LanguageManager languageManager, TextWriter? output = null)
    {
        _jobService = jobService;
        _languageManager = languageManager;
        _output = output ?? Console.Out;
    }

    public CommandResult Execute(List<string> args)
    {
        try
        {
            var name = args[0];
            var source = args[1];
            var target = args[2];
            var type = BackupTypeParser.Parse(args[3]);

            var job = _jobService.CreateJob(name, source, target, type);
            _output.WriteLine(_languageManager.GetFormattedString("success.job_created", job.Name, job.Id));
            return CommandResult.Ok();
        }
        catch (Exception ex) when (ex is DomainException or ArgumentException
            or ArgumentOutOfRangeException or IndexOutOfRangeException)
        {
            return CommandResult.Fail(ex.Message);
        }
    }
}
