using Application.Events;
using Application.Ports;
using Application.Services;
using EasySave.Commands;
using EasySave.UI;
using EasySave.Utilities;
using Model;
using Moq;
using Shared;

namespace ConsoleTest;

public class ConsoleIntegrationTests
{
    private LanguageManager CreateLanguageManager(Mock<ILanguageConfig> mockConfig)
    {
        var languageService = new LanguageApplicationService(mockConfig.Object);
        return new LanguageManager(languageService);
    }

    [Fact]
    public void FullFlow_CreateAndListJob_ShouldShowCreatedJob()
    {
        var mockRepo = new Mock<IJobRepository>();
        mockRepo.Setup(r => r.Count()).Returns(0);
        var createdJobs = new List<BackupJob>();
        mockRepo.Setup(r => r.Save(It.IsAny<BackupJob>()))
            .Callback<BackupJob>(j => { j.Id = 1; createdJobs.Add(j); });
        mockRepo.Setup(r => r.GetAll()).Returns(() => new List<BackupJob>(createdJobs));

        var domainService = new BackupDomainService();
        var jobService = new JobManagementService(mockRepo.Object, domainService);

        var mockConfig = new Mock<ILanguageConfig>();
        mockConfig.Setup(c => c.GetLanguage()).Returns(Language.EN);
        var languageService = new LanguageApplicationService(mockConfig.Object);
        var languageManager = new LanguageManager(languageService);
        var inputParser = new InputParser();

        var output = new StringWriter();
        var commands = new Dictionary<string, ICommand>
        {
            ["1"] = new CreateJobCommand(jobService, output),
            ["2"] = new ListJobsCommand(jobService, output),
            ["7"] = new ExitCommand()
        };

        var input = new StringReader("1\nTestBackup\n/source\n/target\nFull\n2\n7\n");

        var ui = new ConsoleUI(languageManager, inputParser, commands, input, output);
        ui.Run();

        var text = output.ToString();
        Assert.Contains("TestBackup", text);
        Assert.Contains("/source", text);
        Assert.Contains("/target", text);
    }

