using System.ComponentModel;
using GUI.ViewModels;
using GUI.Views;

namespace GUI;

/// <summary>
/// Main page code-behind that maps ViewModels to their corresponding Views.
/// 
/// Design choice: We use programmatic view switching in code-behind rather than
/// DataTemplateSelector because MAUI's DataTemplateSelector for ContentView
/// has known issues with proper BindingContext propagation.
/// 
/// The code-behind only handles view creation/switching — no business logic.
/// This is an acceptable MVVM practice when the framework requires it.
/// </summary>
public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    // Cache views to avoid recreating them on every navigation
    private readonly JobListView _jobListView = new();
    private readonly CreateJobView _createJobView = new();
    private readonly ExecuteJobView _executeJobView = new();

    private ContentView? _contentArea;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        // Find the content area (second child of the root Grid)
        _contentArea = ((Grid)Content).Children[1] as ContentView;

        // Listen for CurrentViewModel changes to swap the displayed view
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        // Set initial view
        UpdateContent();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.CurrentViewModel))
        {
            UpdateContent();
        }
    }

    /// <summary>
    /// Maps the active ViewModel to its View and sets the BindingContext.
    /// This is the "View Resolver" — the single place where ViewModel↔View
    /// coupling is defined.
    /// </summary>
    private void UpdateContent()
    {
        if (_contentArea is null) return;

        var currentVm = _viewModel.CurrentViewModel;

        ContentView view = currentVm switch
        {
            JobListViewModel => _jobListView,
            CreateJobViewModel => _createJobView,
            ExecuteJobViewModel => _executeJobView,
            _ => _jobListView
        };

        view.BindingContext = currentVm;
        _contentArea.Content = view;
    }
}
