namespace EasySave.Commands;

public class ExitCommand : ICommand
{
    public CommandResult Execute(List<string> args)
    {
        return CommandResult.Ok();
    }
}
