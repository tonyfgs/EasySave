namespace EasySave.Commands;

public interface ICommand
{
    CommandResult Execute(List<string> args);
}
