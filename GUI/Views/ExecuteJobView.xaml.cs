using Model;

namespace GUI.Views;

public partial class ExecuteJobView : ContentView
{
    public ExecuteJobView()
    {
        InitializeComponent();
    }
    
    private void OnJobCheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.BindingContext is BackupJob job)
        {
            var vm = BindingContext as GUI.ViewModels.ExecuteJobViewModel;
            vm?.ToggleJobSelectionCommand.Execute(job);
        }
    }
}