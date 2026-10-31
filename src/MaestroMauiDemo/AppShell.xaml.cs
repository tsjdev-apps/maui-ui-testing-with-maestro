using MaestroMauiDemo.Views;

namespace MaestroMauiDemo;

public partial class AppShell : Shell
{
    public const string AddTaskRoute = nameof(AddTaskPage);

    public AppShell(TaskListPage taskListPage)
    {
        InitializeComponent();
        TaskListContent.Content = taskListPage;
        Routing.RegisterRoute(AddTaskRoute, typeof(AddTaskPage));
    }
}
