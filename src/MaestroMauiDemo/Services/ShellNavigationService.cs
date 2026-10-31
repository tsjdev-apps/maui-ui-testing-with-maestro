using MaestroMauiDemo.Core.Services;

namespace MaestroMauiDemo.Services;

public sealed class ShellNavigationService : INavigationService
{
    public Task GoToAsync(string route) => Shell.Current.GoToAsync(route);

    public Task GoBackAsync() => Shell.Current.GoToAsync("..");
}
