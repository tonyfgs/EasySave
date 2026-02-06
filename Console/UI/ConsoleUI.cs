using EasySave.Commands;
using EasySave.Utilities;

namespace EasySave.UI;

public class ConsoleUI
{
    private readonly LanguageManager _languageManager;
    private readonly InputParser _inputParser;
    private readonly Dictionary<string, ICommand> _commands;
    private readonly TextReader _input;
    private readonly TextWriter _output;

    public ConsoleUI(
        LanguageManager languageManager,
        InputParser inputParser,
        Dictionary<string, ICommand> commands,
        TextReader input,
        TextWriter output)
    {
        _languageManager = languageManager;
        _inputParser = inputParser;
        _commands = commands;
        _input = input;
        _output = output;
    }

    public void Run()
    {
        while (true)
        {
            ShowMenu();
            var choice = ReadInput();

            if (string.IsNullOrEmpty(choice) || choice == "7")
            {
                if (_commands.TryGetValue("7", out var exitCmd))
                    exitCmd.Execute(new List<string>());
                _output.WriteLine(_languageManager.GetString("info.goodbye"));
                break;
            }

            DispatchCommand(choice);
        }
    }

    private void ShowMenu()
    {
        _output.WriteLine();
        _output.WriteLine(_languageManager.GetString("menu.title"));
        _output.WriteLine(new string('-', 40));
        _output.WriteLine(_languageManager.GetString("menu.create"));
        _output.WriteLine(_languageManager.GetString("menu.list"));
        _output.WriteLine(_languageManager.GetString("menu.modify"));
        _output.WriteLine(_languageManager.GetString("menu.delete"));
        _output.WriteLine(_languageManager.GetString("menu.execute"));
        _output.WriteLine(_languageManager.GetString("menu.language"));
        _output.WriteLine(_languageManager.GetString("menu.exit"));
    }

    private string ReadInput()
    {
        _output.Write(_languageManager.GetString("prompt.choice"));
        return _input.ReadLine()?.Trim() ?? string.Empty;
    }

    private void DispatchCommand(string name)
    {
        if (!_commands.TryGetValue(name, out var command))
        {
            _output.WriteLine(_languageManager.GetString("error.invalid_choice"));
            return;
        }

        var args = GatherArgs(name);
        var result = command.Execute(args);
        if (!result.IsSuccess())
        {
            _output.Write(_languageManager.GetString("error.generic"));
            _output.WriteLine(result.GetErrorMessage());
        }
    }

    private List<string> GatherArgs(string commandKey)
    {
        return commandKey switch
        {
            "1" => GatherCreateArgs(),
            "3" => GatherModifyArgs(),
            "4" => GatherDeleteArgs(),
            "5" => GatherExecuteArgs(),
            "6" => GatherLanguageArgs(),
            _ => new List<string>()
        };
    }

    private List<string> GatherCreateArgs()
    {
        _output.Write(_languageManager.GetString("prompt.name"));
        var name = _input.ReadLine()?.Trim() ?? string.Empty;
        _output.Write(_languageManager.GetString("prompt.source"));
        var source = _input.ReadLine()?.Trim() ?? string.Empty;
        _output.Write(_languageManager.GetString("prompt.target"));
        var target = _input.ReadLine()?.Trim() ?? string.Empty;
        _output.Write(_languageManager.GetString("prompt.type"));
        var type = _input.ReadLine()?.Trim() ?? string.Empty;
        return new List<string> { name, source, target, type };
    }

    private List<string> GatherModifyArgs()
    {
        _output.Write(_languageManager.GetString("prompt.id"));
        var id = _input.ReadLine()?.Trim() ?? string.Empty;
        _output.Write(_languageManager.GetString("prompt.name"));
        var name = _input.ReadLine()?.Trim() ?? string.Empty;
        _output.Write(_languageManager.GetString("prompt.source"));
        var source = _input.ReadLine()?.Trim() ?? string.Empty;
        _output.Write(_languageManager.GetString("prompt.target"));
        var target = _input.ReadLine()?.Trim() ?? string.Empty;
        _output.Write(_languageManager.GetString("prompt.type"));
        var type = _input.ReadLine()?.Trim() ?? string.Empty;
        return new List<string> { id, name, source, target, type };
    }

    private List<string> GatherDeleteArgs()
    {
        _output.Write(_languageManager.GetString("prompt.id"));
        var id = _input.ReadLine()?.Trim() ?? string.Empty;
        return new List<string> { id };
    }

    private List<string> GatherExecuteArgs()
    {
        _output.Write(_languageManager.GetString("prompt.execute_input"));
        var input = _input.ReadLine()?.Trim() ?? string.Empty;

        if (input == "*")
            return new List<string> { "*" };

        List<int> jobIds;
        if (input.Contains('-'))
            jobIds = _inputParser.ParseJobRange(input);
        else if (input.Contains(';'))
            jobIds = _inputParser.ParseJobList(input);
        else
            jobIds = new List<int> { int.Parse(input) };

        return jobIds.Select(id => id.ToString()).ToList();
    }

    private List<string> GatherLanguageArgs()
    {
        _output.Write(_languageManager.GetString("prompt.language"));
        var lang = _input.ReadLine()?.Trim() ?? string.Empty;
        return new List<string> { lang };
    }
}
