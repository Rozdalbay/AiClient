namespace AiDesktopClient.Models;

// периоды аналитики: Today/Week/Month/AllTime, порядок имеет значение для switch'ей ниже - не переставь по приколу
public enum UsagePeriod
{
    Today,
    Week,
    Month,
    AllTime
}

// immutable-пакет данных для UI: один объект - вся аналитика за период + предыдущий период для процентов; это АКТУАЛЬНАЯ модель, а не та статика в UsageInfo
public sealed class UsagePeriodData
{
    public UsagePeriod Period { get; init; }
    public decimal Cost { get; init; }
    public long Tokens { get; init; }
    public int Requests { get; init; }
    public decimal BudgetLimit { get; init; }
    public decimal? CostChangePercent { get; init; }
    public decimal? TokenChangePercent { get; init; }
    public decimal? RequestChangePercent { get; init; }
    public bool HasAnyUsage { get; init; }
    public IReadOnlyList<ModelUsageStat> ModelStats { get; init; } = [];
    public IReadOnlyList<DailyCostPoint> DailyCosts { get; init; } = [];
}