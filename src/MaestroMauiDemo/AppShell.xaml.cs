using MaestroMauiDemo.Core.Services;
using MaestroMauiDemo.Views;

namespace MaestroMauiDemo;

public partial class AppShell : Shell
{
    public AppShell(TaskListPage taskListPage)
    {
        InitializeComponent();
        TaskListContent.Content = taskListPage;
        Routing.RegisterRoute(NavigationRoutes.AddTask, typeof(AddTaskPage));
    }
}
