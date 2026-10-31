using MaestroMauiDemo.Models;
using MaestroMauiDemo.ViewModels;

namespace MaestroMauiDemo.Views;

public partial class TaskListPage : ContentPage
{
    public TaskListViewModel ViewModel { get; }

    public TaskListPage(TaskListViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        BindingContext = ViewModel;
    }

    private async void OnPageAppearing(object? sender, EventArgs e) =>
        await ViewModel.LoadCommand.ExecuteAsync(null);

    private async void OnCompletionChanged(object? sender, CheckedChangedEventArgs e)
    {
        // CheckBox has no command property; ignore changes caused by its one-way binding.
        if (sender is CheckBox { BindingContext: TodoItem item } && item.IsCompleted != e.Value)
        {
            await ViewModel.ToggleCompletionCommand.ExecuteAsync(item);
        }
    }
}
