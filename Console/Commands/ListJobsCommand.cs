using Application.Services;

namespace EasySave.Commands;

public class ListJobsCommand : ICommand
{
    private readonly JobManagementService _jobService;
    private readonly TextWriter _output;

    public ListJobsCommand(JobManagementService jobService, TextWriter? output = null)
    {
        _jobService = jobService;
        _output = output ?? Console.Out;
    }

    public CommandResult Execute(List<string> args)
    {
        var jobs = _jobService.ListJobs();
        if (jobs.Count == 0)
        {
            _output.WriteLine("No backup jobs found.");
            return CommandResult.Ok();
        }
        foreach (var job in jobs)
        {
            _output.WriteLine($"[{job.Id}] {job.Name} | {job.SourcePath} -> {job.TargetPath} | {job.Type}");
        }
        return CommandResult.Ok();
    }
}
