namespace MaestroMauiDemo;

public partial class App : Application
{
    private readonly IServiceProvider serviceProvider;

    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        this.serviceProvider = serviceProvider;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Pages need the application resources before their XAML is initialized.
        return new Window(serviceProvider.GetRequiredService<AppShell>());
    }
}
