using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Input;
using Application.DTOs;
using Application.Events;
using Application.Ports;
using Application.Services;
using GUI.Helpers;
using GUI.Services;
using Model;

namespace GUI.ViewModels;


public class ExecuteJobViewModel : ObservableObject,
    IEventHandler<StateChangedEvent>,
    IEventHandler<BusinessSoftwareDetectedEvent>,
    IEventHandler<StopRequestedEvent>
{
    private readonly BackupExecutionService _executionService;
    private readonly JobManagementService _jobManagementService;
    private readonly IEventBus _eventBus;

    // Per-job tracking: which job IDs are currently executing
    private readonly HashSet<int> _runningJobIds = new();
    private readonly HashSet<int> _stoppedJobIds = new();

    private int _pendingUiUpdate;
    private bool _isExecuting;
    private string _statusMessage = string.Empty;
    private int _overallProgress;
    private string _currentFile = string.Empty;
    private int _filesProcessed;
    private int _totalFiles;

    public ObservableCollection<JobProgress> AvailableJobs { get; } = new();
    public ObservableCollection<BackupJob> SelectedJobs { get; } = new();
    public ObservableCollection<JobExecutionResult> Results { get; } = new();

    public bool IsExecuting
    {
        get => _isExecuting;
        private set => SetProperty(ref _isExecuting, value);
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
    public ICommand SelectJobCommand { get; }

    public ExecuteJobViewModel()
    {
        _executionService = ServiceLocator.BackupExecutionService;
        _jobManagementService = ServiceLocator.JobManagementService;
        _eventBus = ServiceLocator.EventBus;

        _eventBus.Subscribe<StateChangedEvent>(this);
        _eventBus.Subscribe<BusinessSoftwareDetectedEvent>(this);
        _eventBus.Subscribe<StopRequestedEvent>(this);

        // Allow launching more jobs even if some are already running
        ExecuteSelectedCommand = new RelayCommand(
            async () => await ExecuteSelectedAsync(),
            () => SelectedJobs.Any(j => !_runningJobIds.Contains(j.Id)));

        ExecuteAllCommand = new RelayCommand(
            async () => await ExecuteAllAsync(),
            () => AvailableJobs.Any(jp => !_runningJobIds.Contains(jp.Job.Id)));

        SelectJobCommand = new RelayCommand<JobProgress>(SelectJob);
    }

    public void LoadJobs()
    {
        AvailableJobs.Clear();
        SelectedJobs.Clear();
        Results.Clear();
        _runningJobIds.Clear();
        StatusMessage = string.Empty;
        OverallProgress = 0;

        var jobs = _jobManagementService.ListJobs();
        foreach (var job in jobs)
        {
            var currentProgress = new JobProgress(new StateSnapshot { Name = job.Name }, job, _eventBus);
            AvailableJobs.Add(currentProgress);
        }

        if (AvailableJobs.Count == 0)
            StatusMessage = "No jobs available to execute.";
    }

    // Toggle selection — running jobs cannot be re-selected until they finish
    private void SelectJob(JobProgress? jobProgress)
    {
        if (jobProgress is null) return;
        if (_runningJobIds.Contains(jobProgress.Job.Id)) return;

        jobProgress.IsSelected = !jobProgress.IsSelected;

        if (jobProgress.IsSelected)
            SelectedJobs.Add(jobProgress.Job);
        else
            SelectedJobs.Remove(jobProgress.Job);

        RefreshCommandStates();
    }

    private async Task ExecuteSelectedAsync()
    {
        // Only run jobs that are not already executing
        var jobsToRun = SelectedJobs
            .Where(j => !_runningJobIds.Contains(j.Id))
            .ToList();

        if (jobsToRun.Count == 0) return;

        var jobIds = jobsToRun.Select(j => j.Id).ToList();

        foreach (var j in jobsToRun)
            _runningJobIds.Add(j.Id);

        IsExecuting = true;
        Results.Clear();
        CurrentFile = string.Empty;
        StatusMessage = "Executing backups...";
        RefreshCommandStates();

        try
        {
            var results = await _executionService.ExecuteJobsAsync(jobIds);

            int succeeded = results.Count(r => r.Result.Success);
            int failed = results.Count - succeeded;

            foreach (var r in results)
                Results.Add(r);

            if (failed > 0)
            {
                var errors = string.Join("; ", results
                    .Where(r => !r.Result.Success)
                    .Select(j => $"Job {j.JobId}: {string.Join(", ", j.Result.Errors)}"));
                StatusMessage = $"Completed: {succeeded} succeeded, {failed} failed. Errors: {errors}";
            }
            else
            {
                StatusMessage = $"Completed: {succeeded} succeeded.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Execution error: {ex.Message}";
        }
        finally
        {
            foreach (var j in jobsToRun)
            {
                _runningJobIds.Remove(j.Id);
                var jp = AvailableJobs.FirstOrDefault(p => p.Job.Id == j.Id);
                if (jp != null) jp.IsSelected = false;
                SelectedJobs.Remove(j);
            }

            IsExecuting = _runningJobIds.Count > 0;

            if (!IsExecuting)
            {
                OverallProgress = 0;
                CurrentFile = string.Empty;
            }

            RefreshCommandStates();
        }
    }

    private async Task ExecuteAllAsync()
    {
        // Only run jobs not already executing
        var jobsToRun = AvailableJobs
            .Where(jp => !_runningJobIds.Contains(jp.Job.Id))
            .ToList();

        if (jobsToRun.Count == 0) return;

        foreach (var jp in jobsToRun)
        {
            jp.IsSelected = true;
            if (!SelectedJobs.Contains(jp.Job))
                SelectedJobs.Add(jp.Job);
            _runningJobIds.Add(jp.Job.Id);
        }

        IsExecuting = true;
        Results.Clear();
        CurrentFile = string.Empty;
        StatusMessage = "Executing backups...";
        RefreshCommandStates();

        try
        {
            var jobIds = jobsToRun.Select(jp => jp.Job.Id).ToList();
            var results = await _executionService.ExecuteJobsAsync(jobIds);

            int succeeded = results.Count(r => r.Result.Success);
            int failed = results.Count - succeeded;

            foreach (var r in results)
                Results.Add(r);

            if (failed > 0)
            {
                var errors = string.Join("; ", results
                    .Where(r => !r.Result.Success)
                    .Select(j => $"Job {j.JobId}: {string.Join(", ", j.Result.Errors)}"));
                StatusMessage = $"Completed: {succeeded} succeeded, {failed} failed. Errors: {errors}";
            }
            else
            {
                StatusMessage = $"Completed: {succeeded} succeeded.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Execution error: {ex.Message}";
        }
        finally
        {
            foreach (var jp in jobsToRun)
            {
                _runningJobIds.Remove(jp.Job.Id);
                jp.IsSelected = false;
                SelectedJobs.Remove(jp.Job);
            }

            IsExecuting = _runningJobIds.Count > 0;

            if (!IsExecuting)
            {
                OverallProgress = 0;
                CurrentFile = string.Empty;
            }

            RefreshCommandStates();
        }
    }

    // Unselect the stopped job immediately — reset happens when End event arrives
    public void Handle(StopRequestedEvent @event)
    {
        _stoppedJobIds.Add(@event.JobId);

        var jobProgress = AvailableJobs.FirstOrDefault(jp => jp.Job.Id == @event.JobId);
        if (jobProgress is null) return;

        jobProgress.IsSelected = false;
        SelectedJobs.Remove(jobProgress.Job);
        RefreshCommandStates();
    }

    public void Handle(BusinessSoftwareDetectedEvent @event)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusMessage = string.Format(
                ServiceLocator.LocalizationService.BusinessSoftwareBlocked,
                @event.JobName);
        });
    }

    public void Handle(StateChangedEvent @event)
    {
        var snapshot = @event.Snapshot;
        bool isProgressUpdate = snapshot.State == JobState.Active;

        if (isProgressUpdate && Interlocked.CompareExchange(ref _pendingUiUpdate, 1, 0) != 0)
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (isProgressUpdate) Interlocked.Exchange(ref _pendingUiUpdate, 0);

            JobProgress? jobToUpdate = AvailableJobs.FirstOrDefault(j => j.Name == snapshot.Name);
            if (jobToUpdate != null)
            {
                if (snapshot.State == JobState.End && _stoppedJobIds.Remove(jobToUpdate.Job.Id))
                    jobToUpdate.Reset();
                else
                    jobToUpdate.Update(snapshot);
            }

            // Aggregate progress across all running jobs
            var runningJobs = AvailableJobs.Where(j => j.IsSelected).ToList();
            var total = runningJobs.Sum(j => j.TotalFiles);
            var processed = runningJobs.Sum(j => j.TotalFiles - j.FilesRemaining);
            TotalFiles = total;
            FilesProcessed = processed;
            OverallProgress = total > 0 ? (int)((double)processed / total * 100) : 0;

            if (!string.IsNullOrEmpty(snapshot.CurrentSourceFile))
                CurrentFile = Path.GetFileName(snapshot.CurrentSourceFile);
        });
    }

    private void RefreshCommandStates()
    {
        ((RelayCommand)ExecuteSelectedCommand).RaiseCanExecuteChanged();
        ((RelayCommand)ExecuteAllCommand).RaiseCanExecuteChanged();
    }
}
