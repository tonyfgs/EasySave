using Application.Services;
using Model;

namespace EasySave.Commands;

public class ModifyJobCommand : ICommand
{
    private readonly JobManagementService _jobService;
    private readonly TextWriter _output;

    public ModifyJobCommand(JobManagementService jobService, TextWriter? output = null)
    {
        _jobService = jobService;
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
            _output.WriteLine($"Job {id} modified.");
            return CommandResult.Ok();
        }
        catch (Exception ex) when (ex is DomainException or FormatException
            or IndexOutOfRangeException or ArgumentOutOfRangeException or ArgumentException)
        {
            return CommandResult.Fail(ex.Message);
        }
    }
}
