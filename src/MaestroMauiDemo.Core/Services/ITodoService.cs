using MaestroMauiDemo.Core.Models;

namespace MaestroMauiDemo.Core.Services;

public interface ITodoService
{
    Task<IReadOnlyList<TodoItem>> GetItemsAsync();

    Task<TodoItem> AddAsync(string title);

    Task ToggleCompletionAsync(Guid id);

    Task DeleteAsync(Guid id);
}
