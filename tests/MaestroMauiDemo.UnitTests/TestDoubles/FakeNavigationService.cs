using MaestroMauiDemo.Core.Services;

namespace MaestroMauiDemo.UnitTests.TestDoubles;

public sealed class FakeNavigationService : INavigationService
{
    public List<string> RequestedRoutes { get; } = [];

    public int GoBackCalls { get; private set; }

    public Task GoToResult { get; set; } = Task.CompletedTask;

    public Task GoBackResult { get; set; } = Task.CompletedTask;

    public Task GoToAsync(string route)
    {
        RequestedRoutes.Add(route);
        return GoToResult;
    }

    public Task GoBackAsync()
    {
        GoBackCalls++;
        return GoBackResult;
    }
}
