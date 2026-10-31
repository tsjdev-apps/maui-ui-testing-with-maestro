using MaestroMauiDemo.Core.Models;
using MaestroMauiDemo.Core.Services;
using MaestroMauiDemo.Core.ViewModels;
using MaestroMauiDemo.UnitTests.TestDoubles;

namespace MaestroMauiDemo.UnitTests.ViewModels;

public sealed class TaskListViewModelTests
{
    [Fact]
    public void Constructor_WhenCreated_StartsWithEmptyCollection()
    {
        var viewModel = new TaskListViewModel(new FakeTodoService(), new FakeNavigationService());

        Assert.Empty(viewModel.Items);
    }

    [Fact]
    public async Task LoadCommand_WhenExecuted_ExposesServiceItems()
    {
        var service = new FakeTodoService();
        service.Items.Add(new TodoItem("First task"));
        service.Items.Add(new TodoItem("Second task"));
        var viewModel = new TaskListViewModel(service, new FakeNavigationService());

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(service.Items, viewModel.Items);
        Assert.Equal(1, service.GetItemsCalls);
    }

    [Fact]
    public async Task LoadCommand_WhenExecutedRepeatedly_DoesNotDuplicateItems()
    {
        var service = new FakeTodoService();
        service.Items.Add(new TodoItem("First task"));
        var viewModel = new TaskListViewModel(service, new FakeNavigationService());

        await viewModel.LoadCommand.ExecuteAsync(null);
        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Same(service.Items[0], Assert.Single(viewModel.Items));
    }

    [Fact]
    public async Task LoadCommand_WhenServiceItemsChange_ReplacesVisibleCollection()
    {
        var service = new FakeTodoService();
        service.Items.Add(new TodoItem("Original task"));
        var viewModel = new TaskListViewModel(service, new FakeNavigationService());
        await viewModel.LoadCommand.ExecuteAsync(null);
        service.Items.Clear();
        var replacement = new TodoItem("Replacement task");
        service.Items.Add(replacement);

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Same(replacement, Assert.Single(viewModel.Items));
    }

    [Fact]
    public async Task LoadCommand_WhenServiceBecomesEmpty_ClearsVisibleCollection()
    {
        var service = new FakeTodoService();
        service.Items.Add(new TodoItem("Original task"));
        var viewModel = new TaskListViewModel(service, new FakeNavigationService());
        await viewModel.LoadCommand.ExecuteAsync(null);
        service.Items.Clear();

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Empty(viewModel.Items);
    }

    [Fact]
    public async Task LoadCommand_WhileServiceIsPending_RemainsBusyUntilCompletion()
    {
        var completion = new TaskCompletionSource<IReadOnlyList<TodoItem>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new FakeTodoService { GetItemsResult = completion.Task };
        var viewModel = new TaskListViewModel(service, new FakeNavigationService());

        Task execution = viewModel.LoadCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsBusy);
        Assert.False(viewModel.IsNotBusy);
        Assert.True(viewModel.LoadCommand.IsRunning);
        Assert.False(viewModel.LoadCommand.CanExecute(null));

        completion.SetResult([]);
        await execution;

