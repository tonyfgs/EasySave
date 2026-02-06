using Application.Services;
using Model;

namespace EasySave.Commands;

public class CreateJobCommand : ICommand
{
    private readonly JobManagementService _jobService;
    private readonly TextWriter _output;

    public CreateJobCommand(JobManagementService jobService, TextWriter? output = null)
    {
        _jobService = jobService;
        _output = output ?? Console.Out;
    }

    public CommandResult Execute(List<string> args)
    {
        try
        {
            var name = args[0];
            var source = args[1];
            var target = args[2];
            var type = Enum.Parse<BackupType>(args[3], ignoreCase: true);

            var job = _jobService.CreateJob(name, source, target, type);
            _output.WriteLine($"Job '{job.Name}' created with ID {job.Id}.");
            return CommandResult.Ok();
        }
        catch (Exception ex) when (ex is DomainException or ArgumentException
            or ArgumentOutOfRangeException or IndexOutOfRangeException)
        {
            return CommandResult.Fail(ex.Message);
        }
    }
}
