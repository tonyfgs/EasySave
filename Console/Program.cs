using Application.Events;
using Application.Handlers;
using Application.Services;
using EasySave.Commands;
using EasySave.UI;
using EasySave.Utilities;
using Infrastructure;
using Logger.Service;
using Model;

namespace EasySave;

public class Program
{
    public static void Main(string[] args)
    {
        var basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EasySave");

        var configPath = Path.Combine(basePath, "config.json");
        var logDirectory = Path.Combine(basePath, "logs");
        var jobsPath = Path.Combine(basePath, "jobs.json");
        var statePath = Path.Combine(basePath, "state.json");

        var appConfig = new AppConfiguration(configPath, logDirectory);
        var easyLogger = new DailyLogsService();
        var transferLoggerFactory = new TransferLoggerFactory(
            appConfig.GetLogFormat(), easyLogger, appConfig.GetLogDirectory());
        var transferLogger = transferLoggerFactory.Create();

        var eventBus = new InProcessEventBus();
        var stateManager = new JsonStateManager(statePath);
        var transferCompletedHandler = new TransferCompletedHandler(transferLogger);
        var stateChangedHandler = new StateChangedHandler(stateManager);
        eventBus.Subscribe(transferCompletedHandler);
        eventBus.Subscribe(stateChangedHandler);

        var fileSystem = new LocalFileSystemGateway();
        var pathAdapter = new UNCPathAdapter();
        var jobRepository = new FileJobRepository(jobsPath);
        var domainService = new BackupDomainService();
        var tracker = new ProgressTracker();

        var languageService = new LanguageApplicationService(appConfig);
        var backupExecutor = new BackupExecutor(
            fileSystem, pathAdapter, eventBus, domainService, tracker);
        var strategyFactory = new BackupStrategyFactory();
        var jobService = new JobManagementService(jobRepository, domainService);
        var executionService = new BackupExecutionService(
            jobRepository, backupExecutor, strategyFactory);

        var languageManager = new LanguageManager(languageService);
        var inputParser = new InputParser();

        var output = Console.Out;
        var commands = new Dictionary<string, ICommand>
        {
            ["1"] = new CreateJobCommand(jobService, output),
            ["2"] = new ListJobsCommand(jobService, output),
            ["3"] = new ModifyJobCommand(jobService, output),
            ["4"] = new DeleteJobCommand(jobService, output),
            ["5"] = new ExecuteJobCommand(executionService, output),
            ["6"] = new ChangeLanguageCommand(languageService, output),
            ["7"] = new ExitCommand(),
        };

        if (args.Length > 0)
        {
            HandleCommandLineArgs(args, inputParser, executionService, output);
            return;
        }

        var ui = new ConsoleUI(
            languageManager, inputParser, commands,
            Console.In, output);
        ui.Run();
    }

    private static void HandleCommandLineArgs(
        string[] args, InputParser inputParser,
        BackupExecutionService executionService, TextWriter output)
    {
        var input = args[0];
        List<int> jobIds;

        if (input.Contains('-'))
            jobIds = inputParser.ParseJobRange(input);
        else if (input.Contains(';'))
            jobIds = inputParser.ParseJobList(input);
        else
            jobIds = new List<int> { int.Parse(input) };

        var executeCommand = new ExecuteJobCommand(executionService, output);
        var stringIds = jobIds.Select(id => id.ToString()).ToList();
        var result = executeCommand.Execute(stringIds);

        if (!result.IsSuccess())
            output.WriteLine($"Error: {result.GetErrorMessage()}");
    }
}
