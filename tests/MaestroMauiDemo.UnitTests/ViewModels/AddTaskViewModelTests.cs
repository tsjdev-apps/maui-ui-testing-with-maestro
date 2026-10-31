using MaestroMauiDemo.Core.Models;
using MaestroMauiDemo.Core.ViewModels;
using MaestroMauiDemo.UnitTests.TestDoubles;

namespace MaestroMauiDemo.UnitTests.ViewModels;

public sealed class AddTaskViewModelTests
{
    [Fact]
    public void Constructor_WhenCreated_StartsWithEmptyTitle()
    {
        var viewModel = new AddTaskViewModel(new FakeTodoService(), new FakeNavigationService());

        Assert.Empty(viewModel.TaskTitle);
    }

    [Fact]
    public void Constructor_WhenCreated_HasNoValidationMessage()
    {
        var viewModel = new AddTaskViewModel(new FakeTodoService(), new FakeNavigationService());

        Assert.Empty(viewModel.ValidationMessage);
        Assert.False(viewModel.HasValidationMessage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public async Task SaveCommand_WithInvalidTitle_DoesNotSaveOrNavigate(string? title)
    {
        var service = new FakeTodoService();
        var navigation = new FakeNavigationService();
        var viewModel = new AddTaskViewModel(service, navigation) { TaskTitle = title! };

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Empty(service.AddedTitles);
        Assert.Equal(0, navigation.GoBackCalls);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task SaveCommand_WithEmptyTitle_ShowsValidationMessage()
    {
        var viewModel = new AddTaskViewModel(new FakeTodoService(), new FakeNavigationService());

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Enter a task title.", viewModel.ValidationMessage);
        Assert.True(viewModel.HasValidationMessage);
    }

    [Fact]
    public async Task TaskTitle_WhenValidInputIsEntered_ClearsValidationMessage()
    {
        var viewModel = new AddTaskViewModel(new FakeTodoService(), new FakeNavigationService());
        await viewModel.SaveCommand.ExecuteAsync(null);

        viewModel.TaskTitle = "Valid task";

        Assert.Empty(viewModel.ValidationMessage);
        Assert.False(viewModel.HasValidationMessage);
    }

    [Theory]
    [InlineData("New task", "New task")]
    [InlineData("  New task  ", "New task")]
    [InlineData("\tNew task\r\n", "New task")]
    [InlineData("  Keep  inner spaces  ", "Keep  inner spaces")]
    public async Task SaveCommand_WithValidTitle_PassesTrimmedTitleToService(string title, string expectedTitle)
    {
        var service = new FakeTodoService();
        var viewModel = new AddTaskViewModel(service, new FakeNavigationService()) { TaskTitle = title };

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal(expectedTitle, Assert.Single(service.AddedTitles));
    }

    [Fact]
    public async Task SaveCommand_AfterSuccessfulSave_RequestsNavigationBack()
    {
        var navigation = new FakeNavigationService();
        var viewModel = new AddTaskViewModel(new FakeTodoService(), navigation) { TaskTitle = "New task" };

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal(1, navigation.GoBackCalls);
        Assert.Empty(navigation.RequestedRoutes);
    }

    [Fact]
    public async Task SaveCommand_AfterCorrectingInvalidInput_SavesOnlyValidTitle()
    {
        var service = new FakeTodoService();
        var viewModel = new AddTaskViewModel(service, new FakeNavigationService());
        await viewModel.SaveCommand.ExecuteAsync(null);
        viewModel.TaskTitle = "Valid task";

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Valid task", Assert.Single(service.AddedTitles));
        Assert.False(viewModel.HasValidationMessage);
    }

    [Fact]
    public async Task SaveCommand_WhileServiceIsPending_DoesNotNavigateBeforeSaveCompletes()
    {
        var completion = new TaskCompletionSource<TodoItem>(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new FakeTodoService { AddResult = completion.Task };
        var navigation = new FakeNavigationService();
        var viewModel = new AddTaskViewModel(service, navigation) { TaskTitle = "New task" };

        Task execution = viewModel.SaveCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsBusy);
        Assert.False(viewModel.IsNotBusy);
        Assert.True(viewModel.SaveCommand.IsRunning);
        Assert.False(viewModel.SaveCommand.CanExecute(null));
        Assert.Equal(0, navigation.GoBackCalls);

        completion.SetResult(new TodoItem("New task"));
        await execution;

        Assert.False(viewModel.IsBusy);
        Assert.True(viewModel.IsNotBusy);
        Assert.False(viewModel.SaveCommand.IsRunning);
        Assert.True(viewModel.SaveCommand.CanExecute(null));
        Assert.Equal(1, navigation.GoBackCalls);
    }

    [Fact]
    public async Task SaveCommand_WhileNavigationIsPending_RemainsBusyUntilReturnCompletes()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var navigation = new FakeNavigationService { GoBackResult = completion.Task };
        var viewModel = new AddTaskViewModel(new FakeTodoService(), navigation) { TaskTitle = "New task" };

        Task execution = viewModel.SaveCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsBusy);
        Assert.False(viewModel.SaveCommand.CanExecute(null));

        completion.SetResult();
        await execution;

        Assert.False(viewModel.IsBusy);
        Assert.True(viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task SaveCommand_WhenServiceFails_PropagatesErrorWithoutNavigating()
    {
        var error = new InvalidOperationException("Saving failed.");
        var service = new FakeTodoService { AddResult = Task.FromException<TodoItem>(error) };
        var navigation = new FakeNavigationService();
        var viewModel = new AddTaskViewModel(service, navigation) { TaskTitle = "New task" };

        InvalidOperationException actualError = await Assert.ThrowsAsync<InvalidOperationException>(
            () => viewModel.SaveCommand.ExecuteAsync(null));

        Assert.Same(error, actualError);
        Assert.Equal(0, navigation.GoBackCalls);
        Assert.False(viewModel.IsBusy);
        Assert.True(viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task SaveCommand_WhenNavigationFails_PropagatesErrorAndClearsBusyState()
    {
        var error = new InvalidOperationException("Navigation failed.");
        var navigation = new FakeNavigationService { GoBackResult = Task.FromException(error) };
        var viewModel = new AddTaskViewModel(new FakeTodoService(), navigation) { TaskTitle = "New task" };

        InvalidOperationException actualError = await Assert.ThrowsAsync<InvalidOperationException>(
            () => viewModel.SaveCommand.ExecuteAsync(null));

        Assert.Same(error, actualError);
        Assert.False(viewModel.IsBusy);
        Assert.True(viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task CancelCommand_WhenExecuted_RequestsNavigationBack()
    {
        var navigation = new FakeNavigationService();
        var viewModel = new AddTaskViewModel(new FakeTodoService(), navigation);

        await viewModel.CancelCommand.ExecuteAsync(null);

        Assert.Equal(1, navigation.GoBackCalls);
    }

    [Fact]
    public async Task CancelCommand_WithEnteredTitle_DoesNotCreateTask()
    {
        var service = new FakeTodoService();
        var viewModel = new AddTaskViewModel(service, new FakeNavigationService()) { TaskTitle = "Unsaved task" };

        await viewModel.CancelCommand.ExecuteAsync(null);

        Assert.Empty(service.AddedTitles);
    }

    [Fact]
    public async Task CancelCommand_WhileNavigationIsPending_ClearsBusyStateOnlyAfterCompletion()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var navigation = new FakeNavigationService { GoBackResult = completion.Task };
        var viewModel = new AddTaskViewModel(new FakeTodoService(), navigation);

        Task execution = viewModel.CancelCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsBusy);
        Assert.False(viewModel.CancelCommand.CanExecute(null));

        completion.SetResult();
        await execution;

        Assert.False(viewModel.IsBusy);
        Assert.True(viewModel.CancelCommand.CanExecute(null));
    }

    [Fact]
    public async Task CancelCommand_WhenNavigationFails_PropagatesErrorAndClearsBusyState()
    {
        var error = new InvalidOperationException("Navigation failed.");
        var navigation = new FakeNavigationService { GoBackResult = Task.FromException(error) };
        var viewModel = new AddTaskViewModel(new FakeTodoService(), navigation);

        InvalidOperationException actualError = await Assert.ThrowsAsync<InvalidOperationException>(
            () => viewModel.CancelCommand.ExecuteAsync(null));

        Assert.Same(error, actualError);
        Assert.False(viewModel.IsBusy);
        Assert.True(viewModel.CancelCommand.CanExecute(null));
    }
}
