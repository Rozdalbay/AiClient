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

    private MainWindow? _mainWindow;
    private LoginWindow? _loginWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();

        ThemeManager = ServiceProvider.GetRequiredService<ThemeManager>();
        ThemeManager.ApplyInitialTheme();

        var authService = ServiceProvider.GetRequiredService<AuthService>();
        var session = authService.LoadSession();

        if (session is { IsAuthenticated: true })
        {
            if (!session.RememberMe)
            {
                authService.ClearSession();
                ShowLoginWindow();
            }
            else
            {
                ShowMainWindow();
            }
        }
        else
        {
            ShowLoginWindow();
        }
    }

    private void ShowLoginWindow()
    {
        var authService = ServiceProvider!.GetRequiredService<AuthService>();
        var viewModel = new LoginViewModel(authService);

        _loginWindow = new LoginWindow();
        _loginWindow.ApplyViewModel(viewModel);

        viewModel.LoginSucceeded += (_, _) =>
        {
            ShowMainWindow();
            _loginWindow?.Close();
            _loginWindow = null;
        };

        viewModel.CloseRequested += (_, _) =>
        {
            _loginWindow?.Close();
            _loginWindow = null;
            Shutdown();
        };

        _loginWindow.Closed += (_, _) =>
        {
            if (_loginWindow is not null)
            {
                _loginWindow = null;
                if (_mainWindow is null)
                    Shutdown();
            }
        };

        _loginWindow.Show();
    }

    private void ShowMainWindow()
    {
        _mainWindow = ServiceProvider!.GetRequiredService<MainWindow>();
        _mainWindow.DataContext = ServiceProvider.GetRequiredService<MainViewModel>();

        var currentMain = _mainWindow;
        currentMain.Closed += (_, _) =>
        {
            if (ReferenceEquals(_mainWindow, currentMain))
            {
                _mainWindow = null;
                if (_loginWindow is null)
                    Shutdown();
            }
        };

        _mainWindow.Show();
    }

    public static void Logout()
    {
        var instance = (App)Current;
        var authService = ServiceProvider!.GetRequiredService<AuthService>();
        authService.ClearSession();

        var mainWindow = instance._mainWindow;
        instance._mainWindow = null;

        instance.ShowLoginWindow();

        mainWindow?.Close();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ThemeManager>();
        services.AddSingleton<IToastService, ToastService>();
        services.AddSingleton<AuthService>();
        services.AddSingleton(_ =>
        {
            var handler = new HttpClientHandler { UseProxy = false };
            return new HttpClient(handler)
            {
                BaseAddress = new Uri("http://127.0.0.1:5000/"),
                Timeout = Timeout.InfiniteTimeSpan
            };
        });
        services.AddSingleton<IChatService, SseChatService>();
        services.AddSingleton<IModelService, MockModelService>();
        services.AddSingleton<IUsageService, LocalUsageService>();
        services.AddSingleton<IBackendService, BackendHealthService>();

        services.AddTransient<MainViewModel>();
        services.AddTransient<ChatViewModel>();
        services.AddSingleton<SettingsViewModel>();

        services.AddTransient<MainWindow>();
    }
}
