using MaestroMauiDemo.Core.Models;
using MaestroMauiDemo.Core.Services;

namespace MaestroMauiDemo.UnitTests.TestDoubles;

public sealed class FakeTodoService : ITodoService
{
    public List<TodoItem> Items { get; } = [];

    public List<string> AddedTitles { get; } = [];

    public List<Guid> ToggledIds { get; } = [];

    public List<Guid> DeletedIds { get; } = [];

    public int GetItemsCalls { get; private set; }

    public Task<IReadOnlyList<TodoItem>>? GetItemsResult { get; set; }

    public Task<TodoItem>? AddResult { get; set; }

    public Task ToggleResult { get; set; } = Task.CompletedTask;

    public Task DeleteResult { get; set; } = Task.CompletedTask;

    public Task<IReadOnlyList<TodoItem>> GetItemsAsync()
    {
        GetItemsCalls++;
        return GetItemsResult ?? Task.FromResult<IReadOnlyList<TodoItem>>(Items.ToArray());
    }

    public Task<TodoItem> AddAsync(string title)
    {
        AddedTitles.Add(title);
        return AddResult ?? Task.FromResult(new TodoItem(title));
    }

    public Task ToggleCompletionAsync(Guid id)
    {
        ToggledIds.Add(id);
        return ToggleResult;
    }

    public Task DeleteAsync(Guid id)
    {
        DeletedIds.Add(id);
        return DeleteResult;
    }
}
