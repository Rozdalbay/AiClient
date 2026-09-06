using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AiDesktopClient.Models;
using AiDesktopClient.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AiDesktopClient.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IModelService _modelService;
    private readonly IToastService _toastService;

    [ObservableProperty]
    private ChatViewModel _currentChatViewModel;

    [ObservableProperty]
    private object _currentView;

    [ObservableProperty]
    private string _selectedNavigation = "Chats";

    [ObservableProperty]
    private ObservableCollection<Chat> _chats = [];

    [ObservableProperty]
    private ObservableCollection<Project> _projects = [];

    [ObservableProperty]
    private ObservableCollection<ModelInfo> _availableModels = [];

    [ObservableProperty]
    private ModelInfo? _selectedModel;

    [ObservableProperty]
    private BackendConnectionStatus _backendStatus = new();

    [ObservableProperty]
    private UsageInfo _usageInfo = new();

    [ObservableProperty]
    private bool _isSidebarCollapsed;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    public MainViewModel(
        IChatService chatService,
        IModelService modelService,
        IUsageService usageService,
        IBackendService backendService,
        IToastService toastService)
    {
        _modelService = modelService;
        _toastService = toastService;
        _currentChatViewModel = new ChatViewModel(chatService, modelService, toastService, this);
        _currentView = _currentChatViewModel;
        _backendStatus = new BackendConnectionStatus { Status = Contracts.BackendStatus.Connected, LatencyMs = 142 };

        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        var models = await _modelService.GetModelsAsync();
        AvailableModels = new ObservableCollection<ModelInfo>(models);
        SelectedModel = AvailableModels.FirstOrDefault();

        CreateDefaultChats();
        CreateDefaultProjects();
    }

    private void CreateDefaultChats()
    {
        var defaultChats = new List<Chat>
        {
            new() { Title = "How to write a C# program", ModelId = "model-a", ModelName = "Model A",
                CreatedAt = DateTime.Now.AddHours(-1), UpdatedAt = DateTime.Now.AddHours(-1) },
            new() { Title = "Creating REST API", ModelId = "model-b", ModelName = "Model B",
                CreatedAt = DateTime.Now.AddHours(-3), UpdatedAt = DateTime.Now.AddHours(-3) },
            new() { Title = "Working with C# collections", ModelId = "model-a", ModelName = "Model A",
                CreatedAt = DateTime.Now.AddDays(-1), UpdatedAt = DateTime.Now.AddDays(-1) },
            new() { Title = "Python data analysis", ModelId = "model-c", ModelName = "Model C",
                CreatedAt = DateTime.Now.AddDays(-1), UpdatedAt = DateTime.Now.AddDays(-1) },
            new() { Title = "Docker containerization", ModelId = "model-a", ModelName = "Model A",
                CreatedAt = DateTime.Now.AddDays(-3), UpdatedAt = DateTime.Now.AddDays(-3), IsFavorite = true },
            new() { Title = "React hooks tutorial", ModelId = "model-b", ModelName = "Model B",
                CreatedAt = DateTime.Now.AddDays(-5), UpdatedAt = DateTime.Now.AddDays(-5) },
        };

        foreach (var chat in defaultChats)
            Chats.Add(chat);

        if (Chats.Count > 0)
            CurrentChatViewModel.LoadChat(Chats[0]);
    }

    private void CreateDefaultProjects()
    {
        Projects.Add(new Project { Name = "Development", Color = "#7C5CFC" });
        Projects.Add(new Project { Name = "Study", Color = "#5CA0FC" });
        Projects.Add(new Project { Name = "Web", Color = "#5CFCB0" });
        Projects.Add(new Project { Name = "Personal", Color = "#FCE55C" });
    }

    [RelayCommand]
    private void Navigate(string view)
    {
        SelectedNavigation = view;
        switch (view)
        {
            case "Chats":
                CurrentView = CurrentChatViewModel;
                break;
            case "Favorites":
                CurrentView = CurrentChatViewModel;
                break;
            case "Prompts":
                CurrentView = CurrentChatViewModel;
                break;
            case "Settings":
                if (CurrentView is not SettingsViewModel)
                {
                    var settingsVm = App.ServiceProvider!.GetRequiredService<SettingsViewModel>();
                    CurrentView = settingsVm;
                }
                break;
        }
    }

    [RelayCommand]
    private void CreateNewChat()
    {
        var newChat = new Chat
        {
            Title = "New Chat",
            ModelId = SelectedModel?.Id ?? "model-a",
            ModelName = SelectedModel?.DisplayName ?? "Model A"
        };
        Chats.Insert(0, newChat);
        CurrentChatViewModel.LoadChat(newChat);
        CurrentView = CurrentChatViewModel;
        SelectedNavigation = "Chats";
        _toastService.ShowSuccess("New chat created");
    }

    [RelayCommand]
    private void SelectChat(Chat chat)
    {
        CurrentChatViewModel.LoadChat(chat);
        CurrentView = CurrentChatViewModel;
        SelectedNavigation = "Chats";
    }

    [RelayCommand]
    private void RenameChat(Chat chat)
    {
    }

    [RelayCommand]
    private void ToggleFavorite(Chat chat)
    {
        chat.IsFavorite = !chat.IsFavorite;
        _toastService.ShowSuccess(chat.IsFavorite ? "Added to favorites" : "Removed from favorites");
    }

    [RelayCommand]
    private void TogglePin(Chat chat)
    {
        chat.IsPinned = !chat.IsPinned;
    }

    [RelayCommand]
    private void DeleteChat(Chat chat)
    {
        Chats.Remove(chat);
        if (CurrentChatViewModel.CurrentChat == chat && Chats.Count > 0)
            CurrentChatViewModel.LoadChat(Chats[0]);
        _toastService.ShowInfo("Chat deleted");
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarCollapsed = !IsSidebarCollapsed;
    }
}

public partial class BackendConnectionStatus : ObservableObject
{
    [ObservableProperty]
    private Contracts.BackendStatus _status = Contracts.BackendStatus.Connected;

    [ObservableProperty]
    private double _latencyMs;

    [ObservableProperty]
    private DateTime _lastChecked = DateTime.Now;

    [ObservableProperty]
    private string _backendVersion = "1.0.0";
}
