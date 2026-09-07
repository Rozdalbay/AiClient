using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using AiDesktopClient.Models;
using AiDesktopClient.Services;
using AiDesktopClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AiDesktopClient.Views;

public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var toastService = App.ServiceProvider!.GetRequiredService<IToastService>();
        toastService.ToastRequested += OnToastRequested;
    }

    private void OnToastRequested(object? sender, ToastEventArgs e)
    {
        ToastControl.Show(e.Message, e.Type, e.DurationMs);
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximize();
        }
        else
        {
            DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleMaximize();
    }

    private void ToggleMaximize()
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            MaximizeIcon.Text = "\uE739";
        }
        else
        {
            WindowState = WindowState.Maximized;
            MaximizeIcon.Text = "\uE923";
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ToggleSidebarCommand.Execute(null);
    }

    private void ToggleUsagePanel_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ToggleUsagePanelCommand.Execute(null);
    }

    private void ChatListItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is Chat chat)
        {
            ViewModel.SelectChatCommand.Execute(chat);
        }
    }

    private void RenameChat_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem)
        {
            var chat = GetChatFromMenuItem(menuItem);
            if (chat is not null)
            {
                var result = ShowInputDialog("Rename Chat", chat.Title);
                if (result is not null)
                {
                    chat.Title = result;
                    App.ServiceProvider!.GetRequiredService<IToastService>().ShowSuccess("Chat renamed");
                }
            }
        }
    }

    private void PinChat_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem)
        {
            var chat = GetChatFromMenuItem(menuItem);
            if (chat is not null)
                ViewModel.TogglePinCommand.Execute(chat);
        }
    }

    private void FavoriteChat_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem)
        {
            var chat = GetChatFromMenuItem(menuItem);
            if (chat is not null)
                ViewModel.ToggleFavoriteCommand.Execute(chat);
        }
    }

    private void DeleteChat_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem)
        {
            var chat = GetChatFromMenuItem(menuItem);
            if (chat is not null)
                ViewModel.DeleteChatCommand.Execute(chat);
        }
    }

    private static Chat? GetChatFromMenuItem(MenuItem menuItem)
    {
        if (menuItem.Parent is ContextMenu contextMenu)
        {
            if (contextMenu.PlacementTarget is Border border)
                return border.Tag as Chat;
        }
        return null;
    }

    private static string? ShowInputDialog(string title, string defaultValue)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 350,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = (Application.Current.FindResource("PrimaryBackgroundBrush") as SolidColorBrush)?.Clone() ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#131620")),
            BorderBrush = (Application.Current.FindResource("BorderBrush") as SolidColorBrush)?.Clone() ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A2D3E")),
            BorderThickness = new Thickness(1),
            ResizeMode = ResizeMode.NoResize,
            WindowStyle = WindowStyle.ToolWindow
        };

        var stack = new StackPanel { Margin = new Thickness(16) };

        var textBox = new TextBox
        {
            Text = defaultValue,
            Margin = new Thickness(0, 0, 0, 12),
            Background = (Application.Current.FindResource("InputBackgroundBrush") as SolidColorBrush)?.Clone() ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1D2E")),
            Foreground = (Application.Current.FindResource("PrimaryTextBrush") as SolidColorBrush)?.Clone() ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8E8F0")),
            BorderBrush = (Application.Current.FindResource("BorderBrush") as SolidColorBrush)?.Clone() ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A2D3E")),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8, 6, 8, 6)
        };

        var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

        var okButton = new Button
        {
            Content = "OK",
            Width = 70,
            Margin = new Thickness(0, 0, 8, 0),
            Background = (Application.Current.FindResource("PrimaryAccentBrush") as SolidColorBrush)?.Clone() ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7C5CFC")),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0)
        };
        okButton.Click += (s, e) => dialog.DialogResult = true;

        var cancelButton = new Button
        {
            Content = "Cancel",
            Width = 70,
            Background = (Application.Current.FindResource("TertiaryBackgroundBrush") as SolidColorBrush)?.Clone() ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1D2E")),
            Foreground = (Application.Current.FindResource("SecondaryTextBrush") as SolidColorBrush)?.Clone() ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B8D9E")),
            BorderThickness = new Thickness(0)
        };
        cancelButton.Click += (s, e) => dialog.DialogResult = false;

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);

        stack.Children.Add(textBox);
        stack.Children.Add(buttonPanel);
        dialog.Content = stack;

        textBox.Focus();
        textBox.SelectAll();

        return dialog.ShowDialog() == true ? textBox.Text : null;
    }
}
