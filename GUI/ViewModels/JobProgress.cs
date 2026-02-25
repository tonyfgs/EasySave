using System.Windows.Input;
using Application.DTOs;
using Application.Events;
using GUI.Helpers;
using Model;

namespace GUI.ViewModels;

public sealed class JobProgress : ObservableObject
{
    private StateSnapshot _snapshot;

    public string Name => _snapshot.Name;
    public int Progress => _snapshot.Progress;
    public JobState State => _snapshot.State;
    public bool IsPaused => _snapshot.State == JobState.Paused;
    public string PauseResumeLabel => IsPaused ? "Resume" : "Pause";

    public BackupJob Job { get; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public ICommand PauseCommand { get; }
    public ICommand StopCommand { get; }

    public JobProgress(StateSnapshot snapshot, BackupJob job, IEventBus eventBus)
    {
        _snapshot = snapshot;
        Job = job;
        PauseCommand = new RelayCommand(() =>
        {
            if (IsPaused)
                eventBus.Publish(new ResumeRequestedEvent(job.Id));
            else
                eventBus.Publish(new PauseRequestedEvent(job.Id));
        });
        StopCommand = new RelayCommand(() => eventBus.Publish(new StopRequestedEvent(job.Id)));
    }

    public void Update(StateSnapshot snapshot)
    {
        _snapshot = snapshot;
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(State));
        OnPropertyChanged(nameof(IsPaused));
        OnPropertyChanged(nameof(PauseResumeLabel));
    }

    public void Reset()
    {
        _snapshot = new StateSnapshot { Name = _snapshot.Name, State = JobState.Inactive };
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(State));
        OnPropertyChanged(nameof(IsPaused));
        OnPropertyChanged(nameof(PauseResumeLabel));
    }
}
