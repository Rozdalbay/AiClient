using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AiDesktopClient.Models;
using AiDesktopClient.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AiDesktopClient.Controls;

public partial class ChatMessageControl : UserControl
{
    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(ChatMessage), typeof(ChatMessageControl),
            new PropertyMetadata(null, OnMessageChanged));

    private ObservableCollection<Attachment>? _subscribedAttachments;

    public ChatMessageControl()
    {
        InitializeComponent();
    }

    public ChatMessage? Message
    {
        get => (ChatMessage?)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    private static void OnMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ChatMessageControl control)
        {
            if (e.OldValue is INotifyPropertyChanged oldMsg)
                PropertyChangedEventManager.RemoveHandler(oldMsg, control.OnMessagePropertyChanged, string.Empty);

            if (e.NewValue is ChatMessage message)
            {
                PropertyChangedEventManager.AddHandler(message, control.OnMessagePropertyChanged, string.Empty);
            }
            control.RefreshMessage();
        }
    }

    private void OnMessagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!ReferenceEquals(sender, Message)) return;
        if (Dispatcher.CheckAccess())
            RefreshMessage();
        else
            Dispatcher.InvokeAsync(RefreshMessage);
    }

    private void OnAttachmentsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (Dispatcher.CheckAccess())
            RefreshMessage();
        else
            Dispatcher.InvokeAsync(RefreshMessage);
    }

    private void RefreshMessage()
    {
        var message = Message;
        if (!ReferenceEquals(_subscribedAttachments, message?.Attachments))
        {
            if (_subscribedAttachments is not null)
                CollectionChangedEventManager.RemoveHandler(_subscribedAttachments, OnAttachmentsChanged);
            _subscribedAttachments = message?.Attachments;
            if (_subscribedAttachments is not null)
                CollectionChangedEventManager.AddHandler(_subscribedAttachments, OnAttachmentsChanged);
        }

        MessageBorder.Visibility = message is null ? Visibility.Collapsed : Visibility.Visible;
        MarkdownContent.Markdown = message?.Content ?? string.Empty;
        AttachmentsList.ItemsSource = message?.Attachments;
        AttachmentsList.Visibility = message?.Attachments.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        ActionButtons.Visibility = Visibility.Collapsed;
        TokenInfoPanel.Visibility = Visibility.Collapsed;
        TokensBorder.Visibility = Visibility.Collapsed;
        CostBorder.Visibility = Visibility.Collapsed;
        TimeBorder.Visibility = Visibility.Collapsed;
        TokensText.Text = CostText.Text = ResponseTimeText.Text = string.Empty;
        if (message is null) return;

        var isUser = message.Role == MessageRole.User;

        RoleIcon.Text = isUser ? "\uE77B" : "\uE99A";
        RoleText.Text = isUser ? "You" : "Assistant";
        TimeText.Text = message.CreatedAt.ToString("HH:mm");

        if (isUser)
        {
            MessageBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A2540"));
            MessageBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A3550"));
            MessageBorder.BorderThickness = new Thickness(1);
            ((Border)RoleIcon.Parent).Background =
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5CA0FC"));
        }
        else
        {
            MessageBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#131620"));
            MessageBorder.BorderThickness = new Thickness(0);
            ((Border)RoleIcon.Parent).Background =
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7C5CFC"));
        }

        if (message.Role == MessageRole.Assistant && !message.IsGenerating)
        {
            ActionButtons.Visibility = Visibility.Visible;

            if (message.TotalTokens > 0)
            {
                TokensText.Text = $"{message.TotalTokens:N0} tokens";
                TokensBorder.Visibility = Visibility.Visible;
            }

            if (message.Cost > 0)
            {
                CostText.Text = $"${message.Cost:F4}";
                CostBorder.Visibility = Visibility.Visible;
            }

            if (message.ResponseTimeMs > 0)
            {
                ResponseTimeText.Text = $"{message.ResponseTimeMs / 1000.0:F1}s";
                TimeBorder.Visibility = Visibility.Visible;
            }
            TokenInfoPanel.Visibility = message.TotalTokens > 0 || message.Cost > 0 || message.ResponseTimeMs > 0
                ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (Message is not null)
        {
            System.Windows.Clipboard.SetText(Message.Content);
            var toastService = App.ServiceProvider!.GetRequiredService<IToastService>();
            toastService.ShowSuccess("Message copied");
        }
    }

    private void RegenerateButton_Click(object sender, RoutedEventArgs e)
    {
        if (Message is not null)
        {
            var chatViewModel = DataContext as ViewModels.ChatViewModel;
            chatViewModel?.RegenerateCommand.Execute(Message);
        }
    }
}
