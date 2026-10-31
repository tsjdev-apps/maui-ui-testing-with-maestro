using MaestroMauiDemo.Services;
using MaestroMauiDemo.ViewModels;
using MaestroMauiDemo.Views;

namespace MaestroMauiDemo;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<ITodoService, TodoService>();
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<TaskListViewModel>();
        builder.Services.AddSingleton<TaskListPage>();
        builder.Services.AddTransient<AddTaskViewModel>();
        builder.Services.AddTransient<AddTaskPage>();

        return builder.Build();
    }
}