        Assert.False(viewModel.IsBusy);
        Assert.True(viewModel.IsNotBusy);
        Assert.False(viewModel.LoadCommand.IsRunning);
        Assert.True(viewModel.LoadCommand.CanExecute(null));
    }

    [Fact]
    public async Task LoadCommand_WhenServiceFails_PreservesItemsAndClearsBusyState()
    {
        var service = new FakeTodoService();
        var item = new TodoItem("Existing task");
        service.Items.Add(item);
        var viewModel = new TaskListViewModel(service, new FakeNavigationService());
        await viewModel.LoadCommand.ExecuteAsync(null);
        var error = new InvalidOperationException("Loading failed.");
        service.GetItemsResult = Task.FromException<IReadOnlyList<TodoItem>>(error);

        InvalidOperationException actualError = await Assert.ThrowsAsync<InvalidOperationException>(
            () => viewModel.LoadCommand.ExecuteAsync(null));

        Assert.Same(error, actualError);
        Assert.Same(item, Assert.Single(viewModel.Items));
        Assert.False(viewModel.IsBusy);
        Assert.True(viewModel.LoadCommand.CanExecute(null));
    }

    [Fact]
    public async Task AddTaskCommand_WhenExecuted_RequestsAddTaskRoute()
    {
        var navigation = new FakeNavigationService();
        var viewModel = new TaskListViewModel(new FakeTodoService(), navigation);

        await viewModel.AddTaskCommand.ExecuteAsync(null);

        Assert.Equal(NavigationRoutes.AddTask, Assert.Single(navigation.RequestedRoutes));
    }

    [Fact]
    public async Task AddTaskCommand_WhileNavigationIsPending_ClearsBusyStateOnlyAfterCompletion()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var navigation = new FakeNavigationService { GoToResult = completion.Task };
        var viewModel = new TaskListViewModel(new FakeTodoService(), navigation);

        Task execution = viewModel.AddTaskCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsBusy);
        Assert.False(viewModel.AddTaskCommand.CanExecute(null));

        completion.SetResult();
        await execution;

        Assert.False(viewModel.IsBusy);
        Assert.True(viewModel.AddTaskCommand.CanExecute(null));
    }

    [Fact]
    public async Task AddTaskCommand_WhenNavigationFails_PropagatesErrorAndClearsBusyState()
    {
        var error = new InvalidOperationException("Navigation failed.");
        var navigation = new FakeNavigationService { GoToResult = Task.FromException(error) };
        var viewModel = new TaskListViewModel(new FakeTodoService(), navigation);

        InvalidOperationException actualError = await Assert.ThrowsAsync<InvalidOperationException>(
            () => viewModel.AddTaskCommand.ExecuteAsync(null));

        Assert.Same(error, actualError);
        Assert.False(viewModel.IsBusy);
        Assert.True(viewModel.AddTaskCommand.CanExecute(null));
    }

    [Fact]
    public async Task ToggleCompletionCommand_WithSelectedItem_RequestsOnlyItsId()
    {
        var service = new FakeTodoService();
        var item = new TodoItem("Selected task");
        var viewModel = new TaskListViewModel(service, new FakeNavigationService());

        await viewModel.ToggleCompletionCommand.ExecuteAsync(item);

        Assert.Equal(item.Id, Assert.Single(service.ToggledIds));
    }

    [Fact]
    public async Task ToggleCompletionCommand_WithLoadedItem_UpdatesItsObservableCompletionState()
    {
        var service = new TodoService();
        var viewModel = new TaskListViewModel(service, new FakeNavigationService());
        await viewModel.LoadCommand.ExecuteAsync(null);
        TodoItem item = viewModel.Items[1];

        await viewModel.ToggleCompletionCommand.ExecuteAsync(item);

        Assert.True(item.IsCompleted);
        Assert.All(viewModel.Items.Where(other => other.Id != item.Id), other => Assert.False(other.IsCompleted));
    }

    [Fact]
    public async Task ToggleCompletionCommand_WhileServiceIsPending_ClearsBusyStateOnlyAfterCompletion()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new FakeTodoService { ToggleResult = completion.Task };
        var viewModel = new TaskListViewModel(service, new FakeNavigationService());
        var item = new TodoItem("Selected task");

        Task execution = viewModel.ToggleCompletionCommand.ExecuteAsync(item);

        Assert.True(viewModel.IsBusy);
        Assert.False(viewModel.ToggleCompletionCommand.CanExecute(item));

        completion.SetResult();
        await execution;

        Assert.False(viewModel.IsBusy);
        Assert.True(viewModel.ToggleCompletionCommand.CanExecute(item));
    }

    [Fact]
    public async Task ToggleCompletionCommand_WhenServiceFails_PropagatesErrorAndClearsBusyState()
    {
        var error = new InvalidOperationException("Completion failed.");
        var service = new FakeTodoService { ToggleResult = Task.FromException(error) };
        var viewModel = new TaskListViewModel(service, new FakeNavigationService());
        var item = new TodoItem("Selected task");

        InvalidOperationException actualError = await Assert.ThrowsAsync<InvalidOperationException>(
            () => viewModel.ToggleCompletionCommand.ExecuteAsync(item));

        Assert.Same(error, actualError);
        Assert.False(viewModel.IsBusy);
        Assert.True(viewModel.ToggleCompletionCommand.CanExecute(item));
    }

    [Fact]
    public async Task DeleteCommand_WithSelectedItem_RemovesOnlyThatItem()
    {
        var service = new FakeTodoService();
        var selectedItem = new TodoItem("Selected task");
        var otherItem = new TodoItem("Other task");
        service.Items.AddRange([selectedItem, otherItem]);
        var viewModel = new TaskListViewModel(service, new FakeNavigationService());
        await viewModel.LoadCommand.ExecuteAsync(null);

        await viewModel.DeleteCommand.ExecuteAsync(selectedItem);

        Assert.Equal(selectedItem.Id, Assert.Single(service.DeletedIds));
        Assert.Same(otherItem, Assert.Single(viewModel.Items));
    }

    [Fact]
    public async Task DeleteCommand_WhileServiceIsPending_KeepsItemUntilDeletionCompletes()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new FakeTodoService { DeleteResult = completion.Task };
        var item = new TodoItem("Selected task");
        service.Items.Add(item);
        var viewModel = new TaskListViewModel(service, new FakeNavigationService());
        await viewModel.LoadCommand.ExecuteAsync(null);

        Task execution = viewModel.DeleteCommand.ExecuteAsync(item);

        Assert.Same(item, Assert.Single(viewModel.Items));
        Assert.True(viewModel.IsBusy);
        Assert.False(viewModel.DeleteCommand.CanExecute(item));

        completion.SetResult();
        await execution;

        Assert.Empty(viewModel.Items);
        Assert.False(viewModel.IsBusy);
        Assert.True(viewModel.DeleteCommand.CanExecute(item));
    }

    [Fact]
    public async Task DeleteCommand_WhenServiceFails_PreservesItemAndClearsBusyState()
    {
        var error = new InvalidOperationException("Deletion failed.");
        var service = new FakeTodoService { DeleteResult = Task.FromException(error) };
        var item = new TodoItem("Selected task");
        service.Items.Add(item);
        var viewModel = new TaskListViewModel(service, new FakeNavigationService());
        await viewModel.LoadCommand.ExecuteAsync(null);

        InvalidOperationException actualError = await Assert.ThrowsAsync<InvalidOperationException>(
            () => viewModel.DeleteCommand.ExecuteAsync(item));

        Assert.Same(error, actualError);
        Assert.Same(item, Assert.Single(viewModel.Items));
        Assert.False(viewModel.IsBusy);
        Assert.True(viewModel.DeleteCommand.CanExecute(item));
    }

    [Fact]
    public void IsBusy_WhenChanged_NotifiesAvailability()
    {
        var viewModel = new TaskListViewModel(new FakeTodoService(), new FakeNavigationService());
        var changedProperties = new List<string?>();
        viewModel.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        viewModel.IsBusy = true;

        Assert.False(viewModel.IsNotBusy);
        Assert.Contains(nameof(viewModel.IsBusy), changedProperties);
        Assert.Contains(nameof(viewModel.IsNotBusy), changedProperties);
    }
}
