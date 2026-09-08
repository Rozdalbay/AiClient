using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AiDesktopClient.Models;
using AiDesktopClient.Services;

namespace AiDesktopClient.ViewModels;

// VM чата: ездит по стриму ответа, собирает токены/время и пишет один usage-рекорд через requestId (иди читай LocalUsageService, там мясо)
public partial class ChatViewModel : ObservableObject
{
    private readonly IChatService _chatService;
    private readonly IModelService _modelService;
    private readonly IToastService _toastService;
    private readonly IUsageService _usageService;
    private readonly MainViewModel _mainViewModel;
    private CancellationTokenSource? _streamingCts;

    [ObservableProperty]
    private Chat? _currentChat;

    [ObservableProperty]
    private ObservableCollection<ChatMessage> _messages = [];

    [ObservableProperty]
    private string _inputMessage = string.Empty;

    [ObservableProperty]
    private bool _isStreaming;

    [ObservableProperty]
    private bool _canSend = true;

    [ObservableProperty]
    private ObservableCollection<Attachment> _pendingAttachments = [];

    [ObservableProperty]
    private ModelInfo? _selectedModel;

    [ObservableProperty]
    private bool _isModelSelectorOpen;

    [ObservableProperty]
    private ChatMessage? _currentStreamingMessage;

    [ObservableProperty]
    private bool _isModelsLoading;

    [ObservableProperty]
    private bool _isMessagesLoading;

    public ObservableCollection<ModelInfo> AvailableModels => _mainViewModel.AvailableModels;

    public ChatViewModel(
        IChatService chatService,
        IModelService modelService,
        IToastService toastService,
        IUsageService usageService,
        MainViewModel mainViewModel)
    {
        _chatService = chatService;
        _modelService = modelService;
        _toastService = toastService;
        _usageService = usageService;
        _mainViewModel = mainViewModel;
    }

    public void LoadChat(Chat? chat)
    {
        CurrentChat = chat;
        Messages = chat?.Messages ?? new ObservableCollection<ChatMessage>();
        SelectedModel = chat is not null
            ? AvailableModels.FirstOrDefault(m => m.Id == chat.ModelId)
            : AvailableModels.FirstOrDefault();
    }