    [Fact]
    public void FullFlow_DeleteJob_ShouldCallRepositoryDelete()
    {
        var mockRepo = new Mock<IJobRepository>();
        var domainService = new BackupDomainService();
        var jobService = new JobManagementService(mockRepo.Object, domainService);

        var mockConfig = new Mock<ILanguageConfig>();
        mockConfig.Setup(c => c.GetLanguage()).Returns(Language.EN);
        var languageManager = CreateLanguageManager(mockConfig);
        var inputParser = new InputParser();

        var output = new StringWriter();
        var commands = new Dictionary<string, ICommand>
        {
            ["4"] = new DeleteJobCommand(jobService, output),
            ["7"] = new ExitCommand()
        };

        var input = new StringReader("4\n2\n7\n");
        var ui = new ConsoleUI(languageManager, inputParser, commands, input, output);
        ui.Run();

        mockRepo.Verify(r => r.Delete(2), Times.Once);
        Assert.Contains("deleted", output.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FullFlow_ModifyJob_ShouldUpdateRepository()
    {
        var mockRepo = new Mock<IJobRepository>();
        var existingJob = new BackupJob(1, "OldName", "/old/src", "/old/dst", BackupType.Full);
        mockRepo.Setup(r => r.GetById(1)).Returns(existingJob);

        var domainService = new BackupDomainService();
        var jobService = new JobManagementService(mockRepo.Object, domainService);

        var mockConfig = new Mock<ILanguageConfig>();
        mockConfig.Setup(c => c.GetLanguage()).Returns(Language.EN);
        var languageManager = CreateLanguageManager(mockConfig);
        var inputParser = new InputParser();

        var output = new StringWriter();
        var commands = new Dictionary<string, ICommand>
        {
            ["3"] = new ModifyJobCommand(jobService, output),
            ["7"] = new ExitCommand()
        };

        var input = new StringReader("3\n1\nNewName\n/new/src\n/new/dst\nDifferential\n7\n");
        var ui = new ConsoleUI(languageManager, inputParser, commands, input, output);
        ui.Run();

        mockRepo.Verify(r => r.Update(It.Is<BackupJob>(j =>
            j.Name == "NewName" &&
            j.SourcePath == "/new/src" &&
            j.TargetPath == "/new/dst" &&
            j.Type == BackupType.Differential)), Times.Once);
        Assert.Contains("modified", output.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FullFlow_ChangeLanguage_ShouldSwitchToFrench()
    {
        var currentLang = Language.EN;
        var mockConfig = new Mock<ILanguageConfig>();
        mockConfig.Setup(c => c.GetLanguage()).Returns(() => currentLang);
        mockConfig.Setup(c => c.SetLanguage(It.IsAny<Language>()))
            .Callback<Language>(lang => currentLang = lang);

        var languageService = new LanguageApplicationService(mockConfig.Object);
        var languageManager = new LanguageManager(languageService);
        var inputParser = new InputParser();

        var output = new StringWriter();
        var commands = new Dictionary<string, ICommand>
        {
            ["6"] = new ChangeLanguageCommand(languageService, output),
            ["7"] = new ExitCommand()
        };

        var input = new StringReader("6\nFR\n7\n");
        var ui = new ConsoleUI(languageManager, inputParser, commands, input, output);
        ui.Run();

        Assert.Contains("Au revoir", output.ToString());
    }

    [Fact]
    public void FullFlow_ExecuteAllJobs_StarInput_ShouldExecuteAll()
    {
        var mockRepo = new Mock<IJobRepository>();
        var job1 = new BackupJob(1, "J1", "/s1", "/d1", BackupType.Full);
        var job2 = new BackupJob(2, "J2", "/s2", "/d2", BackupType.Full);
        mockRepo.Setup(r => r.GetAll()).Returns(new List<BackupJob> { job1, job2 });
        mockRepo.Setup(r => r.GetById(1)).Returns(job1);
        mockRepo.Setup(r => r.GetById(2)).Returns(job2);

        var mockFileSystem = new Mock<IFileSystemGateway>();
        mockFileSystem.Setup(fs => fs.EnumerateFiles(It.IsAny<string>()))
            .Returns(new List<FileDescriptor>());
        var mockPathAdapter = new Mock<IPathAdapter>();
        mockPathAdapter.Setup(p => p.ToUNC(It.IsAny<string>())).Returns<string>(s => s);
        var mockEventBus = new Mock<IEventBus>();
        var domainService = new BackupDomainService();
        var tracker = new ProgressTracker();

        var executor = new BackupExecutor(
            mockFileSystem.Object, mockPathAdapter.Object,
            mockEventBus.Object, domainService, tracker);
        var strategyFactory = new BackupStrategyFactory();
        var executionService = new BackupExecutionService(mockRepo.Object, executor, strategyFactory);

        var mockConfig = new Mock<ILanguageConfig>();
        mockConfig.Setup(c => c.GetLanguage()).Returns(Language.EN);
        var languageManager = CreateLanguageManager(mockConfig);
        var inputParser = new InputParser();

        var output = new StringWriter();
        var commands = new Dictionary<string, ICommand>
        {
            ["5"] = new ExecuteJobCommand(executionService, output),
            ["7"] = new ExitCommand()
        };

        var input = new StringReader("5\n*\n7\n");
        var ui = new ConsoleUI(languageManager, inputParser, commands, input, output);
        ui.Run();

        var text = output.ToString();
        Assert.Contains("Job 1: OK", text);
        Assert.Contains("Job 2: OK", text);
    }

    [Fact]
    public void FullFlow_ExecuteJobRange_ShouldParseAndExecute()
    {
        var mockRepo = new Mock<IJobRepository>();
        var job1 = new BackupJob(1, "J1", "/s1", "/d1", BackupType.Full);
        var job2 = new BackupJob(2, "J2", "/s2", "/d2", BackupType.Full);
        mockRepo.Setup(r => r.GetById(1)).Returns(job1);
        mockRepo.Setup(r => r.GetById(2)).Returns(job2);

        var mockFileSystem = new Mock<IFileSystemGateway>();
        mockFileSystem.Setup(fs => fs.EnumerateFiles(It.IsAny<string>()))
            .Returns(new List<FileDescriptor>());
        var mockPathAdapter = new Mock<IPathAdapter>();
        mockPathAdapter.Setup(p => p.ToUNC(It.IsAny<string>())).Returns<string>(s => s);
        var mockEventBus = new Mock<IEventBus>();
        var domainService = new BackupDomainService();
        var tracker = new ProgressTracker();

        var executor = new BackupExecutor(
            mockFileSystem.Object, mockPathAdapter.Object,
            mockEventBus.Object, domainService, tracker);
        var strategyFactory = new BackupStrategyFactory();
        var executionService = new BackupExecutionService(mockRepo.Object, executor, strategyFactory);

        var mockConfig = new Mock<ILanguageConfig>();
        mockConfig.Setup(c => c.GetLanguage()).Returns(Language.EN);
        var languageManager = CreateLanguageManager(mockConfig);
        var inputParser = new InputParser();

        var output = new StringWriter();
        var commands = new Dictionary<string, ICommand>
        {
            ["5"] = new ExecuteJobCommand(executionService, output),
            ["7"] = new ExitCommand()
        };

        var input = new StringReader("5\n1-2\n7\n");
        var ui = new ConsoleUI(languageManager, inputParser, commands, input, output);
        ui.Run();

        var text = output.ToString();
        Assert.Contains("Job 1: OK", text);
        Assert.Contains("Job 2: OK", text);
    }

    [Fact]
    public void FullFlow_ExecuteSemicolonList_ShouldParseAndExecute()
    {
        var mockRepo = new Mock<IJobRepository>();
        var job1 = new BackupJob(1, "J1", "/s1", "/d1", BackupType.Full);
        var job3 = new BackupJob(3, "J3", "/s3", "/d3", BackupType.Full);
        mockRepo.Setup(r => r.GetById(1)).Returns(job1);
        mockRepo.Setup(r => r.GetById(3)).Returns(job3);

        var mockFileSystem = new Mock<IFileSystemGateway>();
        mockFileSystem.Setup(fs => fs.EnumerateFiles(It.IsAny<string>()))
            .Returns(new List<FileDescriptor>());
        var mockPathAdapter = new Mock<IPathAdapter>();
        mockPathAdapter.Setup(p => p.ToUNC(It.IsAny<string>())).Returns<string>(s => s);
        var mockEventBus = new Mock<IEventBus>();
        var domainService = new BackupDomainService();
        var tracker = new ProgressTracker();

        var executor = new BackupExecutor(
            mockFileSystem.Object, mockPathAdapter.Object,
            mockEventBus.Object, domainService, tracker);
        var strategyFactory = new BackupStrategyFactory();
        var executionService = new BackupExecutionService(mockRepo.Object, executor, strategyFactory);

        var mockConfig = new Mock<ILanguageConfig>();
        mockConfig.Setup(c => c.GetLanguage()).Returns(Language.EN);
        var languageManager = CreateLanguageManager(mockConfig);
        var inputParser = new InputParser();

        var output = new StringWriter();
        var commands = new Dictionary<string, ICommand>
        {
            ["5"] = new ExecuteJobCommand(executionService, output),
            ["7"] = new ExitCommand()
        };

        var input = new StringReader("5\n1;3\n7\n");
        var ui = new ConsoleUI(languageManager, inputParser, commands, input, output);
        ui.Run();

        var text = output.ToString();
        Assert.Contains("Job 1: OK", text);
        Assert.Contains("Job 3: OK", text);
    }
}
