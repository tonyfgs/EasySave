using Application.Ports;
using Application.Services;
using EasySave.Commands;
using EasySave.UI;
using EasySave.Utilities;
using Moq;
using Shared;

namespace ConsoleTest;

public class ConsoleUITests
{
    private readonly LanguageManager _languageManager;
    private readonly InputParser _inputParser;
    private readonly Mock<ILanguageConfig> _mockConfig;

    public ConsoleUITests()
    {
        _mockConfig = new Mock<ILanguageConfig>();
        _mockConfig.Setup(c => c.GetLanguage()).Returns(Language.EN);
        var languageService = new LanguageApplicationService(_mockConfig.Object);
        _languageManager = new LanguageManager(languageService);
        _inputParser = new InputParser();
    }

    private (ConsoleUI ui, StringWriter output) CreateUI(
        string userInput, Dictionary<string, ICommand>? commands = null)
    {
        var reader = new StringReader(userInput);
        var writer = new StringWriter();
        var cmds = commands ?? new Dictionary<string, ICommand>
        {
            ["7"] = new ExitCommand()
        };
        var ui = new ConsoleUI(_languageManager, _inputParser, cmds, reader, writer);
        return (ui, writer);
    }

    [Fact]
    public void Run_ExitImmediately_ShouldDisplayMenuAndExit()
    {
        var (ui, output) = CreateUI("7\n");

        ui.Run();

        var text = output.ToString();
        Assert.Contains("EasySave - Backup Manager", text);
        Assert.Contains("7. Exit", text);
        Assert.Contains("Goodbye!", text);
    }

    [Fact]
    public void Run_FrenchLanguage_ShouldDisplayFrenchMenu()
    {
        _mockConfig.Setup(c => c.GetLanguage()).Returns(Language.FR);
        var (ui, output) = CreateUI("7\n");

        ui.Run();

        var text = output.ToString();
        Assert.Contains("EasySave - Gestionnaire de Sauvegardes", text);
        Assert.Contains("7. Quitter", text);
        Assert.Contains("Au revoir !", text);
    }

    [Fact]
    public void Run_InvalidChoice_ShouldDisplayErrorAndContinue()
    {
        var (ui, output) = CreateUI("99\n7\n");

        ui.Run();

        Assert.Contains("Invalid choice", output.ToString());
    }

    [Fact]
    public void Run_ListCommand_ShouldDispatchToListCommand()
    {
        var mockCommand = new Mock<ICommand>();
        mockCommand.Setup(c => c.Execute(It.IsAny<List<string>>()))
            .Returns(CommandResult.Ok());

        var commands = new Dictionary<string, ICommand>
        {
            ["2"] = mockCommand.Object,
            ["7"] = new ExitCommand()
        };

        var (ui, _) = CreateUI("2\n7\n", commands);
        ui.Run();

        mockCommand.Verify(c => c.Execute(It.IsAny<List<string>>()), Times.Once);
    }

    [Fact]
    public void Run_FailedCommand_ShouldDisplayError()
    {
        var mockCommand = new Mock<ICommand>();
        mockCommand.Setup(c => c.Execute(It.IsAny<List<string>>()))
            .Returns(CommandResult.Fail("Job limit exceeded"));

        var commands = new Dictionary<string, ICommand>
        {
            ["1"] = mockCommand.Object,
            ["7"] = new ExitCommand()
        };

        var (ui, output) = CreateUI("1\nname\n/s\n/d\nFull\n7\n", commands);
        ui.Run();

        Assert.Contains("Job limit exceeded", output.ToString());
    }

    [Fact]
    public void Run_CreateCommand_ShouldPromptForArgsAndDispatch()
    {
        var mockCommand = new Mock<ICommand>();
        mockCommand.Setup(c => c.Execute(It.IsAny<List<string>>()))
            .Returns(CommandResult.Ok());

        var commands = new Dictionary<string, ICommand>
        {
            ["1"] = mockCommand.Object,
            ["7"] = new ExitCommand()
        };

        var (ui, _) = CreateUI("1\nMyBackup\n/src\n/dst\nFull\n7\n", commands);
        ui.Run();

        mockCommand.Verify(c => c.Execute(
            It.Is<List<string>>(args =>
                args.Count == 4 &&
                args[0] == "MyBackup" &&
                args[1] == "/src" &&
                args[2] == "/dst" &&
                args[3] == "Full")),
            Times.Once);
    }

    [Fact]
    public void Run_ExecuteCommand_RangeInput_ShouldParseAndDispatch()
    {
        var mockCommand = new Mock<ICommand>();
        mockCommand.Setup(c => c.Execute(It.IsAny<List<string>>()))
            .Returns(CommandResult.Ok());

        var commands = new Dictionary<string, ICommand>
        {
            ["5"] = mockCommand.Object,
            ["7"] = new ExitCommand()
        };

        var (ui, _) = CreateUI("5\n1-3\n7\n", commands);
        ui.Run();

        mockCommand.Verify(c => c.Execute(
            It.Is<List<string>>(args =>
                args.Count == 3 &&
                args[0] == "1" && args[1] == "2" && args[2] == "3")),
            Times.Once);
    }

