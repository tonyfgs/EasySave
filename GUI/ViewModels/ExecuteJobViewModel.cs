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
            ExecuteSelected,
            () => !IsExecuting && SelectedJobs.Count > 0);

        ExecuteAllCommand = new RelayCommand(
            ExecuteAll,
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

    private void ExecuteSelected()
    {
        IsExecuting = true;
        Results.Clear();
        StatusMessage = "Executing backups...";

        try
        {
            var jobIds = SelectedJobs.Select(j => j.Id).ToList();
            var results = _executionService.ExecuteJobs(jobIds);

            foreach (var r in results)
            {
                Results.Add(r);

                if (!r.Result.Success)
                {
                    var errors = string.Join(", ", r.Result.Errors);
                    System.Diagnostics.Debug.WriteLine($"Job {r.JobId} FAILED: {errors}");
                }
            }

            int succeeded = results.Count(r => r.Result.Success);
            int failed = results.Count - succeeded;

            if (failed > 0)
            {
                var failedJobs = results.Where(r => !r.Result.Success).ToList();
                var errorDetails = string.Join("; ", failedJobs.Select(j =>
                    $"Job {j.JobId}: {string.Join(", ", j.Result.Errors)}"));
                StatusMessage = $"Completed: {succeeded} succeeded, {failed} failed. Errors: {errorDetails}";
            }
            else
            {
                StatusMessage = $"Completed: {succeeded} succeeded, {failed} failed.";
            }

            OverallProgress = 100;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Execution error: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Exception: {ex}");
        }
        finally
        {
            IsExecuting = false;
        }
    }

    private void ExecuteAll()
    {
        IsExecuting = true;
        Results.Clear();
        StatusMessage = "Executing backups...";

        try
        {
            // EXACTLY like console
            var results = _executionService.ExecuteAllJobs();

            foreach (var r in results)
            {
                Results.Add(r);

                // Log detailed error info if failed
                if (!r.Result.Success)
                {
                    var errors = string.Join(", ", r.Result.Errors);
                    System.Diagnostics.Debug.WriteLine($"Job {r.JobId} FAILED: {errors}");
                }
            }

            int succeeded = results.Count(r => r.Result.Success);
            int failed = results.Count - succeeded;

            // Show detailed error message if any failed
            if (failed > 0)
            {
                var failedJobs = results.Where(r => !r.Result.Success).ToList();
                var errorDetails = string.Join("; ", failedJobs.Select(j =>
                    $"Job {j.JobId}: {string.Join(", ", j.Result.Errors)}"));
                StatusMessage = $"Completed: {succeeded} succeeded, {failed} failed. Errors: {errorDetails}";
            }
            else
            {
                StatusMessage = $"Completed: {succeeded} succeeded, {failed} failed.";
            }

            OverallProgress = 100;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Execution error: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Exception: {ex}");
        }
        finally
        {
            IsExecuting = false;
        }
    }
}