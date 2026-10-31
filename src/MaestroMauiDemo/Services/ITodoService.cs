using MaestroMauiDemo.Models;

namespace MaestroMauiDemo.Services;

public interface ITodoService
{
    Task<IReadOnlyList<TodoItem>> GetItemsAsync();

    Task<TodoItem> AddAsync(string title);

    Task ToggleCompletionAsync(Guid id);

    Task DeleteAsync(Guid id);
}
