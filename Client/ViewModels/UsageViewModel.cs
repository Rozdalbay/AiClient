using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AiDesktopClient.Models;
using AiDesktopClient.Services;

namespace AiDesktopClient.ViewModels;

public partial class UsageViewModel : ObservableObject
{
    private readonly IUsageService _usageService;
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

    public async Task RefreshAsync()
    {
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

    [RelayCommand]
    private void RefreshUsage()
    {
        _ = RefreshAsync();
    }
}