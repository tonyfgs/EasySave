using EasySave.Commands;

namespace ConsoleTest;

public class ExitCommandTests
{
    [Fact]
    public void Execute_ShouldReturnSuccess()
    {
        var command = new ExitCommand();

        var result = command.Execute(new List<string>());

        Assert.True(result.IsSuccess());
    }

    [Fact]
    public void ExitCommand_ShouldImplementICommand()
    {
        var command = new ExitCommand();

        Assert.IsAssignableFrom<ICommand>(command);
    }
}
