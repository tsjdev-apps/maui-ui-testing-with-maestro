using MaestroMauiDemo.Core.Models;

namespace MaestroMauiDemo.Core.Services;

public sealed class TodoService : ITodoService
{
    private readonly List<TodoItem> items =
    [
        new("Learn .NET MAUI"),
        new("Explore Maestro"),
        new("Write UI tests")
    ];

    public Task<IReadOnlyList<TodoItem>> GetItemsAsync() =>
        Task.FromResult<IReadOnlyList<TodoItem>>(items.ToArray());

    public Task<TodoItem> AddAsync(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        var item = new TodoItem(title.Trim());
        items.Add(item);
        return Task.FromResult(item);
    }

    public Task ToggleCompletionAsync(Guid id)
    {
        TodoItem item = FindItem(id);
        item.IsCompleted = !item.IsCompleted;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        items.Remove(FindItem(id));
        return Task.CompletedTask;
    }

    private TodoItem FindItem(Guid id) =>
        items.Find(item => item.Id == id)
        ?? throw new KeyNotFoundException($"The task with ID '{id}' does not exist.");
}
