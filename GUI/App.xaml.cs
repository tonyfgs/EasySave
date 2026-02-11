using GUI.Helpers;
using GUI.ViewModels;

namespace GUI;

public partial class App : Microsoft.Maui.Controls.Application
{
    public App()
    {
        InitializeComponent();

        // Initialize services before creating any ViewModels
        ServiceLocator.Initialize();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var viewModel = new MainViewModel();
        var mainPage = new MainPage(viewModel);

        return new Window(mainPage)
        {
            Title = "EasySave - Backup Manager",
            Width = 1000,
            Height = 650
        };
    }
}