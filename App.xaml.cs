using System.Net.Http;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using AiDesktopClient.Services;
using AiDesktopClient.ViewModels;
using AiDesktopClient.Views;

namespace AiDesktopClient;

public partial class App : Application
{
    public static IServiceProvider? ServiceProvider { get; private set; }
    public static ThemeManager? ThemeManager { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();

        ThemeManager = ServiceProvider.GetRequiredService<ThemeManager>();
        ThemeManager.ApplyInitialTheme();

        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.DataContext = ServiceProvider.GetRequiredService<MainViewModel>();
        mainWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ThemeManager>();
        services.AddSingleton<IToastService, ToastService>();
        services.AddSingleton(_ =>
        {
            var handler = new HttpClientHandler { UseProxy = false };
            return new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost:5000/"),
                Timeout = Timeout.InfiniteTimeSpan
            };
        });
        services.AddSingleton<IChatService, SseChatService>();
        services.AddSingleton<IModelService, MockModelService>();
        services.AddSingleton<IUsageService, MockUsageService>();
        services.AddSingleton<IBackendService, BackendHealthService>();

        services.AddTransient<MainViewModel>();
        services.AddTransient<ChatViewModel>();
        services.AddSingleton<SettingsViewModel>();

        services.AddTransient<MainWindow>();
    }
}
