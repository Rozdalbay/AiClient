using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AiDesktopClient.Services;

namespace AiDesktopClient.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly AuthService _authService;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private bool _rememberMe = true;

    [ObservableProperty]
    private bool _isRegisterMode;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _showPassword;

    [ObservableProperty]
    private bool _isProcessing;

    public event EventHandler? LoginSucceeded;
    public event EventHandler? CloseRequested;

    public LoginViewModel(AuthService authService)
    {
        _authService = authService;
        _rememberMe = authService.LoadSession()?.RememberMe ?? true;
    }

    partial void OnIsRegisterModeChanged(bool value)
    {
        ErrorMessage = string.Empty;
        Password = string.Empty;
        ConfirmPassword = string.Empty;
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        ShowPassword = !ShowPassword;
    }

    [RelayCommand]
    private void SwitchMode()
    {
        IsRegisterMode = !IsRegisterMode;
    }

    [RelayCommand]
    private void Close()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Submit()
    {
        ErrorMessage = string.Empty;

        if (IsRegisterMode)
            Register();
        else
            Login();
    }

    private void Login()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "Please enter your username";
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter your password";
            return;
        }

        IsProcessing = true;

        if (_authService.Login(Username, Password, out var error))
        {
            _authService.SaveSession(Username, RememberMe);
            Password = string.Empty;
            LoginSucceeded?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            ErrorMessage = error;
        }

        IsProcessing = false;
    }

    private void Register()
    {
        if (string.IsNullOrWhiteSpace(Username) || Username.Length < 3)
        {
            ErrorMessage = "Username must be at least 3 characters";
            return;
        }

        if (string.IsNullOrWhiteSpace(Password) || Password.Length < 6)
        {
            ErrorMessage = "Password must be at least 6 characters";
            return;
        }

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match";
            return;
        }

        IsProcessing = true;

        if (_authService.Register(Username, Password, out var error))
        {
            _authService.SaveSession(Username, RememberMe);
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            LoginSucceeded?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            ErrorMessage = error;
        }

        IsProcessing = false;
    }
}
