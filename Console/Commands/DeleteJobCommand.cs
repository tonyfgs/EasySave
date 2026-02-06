using Application.Services;
using Model;

namespace EasySave.Commands;

public class DeleteJobCommand : ICommand
{
    private readonly JobManagementService _jobService;
    private readonly TextWriter _output;

    public DeleteJobCommand(JobManagementService jobService, TextWriter? output = null)
    {
        _jobService = jobService;
        _output = output ?? Console.Out;
    }

    public CommandResult Execute(List<string> args)
    {
        try
        {
            var id = int.Parse(args[0]);
            _jobService.DeleteJob(id);
            _output.WriteLine($"Job {id} deleted.");
            return CommandResult.Ok();
        }
        catch (Exception ex) when (ex is DomainException or FormatException
            or IndexOutOfRangeException or ArgumentOutOfRangeException)
        {
            return CommandResult.Fail(ex.Message);
        }
    }
}