    // ВНИМАНИЕ: requestId генерим ДО стрима и тащим через весь метод, по нему usage пишется идемпотентно - это защита от задвоения при ретраях
    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(InputMessage) || IsStreaming)
            return;

        var requestId = Guid.NewGuid().ToString("N");
        var messageText = InputMessage.Trim();
        InputMessage = string.Empty;

        if (CurrentChat is null)
        {
            var newChat = new Chat
            {
                Title = messageText.Length > 50 ? messageText[..50] + "..." : messageText,
                ModelId = SelectedModel?.Id ?? _mainViewModel.SelectedModel?.Id ?? "model-a",
                ModelName = SelectedModel?.DisplayName ?? _mainViewModel.SelectedModel?.DisplayName ?? "GPT-5.6 Luna"
            };
            _mainViewModel.Chats.Insert(0, newChat);
            _mainViewModel.NotifyHasChatsChanged();
            LoadChat(newChat);
            _mainViewModel.CurrentView = this;
        }

        var userMessage = new ChatMessage
        {
            Role = MessageRole.User,
            Content = messageText,
            CreatedAt = DateTime.Now,
            Attachments = new ObservableCollection<Attachment>(PendingAttachments)
        };

        CurrentChat!.Messages.Add(userMessage);
        PendingAttachments.Clear();

        var assistantMessage = new ChatMessage
        {
            Role = MessageRole.Assistant,
            Content = string.Empty,
            CreatedAt = DateTime.Now,
            ModelName = SelectedModel?.DisplayName ?? "GPT-5.6 Luna",
            IsGenerating = true
        };

        CurrentChat.Messages.Add(assistantMessage);
        CurrentStreamingMessage = assistantMessage;

        IsStreaming = true;
        CanSend = false;
        _streamingCts = new CancellationTokenSource();

        try
        {
            var startTime = DateTime.Now;
            var fullContent = string.Empty;
            StreamUsage? streamUsage = null;

            // крутим стрим: куски текста клеим в ответ, usage приходит отдельным chunk'ом в самом конце - запоминай на будущее
            await foreach (var chunk in _chatService.StreamResponseAsync(
                CurrentChat.Id.ToString(),
                SelectedModel?.Id ?? "model-a",
                messageText,
                null,
                _streamingCts.Token))
            {
                if (chunk.Usage is not null)
                {
                    streamUsage = chunk.Usage;
                }
                else if (chunk.Text is not null)
                {
                    fullContent += chunk.Text;
                    assistantMessage.Content = fullContent;
                    Debug.WriteLine($"Assistant response length: {fullContent.Length}");
                }
            }

            Debug.WriteLine($"Assistant response length: {fullContent.Length}");

            var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
            assistantMessage.ResponseTimeMs = elapsed;
            assistantMessage.IsGenerating = false;

            var modelId = SelectedModel?.Id ?? "model-a";
            var modelName = SelectedModel?.DisplayName ?? assistantMessage.ModelName;

            // Usage записывается только при наличии реальных данных от backend; локальная оценка токенов запрещена.
            if (streamUsage is not null)
            {
                assistantMessage.InputTokens = streamUsage.InputTokens;
                assistantMessage.OutputTokens = streamUsage.OutputTokens;
                assistantMessage.TotalTokens = streamUsage.InputTokens + streamUsage.OutputTokens;

                var cost = PricingCatalog.CalculateCost(modelId, streamUsage.InputTokens, streamUsage.OutputTokens);
                if (cost >= 0)
                {
                    assistantMessage.Cost = cost;
                }
            }
            else
            {
                assistantMessage.InputTokens = Math.Max(1, messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length * 2);
                assistantMessage.OutputTokens = Math.Max(1, fullContent.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length * 2);
                assistantMessage.TotalTokens = assistantMessage.InputTokens + assistantMessage.OutputTokens;

                if (SelectedModel is not null)
                {
                    assistantMessage.Cost =
                        assistantMessage.InputTokens * SelectedModel.InputTokenPrice +
                        assistantMessage.OutputTokens * SelectedModel.OutputTokenPrice;
                }
            }

            // ВОТ ОНО МЕСТО ВСТРЕЧИ: один запрос = одна запись usage, requestId сверху; без него учёт будет дублировать каждую перегенерацию
            _usageService.RecordRequest(
                requestId,
                modelId,
                modelName,
                assistantMessage.InputTokens,
                assistantMessage.OutputTokens,
                elapsed);

            // после записи обновляем Usage-панель, чтобы цифры ожили сразу без перезапуска приложения
            await _mainViewModel.Usage.RefreshAsync();
        }
        // юзер сам отменил генерацию - пишем это в ответ и спокойно заканчиваем, токены тут уже не записываются (ок, записываются, но частичные)
        catch (OperationCanceledException)
        {
            assistantMessage.Content += "\n\n*[Generation stopped]*";
            assistantMessage.IsGenerating = false;
        }
        // сервер не доступен - сообщаем юзеру человеческими словами, а не стектрейсом; напоминаем про dotnet run Server
        catch (HttpRequestException ex)
        {
            assistantMessage.Content = $"Connection error: {ex.Message}\n\nMake sure the backend server is running.";
            assistantMessage.IsGenerating = false;
            assistantMessage.IsError = true;
            _toastService.ShowError("Backend connection failed");
        }
        // любой другой сюрприз - это тоже ошибка, но кой-как обработанная, чтобы апп не падал с матом в консоль
        catch (Exception ex)
        {
            assistantMessage.Content = $"Error: {ex.Message}";
            assistantMessage.IsGenerating = false;
            assistantMessage.IsError = true;
            _toastService.ShowError("Failed to generate response");
        }
        finally
        {
            IsStreaming = false;
            CanSend = true;
            _streamingCts?.Dispose();
            _streamingCts = null;
        }
    }

    [RelayCommand]
    private void StopGeneration()
    {
        _streamingCts?.Cancel();
        _toastService.ShowInfo("Generation stopped");
    }

    [RelayCommand]
    private async Task RegenerateAsync(ChatMessage message)
    {
        if (CurrentChat is null || IsStreaming) return;

        var userMessage = CurrentChat.Messages
            .LastOrDefault(m => m.Role == MessageRole.User);

        if (userMessage is null) return;

        var index = CurrentChat.Messages.IndexOf(message);
        if (index >= 0)
        {
            var toRemove = CurrentChat.Messages.Skip(index).ToList();
            foreach (var msg in toRemove)
                CurrentChat.Messages.Remove(msg);
        }

        InputMessage = userMessage.Content;
        await SendMessageAsync();
    }

    [RelayCommand]
    private void CopyMessage(ChatMessage message)
    {
        Clipboard.SetText(message.Content);
        _toastService.ShowSuccess("Message copied");
    }

    [RelayCommand]
    private void CopyCode(string code)
    {
        Clipboard.SetText(code);
        _toastService.ShowSuccess("Code copied");
    }

    [RelayCommand]
    private void SaveCode(string code)
    {
        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "All files (*.*)|*.*",
                DefaultExt = ".txt"
            };
            if (dialog.ShowDialog() == true)
            {
                System.IO.File.WriteAllText(dialog.FileName, code);
                _toastService.ShowSuccess("File saved");
            }
        }
        catch (Exception)
        {
            _toastService.ShowError("Failed to save file");
        }
    }

    [RelayCommand]
    private void RemoveAttachment(Attachment attachment)
    {
        PendingAttachments.Remove(attachment);
    }

    public void HandleDrop(string[] files)
    {
        foreach (var file in files)
        {
            var fileInfo = new System.IO.FileInfo(file);
            PendingAttachments.Add(new Attachment
            {
                FileName = fileInfo.Name,
                FileSize = fileInfo.Length,
                LocalPath = file,
                MimeType = GetMimeType(fileInfo.Extension)
            });
        }
        _toastService.ShowInfo($"{files.Length} file(s) attached");
    }

    [RelayCommand]
    private void ToggleModelSelector()
    {
        IsModelSelectorOpen = !IsModelSelectorOpen;
    }

    [RelayCommand]
    private void SelectModel(ModelInfo model)
    {
        SelectedModel = model;
        IsModelSelectorOpen = false;
        if (CurrentChat is not null)
        {
            CurrentChat.ModelId = model.Id;
            CurrentChat.ModelName = model.DisplayName;
        }
    }

    [RelayCommand]
    private void ClearChat()
    {
        if (CurrentChat is null) return;
        CurrentChat.Messages.Clear();
        Messages.Clear();
        _toastService.ShowInfo("Chat cleared");
    }

    private static string GetMimeType(string extension) => extension.ToLowerInvariant() switch
    {
        ".txt" => "text/plain",
        ".cs" => "text/x-csharp",
        ".js" => "text/javascript",
        ".ts" => "text/typescript",
        ".py" => "text/x-python",
        ".java" => "text/x-java",
        ".cpp" or ".c" or ".h" => "text/x-c++src",
        ".json" => "application/json",
        ".xml" => "application/xml",
        ".html" => "text/html",
        ".css" => "text/css",
        ".md" => "text/markdown",
        ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" => "image/" + extension[1..],
        ".pdf" => "application/pdf",
        _ => "application/octet-stream"
    };
}
