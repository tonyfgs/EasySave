using System.Collections.ObjectModel;
using System.Windows.Input;
using Application.Services;
using Model;
using ToolKit;

namespace Presentation.ViewModels;

public class JobListViewModel: ViewModelBase
{
    private readonly JobManagementService _jobManagementService;
    private BackupExecutionService _backupExecutionService;
    private BackupJob? _selectedJob;
    private ObservableCollection<BackupJob> _jobs;

    public ObservableCollection<BackupJob> Jobs
    {
        get => _jobs;
        set => SetField(ref _jobs, value);
    }
    
    public BackupJob? SelectedJob
    {
        get => _selectedJob;
        set
        {
            if (SetField(ref _selectedJob, value))
            {
                OnPropertyChanged(nameof(HasSelection));
            }
        }
    }

    public bool HasSelection => SelectedJob != null;
    public ICommand LoadJobsCommand;
    public ICommand DeleteJobsCommand;
    public ICommand NavigateToCreateCommand;
    public ICommand NavigateToEditCommand;
    
    public JobListViewModel(JobManagementService jobManagementService,
        BackupExecutionService backupExecutionService)
    {
        _jobManagementService = jobManagementService;
        _backupExecutionService = backupExecutionService;
        Jobs = new ObservableCollection<BackupJob>();
        LoadJobsCommand = new RelayCommand(param => LoadJobs());
        DeleteJobsCommand = new RelayCommand(param => DeleteJob());
        
        NavigateToCreateCommand = new RelayCommand(async _ => 
            await Shell.Current.GoToAsync("JobFormPage"));

        NavigateToEditCommand = new RelayCommand(async _ => 
        {
            if (SelectedJob != null)
                await Shell.Current.GoToAsync($"JobFormPage?JobId={SelectedJob.Id}");
        });
    }


    public void LoadJobs()
    {
        List<BackupJob> backupJobs = _jobManagementService.ListJobs();
        Jobs = new ObservableCollection<BackupJob>(backupJobs);
    }

    public void DeleteJob()
    {
        if (SelectedJob != null)
        {
            _jobManagementService.DeleteJob(SelectedJob.Id);
            Jobs.Remove(SelectedJob);
            SelectedJob = null;
        }
    }

}