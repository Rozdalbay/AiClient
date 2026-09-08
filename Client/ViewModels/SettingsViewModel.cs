using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AiDesktopClient.Services;

namespace AiDesktopClient.ViewModels;

// VM настроек: темы, бэкенд-URL, тумблеры; аккаунт тут тоже локальный, потому что нормального бэкенд-профиля всё ещё нет
public partial class SettingsViewModel : ObservableObject
{
    private readonly IBackendService _backendService;
    private readonly IToastService _toastService;
    private readonly ThemeManager _themeManager;
    private readonly AuthService _authService;

    [ObservableProperty]
    private string _selectedSection = "General";

    [ObservableProperty]
    private string _backendUrl = "localhost";

    [ObservableProperty]
    private bool _isDarkTheme = true;

    [ObservableProperty]
    private bool _enableNotifications = true;

    [ObservableProperty]
    private bool _enableStreaming = true;

    [ObservableProperty]
    private bool _showTokenCount = true;

    [ObservableProperty]
    private bool _showCostInfo = true;

    [ObservableProperty]
    private bool _isTestingConnection;

    [ObservableProperty]
    private string _connectionTestResult = string.Empty;

    [ObservableProperty]
    private string _connectionTestStatus = string.Empty;

    [ObservableProperty]
    private bool _compactMode;

    [ObservableProperty]
    private int _fontSize = 13;

    [ObservableProperty]
    private string _currentUsername = string.Empty;

    public string AccountStatus => "Local account";

    public SettingsViewModel(IBackendService backendService, IToastService toastService, ThemeManager themeManager, AuthService authService)
    {
        _backendService = backendService;
        _toastService = toastService;
        _themeManager = themeManager;
        _authService = authService;
        _isDarkTheme = themeManager.CurrentTheme == AppTheme.Dark;
        _backendUrl = themeManager.LoadBackendUrl() ?? "localhost";

        var session = authService.LoadSession();
        _currentUsername = session?.Username ?? string.Empty;
    }

    // тема переключилась - уведомляем ThemeManager, он сам сделает wipe-анимацию; без него тема сменится разве что после перезапуска
    partial void OnIsDarkThemeChanged(bool value)
    {
        _themeManager.SwitchTheme(value ? AppTheme.Dark : AppTheme.Light);
    }

    [RelayCommand]
    private void ToggleDarkTheme()
    {
        IsDarkTheme = !IsDarkTheme;
    }

    // тестовая проверка связи с будущим бэкендом: метаем GET и показываем "Success/Failed", морока с таймаутом - внутри BackendHealthService
    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        IsTestingConnection = true;
        ConnectionTestResult = string.Empty;

        try
        {
            var result = await _backendService.TestConnectionAsync(BackendUrl);
            ConnectionTestStatus = result ? "Success" : "Failed";
            ConnectionTestResult = result
                ? "Connection successful"
                : "Connection failed. Check the URL and try again.";

            if (result)
                _themeManager.SaveBackendUrl(BackendUrl);
        }
        catch (Exception ex)
        {
            ConnectionTestStatus = "Error";
            ConnectionTestResult = $"Error: {ex.Message}";
        }
        finally
        {
            IsTestingConnection = false;
        }
    }

    [RelayCommand]
    private void SaveSettings()
    {
        _toastService.ShowSuccess("Settings saved");
    }

    // сброс в дефолт: по сути рандомное гадание на кофейной гуще, реального хранилища настроек всё ещё нет
    [RelayCommand]
    private void ResetSettings()
    {
        BackendUrl = "https://api.example.com";
        IsDarkTheme = true;
        EnableNotifications = true;
        EnableStreaming = true;
        ShowTokenCount = true;
        ShowCostInfo = true;
        CompactMode = false;
        FontSize = 13;
        _toastService.ShowInfo("Settings reset to defaults");
    }

    [RelayCommand]
    private void Logout()
    {
        App.Logout();
    }
}
