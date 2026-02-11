using System.Collections.ObjectModel;
using System.Windows.Input;
using Application.DTOs;
using Application.Services;
using GUI.Helpers;
using Model;

namespace GUI.ViewModels;


public class ExecuteJobViewModel : ObservableObject
{
    private readonly BackupExecutionService _executionService;
    private readonly JobManagementService _jobManagementService;
    private bool _isExecuting;
    private string _statusMessage = string.Empty;
    private int _overallProgress;

    public ObservableCollection<BackupJob> AvailableJobs { get; } = new();
    public ObservableCollection<BackupJob> SelectedJobs { get; } = new();
    public ObservableCollection<JobExecutionResult> Results { get; } = new();

    public bool IsExecuting
    {
        get => _isExecuting;
        set
        {
            if (SetProperty(ref _isExecuting, value))
            {
                ((RelayCommand)ExecuteSelectedCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ExecuteAllCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public int OverallProgress
    {
        get => _overallProgress;
        set => SetProperty(ref _overallProgress, value);
    }

    public ICommand ExecuteSelectedCommand { get; }
    public ICommand ExecuteAllCommand { get; }
    public ICommand ToggleJobSelectionCommand { get; }

    public ExecuteJobViewModel()
    {
        _executionService = ServiceLocator.BackupExecutionService;
        _jobManagementService = ServiceLocator.JobManagementService;

        ExecuteSelectedCommand = new RelayCommand(
            async () => await ExecuteSelectedAsync(),
            () => !IsExecuting && SelectedJobs.Count > 0);

        ExecuteAllCommand = new RelayCommand(
            async () => await ExecuteAllAsync(),
            () => !IsExecuting && AvailableJobs.Count > 0);

        ToggleJobSelectionCommand = new RelayCommand<BackupJob>(ToggleSelection);
    }

    public void LoadJobs()
    {
        AvailableJobs.Clear();
        SelectedJobs.Clear();
        Results.Clear();
        StatusMessage = string.Empty;
        OverallProgress = 0;

        var jobs = _jobManagementService.ListJobs();
        foreach (var job in jobs)
            AvailableJobs.Add(job);

        if (AvailableJobs.Count == 0)
            StatusMessage = "No jobs available to execute.";
    }

    private void ToggleSelection(BackupJob? job)
    {
        if (job is null) return;

        if (SelectedJobs.Contains(job))
            SelectedJobs.Remove(job);
        else
            SelectedJobs.Add(job);

        ((RelayCommand)ExecuteSelectedCommand).RaiseCanExecuteChanged();
    }

    private async Task ExecuteSelectedAsync()
    {
        var jobIds = SelectedJobs.Select(j => j.Id).ToList();
        await ExecuteAsync(() => _executionService.ExecuteJobs(jobIds));
    }

    private async Task ExecuteAllAsync()
    {
        await ExecuteAsync(() => _executionService.ExecuteAllJobs());
    }

    /// <summary>
    /// Runs the backup on a background thread and updates UI on completion.
    /// Uses MainThread.InvokeOnMainThreadAsync for UI updates.
    /// </summary>
    private async Task ExecuteAsync(Func<List<JobExecutionResult>> executeFunc)
    {
        IsExecuting = true;
        Results.Clear();
        StatusMessage = "Executing backups...";
        OverallProgress = 0;

        try
        {
            var results = await Task.Run(executeFunc);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                foreach (var result in results)
                    Results.Add(result);

                int succeeded = results.Count(r => r.Result.Success);
                int failed = results.Count - succeeded;

                StatusMessage = $"Completed: {succeeded} succeeded, {failed} failed.";
                OverallProgress = 100;
            });
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                StatusMessage = $"Execution error: {ex.Message}";
            });
        }
        finally
        {
            IsExecuting = false;
        }
    }
}