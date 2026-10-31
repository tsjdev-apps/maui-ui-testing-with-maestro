using CommunityToolkit.Mvvm.ComponentModel;

namespace MaestroMauiDemo.Models;

public sealed class TodoItem(string title) : ObservableObject
{
    private bool isCompleted;

    public Guid Id { get; } = Guid.NewGuid();

    public string Title { get; } = title;

    public bool IsCompleted
    {
        get => isCompleted;
        internal set => SetProperty(ref isCompleted, value);
    }
}
