using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AiDesktopClient.Models;

// АХТУНГ ЛЕГАСИ: этот класс - реликвия от старого учёта, теперь всё валится в UsagePeriodData; не чини тут ничего, пусть стоит памятником, прям как дуб
public partial class UsageInfo : ObservableObject
{
    [ObservableProperty]
    private decimal _totalCost;

    [ObservableProperty]
    private long _totalTokens;

    [ObservableProperty]
    private int _totalRequests;

    [ObservableProperty]
    private decimal _dailyCost;

    [ObservableProperty]
    private long _dailyTokens;

    [ObservableProperty]
    private int _dailyRequests;

    [ObservableProperty]
    private decimal _budgetLimit = 10.0m;

    [ObservableProperty]
    private decimal _costChangePercent;

    [ObservableProperty]
    private decimal _tokenChangePercent;

    [ObservableProperty]
    private decimal _requestChangePercent;

    [ObservableProperty]
    private ObservableCollection<ModelUsageStat> _modelStats = [];

    [ObservableProperty]
    private ObservableCollection<DailyCostPoint> _dailyCosts = [];

    public decimal BudgetProgress => BudgetLimit > 0 ? Math.Min(DailyCost / BudgetLimit, 1m) : 0;
}

// статистика по одной модели для карточки в Usage: доля, запросы, токены и цвет-стикер
public partial class ModelUsageStat : ObservableObject
{
    [ObservableProperty]
    private string _modelName = string.Empty;

    [ObservableProperty]
    private string _modelId = string.Empty;

    [ObservableProperty]
    private decimal _cost;

    [ObservableProperty]
    private double _percentage;

    [ObservableProperty]
    private int _requestCount;

    [ObservableProperty]
    private long _totalTokens;

    [ObservableProperty]
    private string _color = "#7C5CFC";
}

// точка дня для графика затрат, pair (дата, стоимость)
public partial class DailyCostPoint : ObservableObject
{
    [ObservableProperty]
    private DateTime _date;

    [ObservableProperty]
    private decimal _cost;
}
