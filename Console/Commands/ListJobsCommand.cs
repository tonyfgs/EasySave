using Application.Services;
using EasySave.UI;

namespace EasySave.Commands;

public class ListJobsCommand : ICommand
{
    private readonly JobManagementService _jobService;
    private readonly LanguageManager _languageManager;
    private readonly TextWriter _output;

    public ListJobsCommand(JobManagementService jobService, LanguageManager languageManager, TextWriter? output = null)
    {
        _jobService = jobService;
        _languageManager = languageManager;
        _output = output ?? Console.Out;
    }

    public CommandResult Execute(List<string> args)
    {
        var jobs = _jobService.ListJobs();
        if (jobs.Count == 0)
        {
            _output.WriteLine(_languageManager.GetString("info.no_jobs"));
            return CommandResult.Ok();
        }
        foreach (var job in jobs)
        {
            _output.WriteLine($"[{job.Id}] {job.Name} | {job.SourcePath} -> {job.TargetPath} | {job.Type}");
        }
        return CommandResult.Ok();
    }
}
