using AiDesktopClient.Models;

namespace AiDesktopClient.Services;

public sealed class MockUsageService : IUsageService
{
    private readonly Random _random = new();

    public Task<UsageInfo> GetUsageAsync(DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var usage = new UsageInfo
        {
            TotalCost = 12.45m,
            TotalTokens = 842150,
            TotalRequests = 1273,
            DailyCost = 1.42m,
            DailyTokens = 84215,
            DailyRequests = 127,
            BudgetLimit = 10.0m,
            CostChangePercent = -12,
            TokenChangePercent = -18,
            RequestChangePercent = 6
        };

        return Task.FromResult(usage);
    }

    public Task<IReadOnlyList<ModelUsageStat>> GetModelStatsAsync(CancellationToken cancellationToken = default)
    {
        var stats = new List<ModelUsageStat>
        {
            new() { ModelName = "Model A", Cost = 0.84m, Percentage = 59, Color = "#7C5CFC" },
            new() { ModelName = "Model B", Cost = 0.31m, Percentage = 22, Color = "#5CA0FC" },
            new() { ModelName = "Model C", Cost = 0.27m, Percentage = 19, Color = "#5CFCB0" }
        };

        return Task.FromResult<IReadOnlyList<ModelUsageStat>>(stats.AsReadOnly());
    }

    public Task<IReadOnlyList<DailyCostPoint>> GetDailyCostsAsync(int days = 7, CancellationToken cancellationToken = default)
    {
        var costs = new List<DailyCostPoint>();
        for (int i = days - 1; i >= 0; i--)
        {
            costs.Add(new DailyCostPoint
            {
                Date = DateTime.Now.AddDays(-i),
                Cost = (decimal)(_random.NextDouble() * 2.0 + 0.1)
            });
        }

        return Task.FromResult<IReadOnlyList<DailyCostPoint>>(costs.AsReadOnly());
    }
}
