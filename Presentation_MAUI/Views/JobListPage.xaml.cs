using Presentation.ViewModels;
using Microsoft.Maui.Controls;

namespace Presentation.Views;

public partial class JobListPage: ContentPage
{

    public JobListPage(JobListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

    }
    
}