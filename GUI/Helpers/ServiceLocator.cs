using Application.Events;
using Application.Handlers;
using Application.Ports;
using Application.Services;
using GUI.Services;
using Infrastructure;
using Logger.Service;
using Model;

namespace GUI.Helpers;

public static class ServiceLocator
{
    private static bool _initialized;

    // Application Services
    public static JobManagementService JobManagementService { get; private set; } = null!;
    public static BackupExecutionService BackupExecutionService { get; private set; } = null!;
    public static LanguageApplicationService LanguageApplicationService { get; private set; } = null!;

    // GUI Services
    public static LocalizationService LocalizationService { get; private set; } = null!;

    // Infrastructure
    public static IEventBus EventBus { get; private set; } = null!;
    public static IStateManager StateManager { get; private set; } = null!;
    public static AppConfiguration AppConfiguration { get; private set; } = null!;

    public static void Initialize()
    {
        if (_initialized) return;

        var basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EasySave");

        var configPath = Path.Combine(basePath, "config.json");
        var logDirectory = Path.Combine(basePath, "logs");
        var jobsPath = Path.Combine(basePath, "jobs.json");
        var statePath = Path.Combine(basePath, "state.json");

        // Infrastructure
        var appConfig = new AppConfiguration(configPath, logDirectory);
        AppConfiguration = appConfig;

        var easyLogger = new DailyLogsService();
        var transferLogger = new DynamicTransferLogger(appConfig, easyLogger, appConfig.GetLogDirectory());

        var eventBus = new InProcessEventBus();
        EventBus = eventBus;
        var stateManager = new JsonStateManager(statePath);
        StateManager = stateManager;

        var transferCompletedHandler = new TransferCompletedHandler(transferLogger);
        var stateChangedHandler = new StateChangedHandler(stateManager);
        eventBus.Subscribe(transferCompletedHandler);
        eventBus.Subscribe(stateChangedHandler);

        var fileSystem = new LocalFileSystemGateway();
        var pathAdapter = new UNCPathAdapter();
        var jobRepository = new FileJobRepository(jobsPath);
        var domainService = new BackupDomainService();

        var cryptoSoftExe = OperatingSystem.IsWindows() ? "CryptoSoft.exe" : "CryptoSoft";
        var cryptoSoftPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CryptoSoft", cryptoSoftExe);
        var encryptionService = new CryptoSoftAdapter(appConfig, cryptoSoftPath);
        IEncryptionConfig encryptionConfig = appConfig;
        var businessSoftwareDetector = new ProcessDetector(appConfig);
        IBusinessSoftwareConfig businessSoftwareConfig = appConfig;
        ILargeFileConfig largeFileConfig = appConfig;
        var largeFileLock = new SemaphoreLargeFileTransferLock();

        var backupExecutor = new BackupExecutor(
            fileSystem, pathAdapter, eventBus, domainService,
            encryptionService, encryptionConfig, businessSoftwareDetector,
            businessSoftwareConfig, largeFileLock, largeFileConfig);
        var strategyFactory = new BackupStrategyFactory();

        JobManagementService = new JobManagementService(jobRepository);
        BackupExecutionService = new BackupExecutionService(
            jobRepository, backupExecutor, strategyFactory,
            businessSoftwareDetector, businessSoftwareConfig, eventBus);
        LanguageApplicationService = new LanguageApplicationService(appConfig);
        LocalizationService = new LocalizationService(LanguageApplicationService);

        _initialized = true;
    }
}
