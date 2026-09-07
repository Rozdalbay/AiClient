using AiDesktopClient.Models;

namespace AiDesktopClient.Services;

public sealed class MockUsageService : IUsageService
{
    private readonly Random _random = new();

    public Task<UsageInfo> GetUsageAsync(DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var usage = new UsageInfo
        {
            TotalCost = 0m,
            TotalTokens = 0,
            TotalRequests = 0,
            DailyCost = 0m,
            DailyTokens = 0,
            DailyRequests = 0,
            BudgetLimit = 10.0m,
            CostChangePercent = 0,
            TokenChangePercent = 0,
            RequestChangePercent = 0
        };

        return Task.FromResult(usage);
    }

    public Task<IReadOnlyList<ModelUsageStat>> GetModelStatsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<ModelUsageStat>>(new List<ModelUsageStat>().AsReadOnly());
    }

    public Task<IReadOnlyList<DailyCostPoint>> GetDailyCostsAsync(int days = 7, CancellationToken cancellationToken = default)
    {
        var costs = new List<DailyCostPoint>();
        for (int i = days - 1; i >= 0; i--)
        {
            costs.Add(new DailyCostPoint
            {
                Date = DateTime.Now.AddDays(-i),
                Cost = 0m
            });
        }

        return Task.FromResult<IReadOnlyList<DailyCostPoint>>(costs.AsReadOnly());
    }

    public void RecordRequest(string modelId, string modelName, int inputTokens, int outputTokens, double responseTimeMs)
    {
        // No-op for mock
    }

    public void Save()
    {
        // No-op for mock
    }
}