    [Fact]
    public void Run_ExecuteCommand_SemicolonInput_ShouldParseAndDispatch()
    {
        var mockCommand = new Mock<ICommand>();
        mockCommand.Setup(c => c.Execute(It.IsAny<List<string>>()))
            .Returns(CommandResult.Ok());

        var commands = new Dictionary<string, ICommand>
        {
            ["5"] = mockCommand.Object,
            ["7"] = new ExitCommand()
        };

        var (ui, _) = CreateUI("5\n1;3\n7\n", commands);
        ui.Run();

        mockCommand.Verify(c => c.Execute(
            It.Is<List<string>>(args =>
                args.Count == 2 &&
                args[0] == "1" && args[1] == "3")),
            Times.Once);
    }

    [Fact]
    public void Run_ExecuteCommand_SingleId_ShouldParseAndDispatch()
    {
        var mockCommand = new Mock<ICommand>();
        mockCommand.Setup(c => c.Execute(It.IsAny<List<string>>()))
            .Returns(CommandResult.Ok());

        var commands = new Dictionary<string, ICommand>
        {
            ["5"] = mockCommand.Object,
            ["7"] = new ExitCommand()
        };

        var (ui, _) = CreateUI("5\n2\n7\n", commands);
        ui.Run();

        mockCommand.Verify(c => c.Execute(
            It.Is<List<string>>(args => args.Count == 1 && args[0] == "2")),
            Times.Once);
    }

    [Fact]
    public void Run_DeleteCommand_ShouldPromptForIdAndDispatch()
    {
        var mockCommand = new Mock<ICommand>();
        mockCommand.Setup(c => c.Execute(It.IsAny<List<string>>()))
            .Returns(CommandResult.Ok());

        var commands = new Dictionary<string, ICommand>
        {
            ["4"] = mockCommand.Object,
            ["7"] = new ExitCommand()
        };

        var (ui, output) = CreateUI("4\n3\n7\n", commands);
        ui.Run();

        mockCommand.Verify(c => c.Execute(
            It.Is<List<string>>(args => args.Count == 1 && args[0] == "3")),
            Times.Once);
        Assert.Contains("Enter job ID", output.ToString());
    }

    [Fact]
    public void Run_ModifyCommand_ShouldPromptForAllFieldsAndDispatch()
    {
        var mockCommand = new Mock<ICommand>();
        mockCommand.Setup(c => c.Execute(It.IsAny<List<string>>()))
            .Returns(CommandResult.Ok());

        var commands = new Dictionary<string, ICommand>
        {
            ["3"] = mockCommand.Object,
            ["7"] = new ExitCommand()
        };

        var (ui, _) = CreateUI("3\n1\nNewName\n/new/src\n/new/dst\nDifferential\n7\n", commands);
        ui.Run();

        mockCommand.Verify(c => c.Execute(
            It.Is<List<string>>(args =>
                args.Count == 5 &&
                args[0] == "1" &&
                args[1] == "NewName" &&
                args[2] == "/new/src" &&
                args[3] == "/new/dst" &&
                args[4] == "Differential")),
            Times.Once);
    }

    [Fact]
    public void Run_EOFInput_ShouldTerminateGracefully()
    {
        var (ui, _) = CreateUI("");

        var exception = Record.Exception(() => ui.Run());

        Assert.Null(exception);
    }

    [Fact]
    public void Run_ExecuteCommand_StarInput_ShouldDispatchAllMarker()
    {
        var mockCommand = new Mock<ICommand>();
        mockCommand.Setup(c => c.Execute(It.IsAny<List<string>>()))
            .Returns(CommandResult.Ok());

        var commands = new Dictionary<string, ICommand>
        {
            ["5"] = mockCommand.Object,
            ["7"] = new ExitCommand()
        };

        var (ui, _) = CreateUI("5\n*\n7\n", commands);
        ui.Run();

        mockCommand.Verify(c => c.Execute(
            It.Is<List<string>>(args => args.Count == 1 && args[0] == "*")),
            Times.Once);
    }

    [Fact]
    public void Run_ExecutePrompt_EN_ShouldMentionStarForAll()
    {
        var mockCommand = new Mock<ICommand>();
        mockCommand.Setup(c => c.Execute(It.IsAny<List<string>>()))
            .Returns(CommandResult.Ok());

        var commands = new Dictionary<string, ICommand>
        {
            ["5"] = mockCommand.Object,
            ["7"] = new ExitCommand()
        };

        var (ui, output) = CreateUI("5\n1\n7\n", commands);
        ui.Run();

        var text = output.ToString();
        Assert.Contains("*", text);
        Assert.Contains("1-3", text);
        Assert.Contains("1;3", text);
    }

    [Fact]
    public void Run_ExecutePrompt_FR_ShouldMentionStarPourTous()
    {
        _mockConfig.Setup(c => c.GetLanguage()).Returns(Language.FR);

        var mockCommand = new Mock<ICommand>();
        mockCommand.Setup(c => c.Execute(It.IsAny<List<string>>()))
            .Returns(CommandResult.Ok());

        var commands = new Dictionary<string, ICommand>
        {
            ["5"] = mockCommand.Object,
            ["7"] = new ExitCommand()
        };

        var (ui, output) = CreateUI("5\n1\n7\n", commands);
        ui.Run();

        Assert.Contains("* pour tous", output.ToString());
    }
}
