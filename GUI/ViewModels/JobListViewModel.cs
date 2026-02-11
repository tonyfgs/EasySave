using System.Collections.ObjectModel;
using System.Windows.Input;
using Application.Services;
using GUI.Helpers;
using Model;

namespace GUI.ViewModels;

public class JobListViewModel : ObservableObject
{
    private readonly JobManagementService _jobService;
    private readonly Action<BackupJob> _onEdit;
    private readonly Action<int> _onDelete;
    private BackupJob? _selectedJob;
    private string _statusMessage = string.Empty;

    public ObservableCollection<BackupJob> Jobs { get; } = new();

    public BackupJob? SelectedJob
    {
        get => _selectedJob;
        set
        {
            if (SetProperty(ref _selectedJob, value))
            {
                ((RelayCommand)EditJobCommand).RaiseCanExecuteChanged();
                ((RelayCommand)DeleteJobCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand EditJobCommand { get; }
    public ICommand DeleteJobCommand { get; }
    public ICommand RefreshCommand { get; }

    public JobListViewModel(Action<BackupJob> onEdit, Action<int> onDelete)
    {
        _jobService = ServiceLocator.JobManagementService;
        _onEdit = onEdit;
        _onDelete = onDelete;

        EditJobCommand = new RelayCommand(
            () => { if (SelectedJob is not null) _onEdit(SelectedJob); },
            () => SelectedJob is not null);

        DeleteJobCommand = new RelayCommand(
            () => DeleteSelected(),
            () => SelectedJob is not null);

        RefreshCommand = new RelayCommand(LoadJobs);

        LoadJobs();
    }

    public void LoadJobs()
    {
        Jobs.Clear();
        var jobs = _jobService.ListJobs();
        foreach (var job in jobs)
            Jobs.Add(job);

        StatusMessage = Jobs.Count == 0
            ? "No backup jobs configured."
            : $"{Jobs.Count} job(s) loaded.";
    }

    private void DeleteSelected()
    {
        if (SelectedJob is null) return;

        try
        {
            var id = SelectedJob.Id;
            _jobService.DeleteJob(id);
            StatusMessage = $"Job {id} deleted.";
            SelectedJob = null;
            LoadJobs();
            _onDelete(id);
        }
        catch (DomainException ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }
}