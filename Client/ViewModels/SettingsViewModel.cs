using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AiDesktopClient.Services;

namespace AiDesktopClient.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IBackendService _backendService;
    private readonly IToastService _toastService;

    [ObservableProperty]
    private string _selectedSection = "General";

    [ObservableProperty]
    private string _backendUrl = "localhost"; // пока что для тестов localhost

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

    public SettingsViewModel(IBackendService backendService, IToastService toastService)
    {
        _backendService = backendService;
        _toastService = toastService;
    }

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
}
