using Application.Concurrency;
using Application.Events;
using Application.Handlers;
using Application.Ports;
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

        var encryptionService = new DisabledEncryptionService();
        IEncryptionConfig encryptionConfig = appConfig;
        var businessSoftwareDetector = new DisabledBusinessSoftwareDetector();
        IBusinessSoftwareConfig businessSoftwareConfig = appConfig;
        ILargeFileConfig largeFileConfig = appConfig;
        var largeFileLock = new SemaphoreLargeFileTransferLock();
        IPriorityFileConfig priorityFileConfig = appConfig;
        var priorityFileGate = new PriorityFileGate();

        var languageService = new LanguageApplicationService(appConfig);
        var backupExecutor = new BackupExecutor(
            fileSystem, pathAdapter, eventBus, domainService,
            encryptionService, encryptionConfig, businessSoftwareDetector,
            businessSoftwareConfig, largeFileLock, largeFileConfig,
            priorityFileConfig, priorityFileGate);
        var strategyFactory = new BackupStrategyFactory();
        var jobService = new JobManagementService(jobRepository);
        var watcher = new BusinessSoftwareWatcher(
            businessSoftwareDetector, businessSoftwareConfig, backupExecutor);
        var executionService = new BackupExecutionService(
            jobRepository, backupExecutor, strategyFactory, watcher);

        var languageManager = new LanguageManager(languageService);
        var inputParser = new InputParser();

        var output = Console.Out;
        var commands = new Dictionary<string, ICommand>
        {
            ["1"] = new CreateJobCommand(jobService, languageManager, output),
            ["2"] = new ListJobsCommand(jobService, languageManager, output),
            ["3"] = new ModifyJobCommand(jobService, languageManager, output),
            ["4"] = new DeleteJobCommand(jobService, languageManager, output),
            ["5"] = new ExecuteJobCommand(executionService, output),
            ["6"] = new ChangeLanguageCommand(languageService, languageManager, output),
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

        try
        {
            jobIds = inputParser.ParseInput(input);
        }
        catch (FormatException ex)
        {
            output.WriteLine($"Error: {ex.Message}");
            output.WriteLine("Usage: EasySave <job-id | start-end | id1;id2;...>");
            return;
        }

        var executeCommand = new ExecuteJobCommand(executionService, output);
        var stringIds = jobIds.Select(id => id.ToString()).ToList();
        var result = executeCommand.Execute(stringIds);

        if (!result.IsSuccess())
            output.WriteLine($"Error: {result.GetErrorMessage()}");
    }
}
