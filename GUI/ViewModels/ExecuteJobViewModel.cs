using System.Collections.ObjectModel;
using System.Windows.Input;
using Application.DTOs;
using Application.Events;
using Application.Ports;
using Application.Services;
using GUI.Helpers;
using GUI.Services;
using Model;

namespace GUI.ViewModels;


public class ExecuteJobViewModel : ObservableObject, IEventHandler<StateChangedEvent>, IEventHandler<BusinessSoftwareDetectedEvent>
{
    private readonly BackupExecutionService _executionService;
    private readonly JobManagementService _jobManagementService;
    private readonly IEventBus _eventBus;
    private bool _isExecuting;
    private string _statusMessage = string.Empty;
    private int _overallProgress;
    private string _currentFile = string.Empty;
    private int _filesProcessed;
    private int _totalFiles;

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

    public string CurrentFile
    {
        get => _currentFile;
        set => SetProperty(ref _currentFile, value);
    }

    public int FilesProcessed
    {
        get => _filesProcessed;
        set => SetProperty(ref _filesProcessed, value);
    }

    public int TotalFiles
    {
        get => _totalFiles;
        set => SetProperty(ref _totalFiles, value);
    }

    public ICommand ExecuteSelectedCommand { get; }
    public ICommand ExecuteAllCommand { get; }
    public ICommand ToggleJobSelectionCommand { get; }

    public ExecuteJobViewModel()
    {
        _executionService = ServiceLocator.BackupExecutionService;
        _jobManagementService = ServiceLocator.JobManagementService;
        _eventBus = ServiceLocator.EventBus;

        // Subscribe to real-time progress events
        _eventBus.Subscribe<StateChangedEvent>(this);
        _eventBus.Subscribe<BusinessSoftwareDetectedEvent>(this);

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
        IsExecuting = true;
        Results.Clear();
        CurrentFile = string.Empty;
        FilesProcessed = 0;
        TotalFiles = 0;
        OverallProgress = 0;
        StatusMessage = "Executing backups...";

        try
        {
            var jobIds = SelectedJobs.Select(j => j.Id).ToList();

            // Run on background thread
            var results = await Task.Run(() => _executionService.ExecuteJobs(jobIds));

            // Update UI on main thread
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                foreach (var r in results)
                {
                    Results.Add(r);
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
                CurrentFile = string.Empty;
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

    private async Task ExecuteAllAsync()
    {
        IsExecuting = true;
        Results.Clear();
        CurrentFile = string.Empty;
        FilesProcessed = 0;
        TotalFiles = 0;
        OverallProgress = 0;
        StatusMessage = "Executing backups...";

        try
        {
            // Run on background thread
            var results = await Task.Run(() => _executionService.ExecuteAllJobs());

            // Update UI on main thread
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                foreach (var r in results)
                {
                    Results.Add(r);
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
                CurrentFile = string.Empty;
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

    // Handle business software detection — show clear blocking message
    public void Handle(BusinessSoftwareDetectedEvent @event)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusMessage = string.Format(
                ServiceLocator.LocalizationService.BusinessSoftwareBlocked,
                @event.JobName);
        });
    }

    // Handle real-time progress updates from StateChangedEvent
    public void Handle(StateChangedEvent @event)
    {
        var snapshot = @event.Snapshot;

        // Update UI on main thread
        MainThread.BeginInvokeOnMainThread(() =>
        {
            OverallProgress = snapshot.Progress;
            FilesProcessed = snapshot.TotalFiles - snapshot.FilesRemaining;
            TotalFiles = snapshot.TotalFiles;

            if (!string.IsNullOrEmpty(snapshot.CurrentSourceFile))
            {
                CurrentFile = Path.GetFileName(snapshot.CurrentSourceFile);
            }
        });
    }
}