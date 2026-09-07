using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Threading;
using System.Windows.Data;
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
    private readonly IUsageService _usageService;
    private readonly IBackendService _backendService;
    private readonly AuthService _authService;
    private readonly DispatcherTimer _healthCheckTimer;

    [ObservableProperty]
    private ChatViewModel _currentChatViewModel;

    [ObservableProperty]
    private object _currentView;

    [ObservableProperty]
    private string _selectedNavigation = "Chats";

    [ObservableProperty]
    private ObservableCollection<Chat> _chats = [];

    [ObservableProperty]
    private ObservableCollection<ModelInfo> _availableModels = [];

    [ObservableProperty]
    private ObservableCollection<ModelUsageStat> _modelStats = [];

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

    [ObservableProperty]
    private bool _isChatVisible = true;

    [ObservableProperty]
    private bool _isPromptsVisible;

    [ObservableProperty]
    private bool _isUsagePanelOpen = true;

    public bool HasChats => Chats.Count > 0;

    public void NotifyHasChatsChanged() => OnPropertyChanged(nameof(HasChats));

    public ICollectionView ChatsView { get; }

    public MainViewModel(
        IChatService chatService,
        IModelService modelService,
        IUsageService usageService,
        IBackendService backendService,
        IToastService toastService,
        AuthService authService)
    {
        _modelService = modelService;
        _toastService = toastService;
        _usageService = usageService;
        _backendService = backendService;
        _authService = authService;
        _currentChatViewModel = new ChatViewModel(chatService, modelService, toastService, usageService, this);
        _currentView = _currentChatViewModel;
        _backendStatus = new BackendConnectionStatus { Status = Contracts.BackendStatus.Connecting };

        ChatsView = CollectionViewSource.GetDefaultView(Chats);
        ChatsView.Filter = FilterChats;

        _healthCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _healthCheckTimer.Tick += async (_, _) => await CheckBackendHealthAsync();
        _healthCheckTimer.Start();

        InitializeAsync();
        _ = CheckBackendHealthAsync();
    }

    private bool FilterChats(object obj)
    {
        if (obj is not Chat chat) return false;
        if (SelectedNavigation == "Favorites")
            return chat.IsFavorite;
        if (!string.IsNullOrWhiteSpace(SearchQuery))
            return chat.Title.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase);
        return true;
    }

    partial void OnSelectedNavigationChanged(string value)
    {
        IsChatVisible = value is "Chats" or "Favorites";
        IsPromptsVisible = value == "Prompts";
        ChatsView.Refresh();

        if (value == "Settings" && CurrentView is not SettingsViewModel)
        {
            var settingsVm = App.ServiceProvider!.GetRequiredService<SettingsViewModel>();
            CurrentView = settingsVm;
        }
        else if (value != "Settings" && CurrentView is SettingsViewModel)
        {
            CurrentView = CurrentChatViewModel;
        }
    }

    partial void OnSearchQueryChanged(string value)
    {
        ChatsView.Refresh();
    }

    partial void OnChatsChanged(ObservableCollection<Chat> value)
    {
        OnPropertyChanged(nameof(HasChats));
        if (ChatsView is ICollectionView view)
            view.Refresh();
    }

    private async void InitializeAsync()
    {
        var models = await _modelService.GetModelsAsync();
        AvailableModels = new ObservableCollection<ModelInfo>(models);
        SelectedModel = AvailableModels.FirstOrDefault();

        await RefreshUsageAsync();
    }

    public async Task RefreshUsageAsync()
    {
        var usage = await _usageService.GetUsageAsync();
        UsageInfo = usage;

        var session = _authService.LoadSession();
        UsageInfo.AccountUsername = session?.Username ?? string.Empty;
        UsageInfo.AccountStatus = "Local account";

        var modelStats = await _usageService.GetModelStatsAsync();
        ModelStats = new ObservableCollection<ModelUsageStat>(modelStats);
        UsageInfo.ModelStats = ModelStats;

        var dailyCosts = await _usageService.GetDailyCostsAsync(7);
        UsageInfo.DailyCosts = new ObservableCollection<DailyCostPoint>(dailyCosts);
    }

    private async Task CheckBackendHealthAsync()
    {
        var result = await _backendService.GetStatusAsync();
        BackendStatus.Status = result.Status;
        BackendStatus.LatencyMs = result.LatencyMs;
        BackendStatus.LastChecked = result.LastChecked;
        BackendStatus.BackendVersion = result.BackendVersion;
    }

    [RelayCommand]
    private void Navigate(string view)
    {
        SelectedNavigation = view;
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
        NotifyHasChatsChanged();
        ChatsView.Refresh();
        CurrentChatViewModel.LoadChat(newChat);
        CurrentView = CurrentChatViewModel;
        SelectedNavigation = "Chats";
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
        ChatsView.Refresh();
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
        NotifyHasChatsChanged();
        ChatsView.Refresh();
        if (CurrentChatViewModel.CurrentChat == chat)
        {
            if (Chats.Count > 0)
                CurrentChatViewModel.LoadChat(Chats[0]);
            else
                CurrentChatViewModel.LoadChat(null!);
        }
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarCollapsed = !IsSidebarCollapsed;
    }

    [RelayCommand]
    private void ToggleUsagePanel()
    {
        IsUsagePanelOpen = !IsUsagePanelOpen;
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
