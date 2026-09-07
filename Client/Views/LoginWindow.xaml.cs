using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using AiDesktopClient.ViewModels;

namespace AiDesktopClient.Views;

public partial class LoginWindow : Window
{
    private LoginViewModel ViewModel => (LoginViewModel)DataContext;

    public LoginWindow()
    {
        InitializeComponent();
        PasswordBox.PasswordChanged += PasswordBox_PasswordChanged;
        ConfirmPasswordBox.PasswordChanged += ConfirmPasswordBox_PasswordChanged;
        UsernameBox.KeyDown += Enter_KeyDown;
        PasswordBox.KeyDown += Enter_KeyDown;
        ConfirmPasswordBox.KeyDown += Enter_KeyDown;
    }

    private void Enter_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && ViewModel.SubmitCommand.CanExecute(null))
            ViewModel.SubmitCommand.Execute(null);
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
            vm.Password = PasswordBox.Password;
    }

    private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
            vm.ConfirmPassword = ConfirmPasswordBox.Password;
    }

    public void ApplyViewModel(LoginViewModel viewModel)
    {
        DataContext = viewModel;
        viewModel.PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(LoginViewModel.ShowPassword):
                    UpdatePasswordVisibility(viewModel.ShowPassword);
                    break;
                case nameof(LoginViewModel.IsRegisterMode):
                    UpdateMode(viewModel.IsRegisterMode);
                    break;
                case nameof(LoginViewModel.ErrorMessage):
                    UpdateError(viewModel.ErrorMessage);
                    break;
            }
        };

        UpdatePasswordVisibility(viewModel.ShowPassword);
        UpdateMode(viewModel.IsRegisterMode);
    }

    private void UpdatePasswordVisibility(bool showPassword)
    {
        if (showPassword)
        {
            PasswordBoxVisible.Text = ViewModel.Password;
            PasswordBoxVisible.Visibility = Visibility.Visible;
            PasswordBox.Visibility = Visibility.Collapsed;
            PasswordToggleIcon.Text = "\uE7B4";
        }
        else
        {
            PasswordBox.Password = ViewModel.Password;
            PasswordBox.Visibility = Visibility.Visible;
            PasswordBoxVisible.Visibility = Visibility.Collapsed;
            PasswordToggleIcon.Text = "\uE7B3";
        }
    }

    private void UpdateMode(bool isRegister)
    {
        if (isRegister)
        {
            SubtitleText.Text = "Create your account";
            SubmitButtonText.Text = "Register";
            SwitchModeLink.Text = "Sign in instead";
            ModeLabel.Text = "Already have an account?";
            ConfirmPasswordGrid.Visibility = Visibility.Visible;
            BackButton.Visibility = Visibility.Visible;
        }
        else
        {
            SubtitleText.Text = "Welcome back";
            SubmitButtonText.Text = "Login";
            SwitchModeLink.Text = "Create account";
            ModeLabel.Text = "Don't have an account?";
            ConfirmPasswordGrid.Visibility = Visibility.Collapsed;
            BackButton.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdateError(string error)
    {
        if (string.IsNullOrEmpty(error))
        {
            ErrorText.Visibility = Visibility.Collapsed;
        }
        else
        {
            ErrorText.Text = error;
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
            return;
        DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CloseCommand.Execute(null);
    }
}
