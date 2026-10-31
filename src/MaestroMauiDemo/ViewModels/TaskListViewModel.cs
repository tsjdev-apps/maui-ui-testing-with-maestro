using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using MaestroMauiDemo.Models;
using MaestroMauiDemo.Services;

namespace MaestroMauiDemo.ViewModels;

public sealed partial class TaskListViewModel(ITodoService todoService) : ViewModelBase
{
    public ObservableCollection<TodoItem> Items { get; } = [];

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            IReadOnlyList<TodoItem> items = await todoService.GetItemsAsync();
            Items.Clear();
            foreach (TodoItem item in items)
            {
                Items.Add(item);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddTaskAsync()
    {
        IsBusy = true;
        try
        {
            await Shell.Current.GoToAsync(AppShell.AddTaskRoute);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ToggleCompletionAsync(TodoItem item)
    {
        IsBusy = true;
        try
        {
            await todoService.ToggleCompletionAsync(item.Id);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(TodoItem item)
    {
        IsBusy = true;
        try
        {
            await todoService.DeleteAsync(item.Id);
            Items.Remove(item);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
