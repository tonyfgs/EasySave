using Application.Services;

namespace EasySave.Commands;

public class ExecuteJobCommand : ICommand
{
    private readonly BackupExecutionService _executionService;
    private readonly TextWriter _output;

    public ExecuteJobCommand(BackupExecutionService executionService, TextWriter? output = null)
    {
        _executionService = executionService;
        _output = output ?? Console.Out;
    }

    public CommandResult Execute(List<string> args)
    {
        try
        {
            var results = args.Count == 1 && args[0] == "*"
                ? _executionService.ExecuteAllJobs()
                : _executionService.ExecuteJobs(args.Select(int.Parse).ToList());

            foreach (var r in results)
            {
                if (r.Result.Success)
                    _output.WriteLine($"Job {r.JobId}: OK ({r.Result.FilesProcessed} files, {r.Result.BytesTransferred} bytes)");
                else
                    _output.WriteLine($"Job {r.JobId}: FAILED - {string.Join(", ", r.Result.Errors)}");
            }

            return results.All(r => r.Result.Success)
                ? CommandResult.Ok()
                : CommandResult.Fail("Some jobs failed execution.");
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            return CommandResult.Fail(ex.Message);
        }
    }
}
