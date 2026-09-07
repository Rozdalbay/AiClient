namespace AiDesktopClient.Models;

public enum UsagePeriod
{
    Today,
    Week,
    Month,
    AllTime
}

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