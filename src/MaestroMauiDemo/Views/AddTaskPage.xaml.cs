using MaestroMauiDemo.ViewModels;

namespace MaestroMauiDemo.Views;

public partial class AddTaskPage : ContentPage
{
    public AddTaskPage(AddTaskViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
