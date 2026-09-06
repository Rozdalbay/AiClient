using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AiDesktopClient.Models;
using AiDesktopClient.ViewModels;

namespace AiDesktopClient.Views;

public partial class ChatView : UserControl
{
    private ChatViewModel ViewModel => (ChatViewModel)DataContext;

    public ChatView()
    {
        InitializeComponent();
        Loaded += ChatView_Loaded;
        AllowDrop = true;
        DragEnter += ChatView_DragEnter;
        DragLeave += ChatView_DragLeave;
        Drop += ChatView_Drop;
    }

    private void ChatView_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.Messages.CollectionChanged += (s, args) =>
        {
            if (args.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                ScrollToBottom();
        };
    }

    private void ScrollToBottom()
    {
        Dispatcher.Invoke(() =>
        {
            MessagesScroll.ScrollToEnd();
        });
    }

    private void MessageInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                return;

            e.Handled = true;
            if (!ViewModel.IsStreaming && !string.IsNullOrWhiteSpace(ViewModel.InputMessage))
                ViewModel.SendMessageCommand.Execute(null);
        }
    }

    private void ModelSelector_Click(object sender, MouseButtonEventArgs e)
    {
        ViewModel.IsModelSelectorOpen = !ViewModel.IsModelSelectorOpen;
        ModelDropdown.Visibility = ViewModel.IsModelSelectorOpen
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void ModelItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is ModelInfo model)
        {
            ViewModel.SelectModelCommand.Execute(model);
            ModelDropdown.Visibility = Visibility.Collapsed;
            SelectedModelText.Text = model.DisplayName;
            InputModelBadge.Text = model.DisplayName;
        }
    }

    private void ClearChat_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ClearChatCommand.Execute(null);
    }

    private void AttachButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Multiselect = true,
            Filter = "All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            foreach (var file in dialog.FileNames)
            {
                var fileInfo = new System.IO.FileInfo(file);
                ViewModel.PendingAttachments.Add(new Attachment
                {
                    FileName = fileInfo.Name,
                    FileSize = fileInfo.Length,
                    LocalPath = file,
                    MimeType = "application/octet-stream"
                });
            }
        }
    }

    private void RemoveAttachment_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Attachment attachment)
        {
            ViewModel.RemoveAttachmentCommand.Execute(attachment);
        }
    }

    private void ChatView_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            DropOverlay.Visibility = Visibility.Visible;
            e.Handled = true;
        }
    }

    private void ChatView_DragLeave(object sender, DragEventArgs e)
    {
        DropOverlay.Visibility = Visibility.Collapsed;
    }

    private void ChatView_Drop(object sender, DragEventArgs e)
    {
        DropOverlay.Visibility = Visibility.Collapsed;

        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files is not null)
                ViewModel.HandleDrop(files);
        }
    }
}
