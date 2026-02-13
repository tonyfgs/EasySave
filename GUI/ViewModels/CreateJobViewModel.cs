using System.Windows.Input;
using Application.Services;
using GUI.Helpers;
using Model;

namespace GUI.ViewModels;

/// <summary>
/// ViewModel for create/edit job form.
/// 
/// Design choice: A single ViewModel handles both Create and Edit modes.
/// The IsEditMode property controls which operation is performed on save.
/// This avoids duplicating form logic across two separate ViewModels.
/// 
/// Validation is delegated to the domain model (BackupJob.Validate()),
/// keeping the ViewModel thin and the domain rules in a single place.
/// </summary>
public class CreateJobViewModel : ObservableObject
{
    private readonly JobManagementService _jobService;
    private readonly Action _onJobCreated;

    private int? _editingJobId;
    private string _name = string.Empty;
    private string _sourcePath = string.Empty;
    private string _targetPath = string.Empty;
    private BackupType _selectedType = BackupType.Full;
    private string _statusMessage = string.Empty;
    private bool _isEditMode;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string SourcePath
    {
        get => _sourcePath;
        set => SetProperty(ref _sourcePath, value);
    }

    public string TargetPath
    {
        get => _targetPath;
        set => SetProperty(ref _targetPath, value);
    }

    public BackupType SelectedType
    {
        get => _selectedType;
        set => SetProperty(ref _selectedType, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    public string FormTitle => IsEditMode
        ? ServiceLocator.LocalizationService.EditJobTitle
        : ServiceLocator.LocalizationService.CreateJobTitle;

    // Expose BackupType values for the Picker
    public List<BackupType> BackupTypes { get; } = new()
    {
        BackupType.Full,
        BackupType.Differential
    };

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand BrowseSourceCommand { get; }
    public ICommand BrowseTargetCommand { get; }

    public CreateJobViewModel(Action onJobCreated)
    {
        _jobService = ServiceLocator.JobManagementService;
        _onJobCreated = onJobCreated;

        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(Cancel);
        BrowseSourceCommand = new RelayCommand(BrowseSource);
        BrowseTargetCommand = new RelayCommand(BrowseTarget);
    }

    /// <summary>
    /// Populate form fields for editing an existing job.
    /// </summary>
    public void LoadForEdit(BackupJob job)
    {
        _editingJobId = job.Id;
        Name = job.Name;
        SourcePath = job.SourcePath;
        TargetPath = job.TargetPath;
        SelectedType = job.Type;
        IsEditMode = true;
        StatusMessage = string.Empty;
        OnPropertyChanged(nameof(FormTitle));
    }

    /// <summary>
    /// Reset form to creation mode.
    /// </summary>
    public void ResetForm()
    {
        _editingJobId = null;
        Name = string.Empty;
        SourcePath = string.Empty;
        TargetPath = string.Empty;
        SelectedType = BackupType.Full;
        IsEditMode = false;
        StatusMessage = string.Empty;
        OnPropertyChanged(nameof(FormTitle));
    }

    private void Save()
    {
        try
        {
            if (IsEditMode && _editingJobId.HasValue)
            {
                _jobService.ModifyJob(_editingJobId.Value, Name, SourcePath, TargetPath, SelectedType);
                StatusMessage = $"Job '{Name}' updated successfully.";
            }
            else
            {
                var job = _jobService.CreateJob(Name, SourcePath, TargetPath, SelectedType);
                StatusMessage = $"Job '{job.Name}' created with ID {job.Id}.";
            }

            _onJobCreated();
            ResetForm();
        }
        catch (DomainException ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Unexpected error: {ex.Message}";
        }
    }

    private void Cancel()
    {
        ResetForm();
        _onJobCreated();
    }

    private async void BrowseSource()
    {
        var result = await FolderPicker();
        if (result is not null)
            SourcePath = result;
    }

    private async void BrowseTarget()
    {
        var result = await FolderPicker();
        if (result is not null)
            TargetPath = result;
    }

    /// <summary>
    /// Uses MAUI's FolderPicker API. Falls back gracefully if unavailable.
    /// </summary>
    private static async Task<string?> FolderPicker()
    {
        await Task.CompletedTask;
        try
        {
            // var result = await CommunityToolkit.Maui.Storage.FolderPicker.Default.PickAsync();
            // if (result.IsSuccessful)
            //     return result.Folder.Path;
            Console.WriteLine("YO");
        }
        catch
        {
            // FolderPicker may not be available on all platforms.
            // User can type the path manually.
        }
        return null;
    }
}