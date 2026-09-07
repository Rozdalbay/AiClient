using AiDesktopClient.Models;

namespace AiDesktopClient.Services;

// мокаем учёт для девелопмента без реального бэкенда: всегда пусто и девственно чисто, будто юзер вообще ничего не жёг
public sealed class MockUsageService : IUsageService
{
    public Task<UsagePeriodData> GetPeriodDataAsync(UsagePeriod period, CancellationToken cancellationToken = default)
    {
        // для ебланов: BudgetLimit тут 10 баксов чтобы пустая панель не выглядела совсем мёртвой, но данные всё равно пустые
        return Task.FromResult(new UsagePeriodData
        {
            Period = period,
            Cost = 0m,
            Tokens = 0,
            Requests = 0,
            BudgetLimit = 10.0m,
            CostChangePercent = null,
            TokenChangePercent = null,
            RequestChangePercent = null,
            HasAnyUsage = false,
            ModelStats = new List<ModelUsageStat>().AsReadOnly(),
            DailyCosts = new List<DailyCostPoint>().AsReadOnly()
        });
    }

    public void RecordRequest(string requestId, string modelId, string modelName, int inputTokens, int outputTokens, double responseTimeMs)
    {
        // No-op for mock
    }

    public void Save()
    {
        // No-op for mock
    }
}