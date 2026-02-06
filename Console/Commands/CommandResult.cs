namespace EasySave.Commands;

public class CommandResult
{
    private readonly bool _success;
    private readonly string _errorMessage;

    private CommandResult(bool success, string errorMessage)
    {
        _success = success;
        _errorMessage = errorMessage;
    }

    public static CommandResult Ok()
    {
        return new CommandResult(true, string.Empty);
    }

    public static CommandResult Fail(string message)
    {
        return new CommandResult(false, message ?? string.Empty);
    }

    public bool IsSuccess() => _success;

    public string GetErrorMessage() => _errorMessage;
}
