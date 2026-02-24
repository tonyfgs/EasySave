using Application.DTOs;
using GUI.Helpers;
using Model;

namespace GUI.ViewModels;

public sealed class JobProgress: ObservableObject
{
    private StateSnapshot _snapshot;

    public string Name => _snapshot.Name;
    public int Progress => _snapshot.Progress;

    public BackupJob Job { get; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }


    public JobProgress(StateSnapshot snapshot, BackupJob job)
    {
        _snapshot = snapshot;
        Job = job;
    }

    public void Update(StateSnapshot snapshot)
    {
        _snapshot = snapshot;
        OnPropertyChanged(nameof(Progress));
    }


}