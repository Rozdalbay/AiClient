using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AiDesktopClient.Models;
using AiDesktopClient.Services;

namespace AiDesktopClient.ViewModels;

// обёртка над IUsageService для GUI: тут живут выбранный период, статусы загрузки/ошибки и лампочка HasAnyUsage, UI НЕ должен трогать сервис напрямую
public partial class UsageViewModel : ObservableObject
{
    private readonly IUsageService _usageService;
    // счётчик поколений загрузки: юзер дёрнул период, пока старый запрос летел - старый ответ нахуй не нужен, сверяем generation
    private int _loadGeneration;

    [ObservableProperty]
    private UsagePeriod _selectedPeriod = UsagePeriod.Today;

    [ObservableProperty]
    private string _selectedPeriodLabel = "Today";

    [ObservableProperty]
    private UsagePeriodData? _data;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isError;

    public event EventHandler? DataRefreshed;

    public bool HasAnyUsage => Data?.HasAnyUsage == true;

    public bool HasBudget => Data is { BudgetLimit: > 0 };

    public bool HasModelStats => Data is { ModelStats.Count: > 0 };

    public double BudgetUsedPercent { get; private set; }

    public UsageViewModel(IUsageService usageService)
    {
        _usageService = usageService;
    }

    // данные пришли - обновляем сводные флажки и стреляем событием DataRefreshed, по нему code-behind перерисовывает график деньги-деньги-лох
    partial void OnDataChanged(UsagePeriodData? value)
    {
        OnPropertyChanged(nameof(HasAnyUsage));
        OnPropertyChanged(nameof(HasBudget));
        OnPropertyChanged(nameof(HasModelStats));
        BudgetUsedPercent = value is { BudgetLimit: > 0 }
            ? Math.Clamp((double)(value.Cost / value.BudgetLimit * 100), 0, 100)
            : 0;
        OnPropertyChanged(nameof(BudgetUsedPercent));

        DataRefreshed?.Invoke(this, EventArgs.Empty);
    }

    // период сменился - лепим человеческий лейбл и сразу дёргаем загрузку; без этого рефреша панель останется с прошлогодними данными
    partial void OnSelectedPeriodChanged(UsagePeriod value)
    {
        SelectedPeriodLabel = value switch
        {
            UsagePeriod.Week => "7 days",
            UsagePeriod.Month => "30 days",
            UsagePeriod.AllTime => "All time",
            _ => "Today"
        };

        _ = RefreshAsync();
    }

    // единственная точка обновления: try/catch/finally гарантируют, что IsLoading снимется ВСЕГДА, иначе UI зависнет на скелетоне и юзер будет материться как сапожник
    public async Task RefreshAsync()
    {
        // берём поколение ДО await, чтобы после него понять - не устарел ли наш ответ
        var generation = ++_loadGeneration;

        IsError = false;
        IsLoading = true;

        try
        {
            var data = await _usageService.GetPeriodDataAsync(SelectedPeriod);
            if (generation != _loadGeneration)
                return;

            Data = data;
        }
        catch
        {
            if (generation != _loadGeneration)
                return;

            Data = null;
            IsError = true;
        }
        finally
        {
            if (generation == _loadGeneration)
                IsLoading = false;
        }
    }

    // кнопка-спаситель для юзера: "Повторить" после ошибки, ну а если снова упадёт - опять заглючит в IsError, и так по кругу до бесконечности
    [RelayCommand]
    private void RefreshUsage()
    {
        _ = RefreshAsync();
    }
}