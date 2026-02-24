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
        if (sender is CheckBox checkBox && checkBox.BindingContext is GUI.ViewModels.JobProgress entry)
        {
            entry.IsSelected = e.Value;
            var vm = BindingContext as GUI.ViewModels.ExecuteJobViewModel;
            vm?.ToggleJobSelectionCommand.Execute(entry.Job);
        }
    }
}