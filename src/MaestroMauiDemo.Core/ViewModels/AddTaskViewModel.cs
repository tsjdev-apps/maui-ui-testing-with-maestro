using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaestroMauiDemo.Core.Services;

namespace MaestroMauiDemo.Core.ViewModels;

public sealed partial class AddTaskViewModel(ITodoService todoService, INavigationService navigationService) : ViewModelBase
{
    [ObservableProperty]
    public partial string TaskTitle { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValidationMessage))]
    public partial string ValidationMessage { get; set; } = string.Empty;

    public bool HasValidationMessage => !string.IsNullOrEmpty(ValidationMessage);

    partial void OnTaskTitleChanged(string value) => ValidationMessage = string.Empty;

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(TaskTitle))
        {
            ValidationMessage = "Enter a task title.";
            return;
        }

        IsBusy = true;
        try
        {
            await todoService.AddAsync(TaskTitle.Trim());
            await navigationService.GoBackAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        IsBusy = true;
        try
        {
            await navigationService.GoBackAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }
}
