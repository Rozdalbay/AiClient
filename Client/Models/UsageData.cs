namespace AiDesktopClient.Models;

public sealed class UsageRecord
{
    public DateTime Timestamp { get; set; }
    public string ModelId { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens { get; set; }
    public double Cost { get; set; }
    public double ResponseTimeMs { get; set; }
}

public sealed class UsageDataStore
{
    public decimal TotalCost { get; set; }
    public long TotalTokens { get; set; }
    public int TotalRequests { get; set; }
    public decimal DailyCost { get; set; }
    public long DailyTokens { get; set; }
    public int DailyRequests { get; set; }
    public decimal BudgetLimit { get; set; } = 10.0m;
    public decimal CostChangePercent { get; set; }
    public decimal TokenChangePercent { get; set; }
    public decimal RequestChangePercent { get; set; }
    public List<UsageRecord> Records { get; set; } = [];
    public List<DailyCostEntry> DailyCosts { get; set; } = [];
}

public sealed class DailyCostEntry
{
    public DateTime Date { get; set; }
    public decimal Cost { get; set; }
}
