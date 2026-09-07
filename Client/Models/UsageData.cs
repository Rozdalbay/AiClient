namespace AiDesktopClient.Models;

// одна запись потраченного запроса: requestId - джентльменский набор для идемпотентности; CAUTION: duplicate requestId игнорируется намеренно
public sealed class UsageRecord
{
    public string RequestId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string ModelId { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens { get; set; }
    public double Cost { get; set; }
    public double ResponseTimeMs { get; set; }
}

// сам сраный файл usage.json: хранит итоги, дневные агрегаты и все записи; BudgetLimit живёт здесь же (сейчас 10 баксов по дефолту)
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

// дневная точка для графика, где date - начало бакета, cost - сумма в нём
public sealed class DailyCostEntry
{
    public DateTime Date { get; set; }
    public decimal Cost { get; set; }
}
