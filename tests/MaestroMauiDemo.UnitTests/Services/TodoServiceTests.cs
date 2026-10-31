using MaestroMauiDemo.Core.Models;
using MaestroMauiDemo.Core.Services;

namespace MaestroMauiDemo.UnitTests.Services;

public sealed class TodoServiceTests
{
    [Fact]
    public async Task GetItemsAsync_WhenServiceIsCreated_ReturnsSeededTasks()
    {
        var service = new TodoService();

        IReadOnlyList<TodoItem> items = await service.GetItemsAsync();

        Assert.Equal(["Learn .NET MAUI", "Explore Maestro", "Write UI tests"], items.Select(item => item.Title));
    }

    [Fact]
    public async Task GetItemsAsync_WhenServiceIsCreated_ReturnsIncompleteTasks()
    {
        var service = new TodoService();

        IReadOnlyList<TodoItem> items = await service.GetItemsAsync();

        Assert.All(items, item => Assert.False(item.IsCompleted));
    }

    [Fact]
    public async Task GetItemsAsync_WhenTasksAreAdded_DoesNotChangePreviousSnapshot()
    {
        var service = new TodoService();
        IReadOnlyList<TodoItem> snapshot = await service.GetItemsAsync();

        await service.AddAsync("New task");

        Assert.Equal(3, snapshot.Count);
    }

    [Fact]
    public async Task AddAsync_WithValidTitle_AddsExactlyOneTask()
    {
        var service = new TodoService();
        IReadOnlyList<TodoItem> originalItems = await service.GetItemsAsync();

        TodoItem addedItem = await service.AddAsync("New task");
        IReadOnlyList<TodoItem> items = await service.GetItemsAsync();

        Assert.Same(addedItem, Assert.Single(items.Except(originalItems)));
    }

    [Theory]
    [InlineData("New task", "New task")]
    [InlineData("  New task  ", "New task")]
    [InlineData("\tNew task\r\n", "New task")]
    [InlineData("  Keep  inner spaces  ", "Keep  inner spaces")]
    public async Task AddAsync_WithValidTitle_TrimsOnlySurroundingWhitespace(string title, string expectedTitle)
    {
        var service = new TodoService();

        TodoItem item = await service.AddAsync(title);

        Assert.Equal(expectedTitle, item.Title);
    }

    [Fact]
    public async Task AddAsync_WithValidTitle_CreatesIncompleteTask()
    {
        var service = new TodoService();

        TodoItem item = await service.AddAsync("New task");

        Assert.False(item.IsCompleted);
    }

    [Fact]
    public async Task AddAsync_WithValidTitle_AppendsToExistingTasks()
    {
        var service = new TodoService();

        TodoItem item = await service.AddAsync("New task");
        IReadOnlyList<TodoItem> items = await service.GetItemsAsync();

        Assert.Same(item, items[^1]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public async Task AddAsync_WithBlankTitle_ThrowsArgumentException(string title)
    {
        var service = new TodoService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.AddAsync(title));
    }

    [Fact]
    public async Task AddAsync_WithNullTitle_ThrowsArgumentNullException()
    {
        var service = new TodoService();

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.AddAsync(null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public async Task AddAsync_WithInvalidTitle_DoesNotChangeExistingTasks(string? title)
    {
        var service = new TodoService();
        IReadOnlyList<TodoItem> originalItems = await service.GetItemsAsync();

        await Assert.ThrowsAnyAsync<ArgumentException>(() => service.AddAsync(title!));

        Assert.Equal(originalItems, await service.GetItemsAsync());
    }

    [Fact]
    public async Task AddAsync_WhenCreatingMultipleTasks_AssignsUniqueNonEmptyIds()
    {
        var service = new TodoService();
        await service.AddAsync("First new task");
        await service.AddAsync("Second new task");

        IReadOnlyList<TodoItem> items = await service.GetItemsAsync();

        Assert.All(items, item => Assert.NotEqual(Guid.Empty, item.Id));
        Assert.Equal(items.Count, items.Select(item => item.Id).Distinct().Count());
    }

    [Fact]
    public async Task ToggleCompletionAsync_WithExistingId_CompletesOnlySelectedTask()
    {
        var service = new TodoService();
        IReadOnlyList<TodoItem> items = await service.GetItemsAsync();
        TodoItem selectedItem = items[1];

        await service.ToggleCompletionAsync(selectedItem.Id);

        Assert.True(selectedItem.IsCompleted);
        Assert.All(items.Where(item => item.Id != selectedItem.Id), item => Assert.False(item.IsCompleted));
    }

    [Fact]
    public async Task ToggleCompletionAsync_WhenCalledTwice_RestoresIncompleteState()
    {
        var service = new TodoService();
        TodoItem item = (await service.GetItemsAsync())[0];

        await service.ToggleCompletionAsync(item.Id);
        await service.ToggleCompletionAsync(item.Id);

        Assert.False(item.IsCompleted);
    }

    [Fact]
    public async Task ToggleCompletionAsync_WithExistingId_NotifiesCompletionChange()
    {
        var service = new TodoService();
        TodoItem item = (await service.GetItemsAsync())[0];
        var changedProperties = new List<string?>();
        item.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        await service.ToggleCompletionAsync(item.Id);

        Assert.Equal(nameof(TodoItem.IsCompleted), Assert.Single(changedProperties));
    }

    [Fact]
    public async Task ToggleCompletionAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var service = new TodoService();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ToggleCompletionAsync(Guid.Empty));
    }

    [Fact]
    public async Task ToggleCompletionAsync_WithUnknownId_DoesNotChangeTasks()
    {
        var service = new TodoService();
        IReadOnlyList<TodoItem> originalItems = await service.GetItemsAsync();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ToggleCompletionAsync(Guid.Empty));

        Assert.Equal(originalItems, await service.GetItemsAsync());
        Assert.All(originalItems, item => Assert.False(item.IsCompleted));
    }

    [Fact]
    public async Task DeleteAsync_WithExistingId_RemovesOnlySelectedTask()
    {
        var service = new TodoService();
        IReadOnlyList<TodoItem> originalItems = await service.GetItemsAsync();
        TodoItem selectedItem = originalItems[1];

        await service.DeleteAsync(selectedItem.Id);

        Assert.Equal(originalItems.Where(item => item.Id != selectedItem.Id), await service.GetItemsAsync());
    }

    [Fact]
    public async Task DeleteAsync_WhenAllTasksAreDeleted_ReturnsEmptyCollection()
    {
        var service = new TodoService();
        IReadOnlyList<TodoItem> items = await service.GetItemsAsync();

        foreach (TodoItem item in items)
        {
            await service.DeleteAsync(item.Id);
        }

        Assert.Empty(await service.GetItemsAsync());
    }

    [Fact]
    public async Task DeleteAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var service = new TodoService();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteAsync(Guid.Empty));
    }

    [Fact]
    public async Task DeleteAsync_WithUnknownId_DoesNotChangeTasks()
    {
        var service = new TodoService();
        IReadOnlyList<TodoItem> originalItems = await service.GetItemsAsync();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteAsync(Guid.Empty));

        Assert.Equal(originalItems, await service.GetItemsAsync());
    }

    [Fact]
    public async Task GetItemsAsync_WithSeparateServices_KeepsTaskStateIsolated()
    {
        var firstService = new TodoService();
        var secondService = new TodoService();

        await firstService.AddAsync("Only in the first service");

        Assert.Equal(3, (await secondService.GetItemsAsync()).Count);
    }
}
