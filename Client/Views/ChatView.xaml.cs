using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using AiDesktopClient.Models;
using AiDesktopClient.ViewModels;

namespace AiDesktopClient.Views;

// код-behind чата: скролл-трейлинг (сносит к низу при новых сообщениях), drag&drop файлов, селектор модели и скелетон при загрузке
public partial class ChatView : UserControl
{
    private ChatViewModel ViewModel => (ChatViewModel)DataContext;
    private ChatViewModel? _subscribedViewModel;
    private ObservableCollection<ChatMessage>? _subscribedMessages;
    private bool _followTail = true;
    private bool _scrollQueued;

    public ChatView()
    {
        InitializeComponent();
        Loaded += ChatView_Loaded;
        Unloaded += (_, _) => DetachViewModel();
        DataContextChanged += (_, _) => { if (IsLoaded) AttachViewModel(); };
        MessagesScroll.ScrollChanged += MessagesScroll_ScrollChanged;
        AllowDrop = true;
        DragEnter += ChatView_DragEnter;
        DragLeave += ChatView_DragLeave;
        Drop += ChatView_Drop;
    }

    private void ChatView_Loaded(object sender, RoutedEventArgs e)
    {
        AttachViewModel();
    }

    private void AttachViewModel()
    {
        DetachViewModel();
        _subscribedViewModel = DataContext as ChatViewModel;
        if (_subscribedViewModel is null) return;
        _subscribedViewModel.PropertyChanged += ViewModel_PropertyChanged;
        AttachMessages();
    }

    private void DetachViewModel()
    {
        if (_subscribedViewModel is not null)
            _subscribedViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        if (_subscribedMessages is not null)
            _subscribedMessages.CollectionChanged -= Messages_CollectionChanged;
        _subscribedViewModel = null;
        _subscribedMessages = null;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(ChatViewModel.Messages))
            AttachMessages();
        else if (e.PropertyName == nameof(ChatViewModel.IsMessagesLoading))
            UpdateEmptyState();
    }

    private void AttachMessages()
    {
        if (_subscribedMessages is not null)
            _subscribedMessages.CollectionChanged -= Messages_CollectionChanged;
        _subscribedMessages = _subscribedViewModel?.Messages;
        if (_subscribedMessages is not null)
            _subscribedMessages.CollectionChanged += Messages_CollectionChanged;
        _followTail = true;
        ScrollToBottom();
        UpdateEmptyState();
    }

    private void UpdateEmptyState()
    {
        var loading = _subscribedViewModel?.IsMessagesLoading == true;
        var hasMessages = _subscribedMessages is not null && _subscribedMessages.Count > 0;

        MessagesSkeleton.Visibility = loading ? Visibility.Visible : Visibility.Collapsed;
        EmptyState.Visibility = !loading && !hasMessages ? Visibility.Visible : Visibility.Collapsed;
        MessagesList.Visibility = !loading && hasMessages ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Messages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            _followTail = true;
            ScrollToBottom();
        }
        UpdateEmptyState();
    }

    private void MessagesScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, MessagesScroll)) return;
        if (e.ExtentHeightChange == 0 && e.VerticalChange != 0)
            _followTail = MessagesScroll.VerticalOffset >= MessagesScroll.ScrollableHeight - 2;
        if (e.ExtentHeightChange != 0 && _followTail)
            ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        if (_scrollQueued) return;
        _scrollQueued = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
        {
            _scrollQueued = false;
            if (_subscribedViewModel is not null && _followTail)
                MessagesScroll.ScrollToEnd();
        }));
    }

    private void MessageInput_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            e.Handled = true;
            if (!ViewModel.IsStreaming && !string.IsNullOrWhiteSpace(ViewModel.InputMessage))
                ViewModel.SendMessageCommand.Execute(null);
        }
    }

    private void MessageInput_KeyDown(object sender, KeyEventArgs e)
    {
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
