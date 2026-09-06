using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using AiDesktopClient.Services;
using AiDesktopClient.ViewModels;
using AiDesktopClient.Views;

namespace AiDesktopClient;

public partial class App : Application
{
    public static IServiceProvider? ServiceProvider { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();

        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.DataContext = ServiceProvider.GetRequiredService<MainViewModel>();
        mainWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IToastService, ToastService>();
        services.AddSingleton<IChatService, MockChatService>();
        services.AddSingleton<IModelService, MockModelService>();
        services.AddSingleton<IUsageService, MockUsageService>();
        services.AddSingleton<IBackendService, MockBackendService>();

        services.AddTransient<MainViewModel>();
        services.AddTransient<ChatViewModel>();
        services.AddTransient<SettingsViewModel>();

        services.AddTransient<MainWindow>();
    }
}
