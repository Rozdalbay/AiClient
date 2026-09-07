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
    private SplashWindow? _splash;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();

        ThemeManager = ServiceProvider.GetRequiredService<ThemeManager>();
        ThemeManager.ApplyInitialTheme();

        ShowSplashWindow();
    }

    private void ShowSplashWindow()
    {
        _splash = new SplashWindow();
        _splash.Show();
        _ = RunStartupSequenceAsync();
    }

    private async Task RunStartupSequenceAsync()
    {
        if (ServiceProvider is null || _splash is null)
            return;

        try
        {
            await RunStartupCore();
        }
        catch (Exception ex)
        {
            try
            {
                var diag = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_error.log");
                System.IO.File.WriteAllText(diag, ex.ToString());
            }
            catch { }
            _splash.SetStatus("Startup error");
            _splash.UpdateProgress(1.0);
            _splash.AnimateClose(() => { _splash = null; Shutdown(); });
        }
    }

    private async Task RunStartupCore()
    {
        if (ServiceProvider is null || _splash is null)
            return;

        var authService = ServiceProvider.GetRequiredService<AuthService>();

        _splash.SetStatus("Loading configuration...");
        _splash.UpdateProgress(0.15);

        _splash.SetStatus("Restoring session...");
        _splash.UpdateProgress(0.35);
        var session = authService.LoadSession();

        _splash.SetStatus("Loading models...");
        _splash.UpdateProgress(0.55);
        var modelService = ServiceProvider.GetRequiredService<IModelService>();
        await modelService.GetModelsAsync();

        _splash.SetStatus("Preparing workspace...");
        _splash.UpdateProgress(0.85);

        bool rememberMe = session is { IsAuthenticated: true, RememberMe: true };
        if (!rememberMe && session is { IsAuthenticated: true })
            authService.ClearSession();

        PrepareTargetWindow(rememberMe);

        _splash.SetStatus("Ready");
        _splash.UpdateProgress(1.0);
        _splash.AnimateClose(() => _splash = null);
    }

    private void PrepareTargetWindow(bool showMain)
    {
        if (showMain)
            ShowMainWindow();
        else
            ShowLoginWindow();
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
