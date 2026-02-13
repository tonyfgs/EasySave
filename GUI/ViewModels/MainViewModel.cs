using System.Windows.Input;
using Application.Services;
using GUI.Helpers;
using GUI.Services;
using Shared;

namespace GUI.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly LanguageApplicationService _languageService;
    private ObservableObject _currentViewModel;
    private Language _currentLanguage;

    // Child ViewModels (created once, reused)
    public JobListViewModel JobListViewModel { get; }
    public CreateJobViewModel CreateJobViewModel { get; }
    public ExecuteJobViewModel ExecuteJobViewModel { get; }
    public SettingsViewModel SettingsViewModel { get; }
    public LocalizationService Localization { get; }

    public ObservableObject CurrentViewModel
    {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }

    public Language CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (SetProperty(ref _currentLanguage, value))
            {
                _languageService.ChangeLanguage(value);
            }
        }
    }

    // Navigation commands
    public ICommand ShowJobListCommand { get; }
    public ICommand ShowCreateJobCommand { get; }
    public ICommand ShowExecuteJobCommand { get; }
    public ICommand ShowSettingsCommand { get; }
    public ICommand ToggleLanguageCommand { get; }

    public MainViewModel()
    {
        _languageService = ServiceLocator.LanguageApplicationService;
        _currentLanguage = _languageService.GetCurrentLanguage();
        Localization = ServiceLocator.LocalizationService;

        // Create child ViewModels, passing navigation callbacks
        JobListViewModel = new JobListViewModel(
            onEdit: job => NavigateToEdit(job),
            onDelete: _ => RefreshJobList());
        CreateJobViewModel = new CreateJobViewModel(
            onJobCreated: () => NavigateToJobList());
        ExecuteJobViewModel = new ExecuteJobViewModel();
        SettingsViewModel = new SettingsViewModel();

        _currentViewModel = JobListViewModel;

        ShowJobListCommand = new RelayCommand(NavigateToJobList);
        ShowCreateJobCommand = new RelayCommand(() => CurrentViewModel = CreateJobViewModel);
        ShowExecuteJobCommand = new RelayCommand(NavigateToExecute);
        ShowSettingsCommand = new RelayCommand(() => CurrentViewModel = SettingsViewModel);
        ToggleLanguageCommand = new RelayCommand(ToggleLanguage);
    }

    private void NavigateToJobList()
    {
        JobListViewModel.LoadJobs();
        CurrentViewModel = JobListViewModel;
    }

    private void NavigateToEdit(Model.BackupJob job)
    {
        CreateJobViewModel.LoadForEdit(job);
        CurrentViewModel = CreateJobViewModel;
    }

    private void NavigateToExecute()
    {
        ExecuteJobViewModel.LoadJobs();
        CurrentViewModel = ExecuteJobViewModel;
    }

    private void RefreshJobList()
    {
        JobListViewModel.LoadJobs();
    }

    private void ToggleLanguage()
    {
        Localization.ChangeLanguage();
        CurrentLanguage = _languageService.GetCurrentLanguage();
    }
}
