using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using AiDesktopClient.Models;

namespace AiDesktopClient.Services;

public sealed class LocalUsageService : IUsageService
{
    private static readonly string UsagePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AiDesktopClient",
        "usage.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly object _lock = new();
    private UsageDataStore _store;

    private static readonly string[] ModelColors =
    [
        "#7C5CFC", "#5CA0FC", "#5CFCB0", "#FC5CA0",
        "#FCE55C", "#FC8C5C", "#5CFCFC", "#C85CFC"
    ];

    public LocalUsageService()
    {
        _store = Load();
    }

    public void RecordRequest(string modelId, string modelName, int inputTokens, int outputTokens, double responseTimeMs)
    {
        var totalTokens = inputTokens + outputTokens;
        var pricing = PricingCatalog.GetPricing(modelId);
        var cost = pricing is not null
            ? inputTokens / 1_000_000.0 * pricing.InputPricePerMillion + outputTokens / 1_000_000.0 * pricing.OutputPricePerMillion
            : 0.0;

        var record = new UsageRecord
        {
            Timestamp = DateTime.Now,
            ModelId = modelId,
            ModelName = modelName,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            TotalTokens = totalTokens,
            Cost = cost,
            ResponseTimeMs = responseTimeMs
        };

        lock (_lock)
        {
            _store.Records.Add(record);
            _store.TotalCost += (decimal)cost;
            _store.TotalTokens += totalTokens;
            _store.TotalRequests++;

            var today = DateTime.Today;
            var todayRecords = _store.Records.Where(r => r.Timestamp.Date == today).ToList();
            _store.DailyCost = todayRecords.Sum(r => (decimal)r.Cost);
            _store.DailyTokens = todayRecords.Sum(r => (long)r.TotalTokens);
            _store.DailyRequests = todayRecords.Count;

            var recordDate = record.Timestamp.Date;
            var existing = _store.DailyCosts.FirstOrDefault(d => d.Date.Date == recordDate);
            if (existing is not null)
            {
                existing.Cost += (decimal)cost;
            }
            else
            {
                _store.DailyCosts.Add(new DailyCostEntry { Date = recordDate, Cost = (decimal)cost });
            }

            UpdateChangePercentages();
        }

        Save();
    }

    public Task<UsageInfo> GetUsageAsync(DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var today = DateTime.Today;
            var todayRecords = _store.Records.Where(r => r.Timestamp.Date == today).ToList();

            var usage = new UsageInfo
            {
                TotalCost = _store.TotalCost,
                TotalTokens = _store.TotalTokens,
                TotalRequests = _store.TotalRequests,
                DailyCost = todayRecords.Sum(r => (decimal)r.Cost),
                DailyTokens = todayRecords.Sum(r => (long)r.TotalTokens),
                DailyRequests = todayRecords.Count,
                BudgetLimit = _store.BudgetLimit,
                CostChangePercent = _store.CostChangePercent,
                TokenChangePercent = _store.TokenChangePercent,
                RequestChangePercent = _store.RequestChangePercent
            };

            return Task.FromResult(usage);
        }
    }

    public Task<IReadOnlyList<ModelUsageStat>> GetModelStatsAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var modelGroups = _store.Records
                .GroupBy(r => r.ModelId)
                .Select(g => new
                {
                    ModelId = g.Key,
                    ModelName = g.First().ModelName,
                    TotalCost = g.Sum(r => (decimal)r.Cost),
                    RequestCount = g.Count(),
                    TotalTokens = g.Sum(r => (long)r.TotalTokens)
                })
                .OrderByDescending(m => m.TotalCost)
                .ToList();

            var totalCost = modelGroups.Sum(m => m.TotalCost);
            var stats = new List<ModelUsageStat>();

            for (int i = 0; i < modelGroups.Count; i++)
            {
                var m = modelGroups[i];
                stats.Add(new ModelUsageStat
                {
                    ModelId = m.ModelId,
                    ModelName = m.ModelName,
                    Cost = m.TotalCost,
                    Percentage = totalCost > 0 ? (double)(m.TotalCost / totalCost * 100) : 0,
                    RequestCount = m.RequestCount,
                    TotalTokens = m.TotalTokens,
                    Color = ModelColors[i % ModelColors.Length]
                });
            }

            return Task.FromResult<IReadOnlyList<ModelUsageStat>>(stats.AsReadOnly());
        }
    }

    public Task<IReadOnlyList<DailyCostPoint>> GetDailyCostsAsync(int days = 7, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var points = new List<DailyCostPoint>();
            for (int i = days - 1; i >= 0; i--)
            {
                var date = DateTime.Today.AddDays(-i);
                var entry = _store.DailyCosts.FirstOrDefault(d => d.Date.Date == date);
                points.Add(new DailyCostPoint
                {
                    Date = date,
                    Cost = entry?.Cost ?? 0m
                });
            }

            return Task.FromResult<IReadOnlyList<DailyCostPoint>>(points.AsReadOnly());
        }
    }

    public void Save()
    {
        try
        {
            lock (_lock)
            {
                var dir = Path.GetDirectoryName(UsagePath)!;
                Directory.CreateDirectory(dir);
                var json = JsonSerializer.Serialize(_store, JsonOptions);
                File.WriteAllText(UsagePath, json);
            }
        }
        catch
        {
            // Silently ignore save failures
        }
    }

    private UsageDataStore Load()
    {
        try
        {
            if (File.Exists(UsagePath))
            {
                var json = File.ReadAllText(UsagePath);
                var data = JsonSerializer.Deserialize<UsageDataStore>(json);
                if (data is not null)
                {
                    var today = DateTime.Today;
                    var todayRecords = data.Records.Where(r => r.Timestamp.Date == today).ToList();
                    data.DailyCost = todayRecords.Sum(r => (decimal)r.Cost);
                    data.DailyTokens = todayRecords.Sum(r => (long)r.TotalTokens);
                    data.DailyRequests = todayRecords.Count;

                    UpdateChangePercentagesForStore(data);
                    return data;
                }
            }
        }
        catch
        {
            // Fall through to default store
        }

        return new UsageDataStore();
    }

    private void UpdateChangePercentages()
    {
        var today = DateTime.Today;
        var yesterday = today.AddDays(-1);

        var todayRecords = _store.Records.Where(r => r.Timestamp.Date == today).ToList();
        var yesterdayRecords = _store.Records.Where(r => r.Timestamp.Date == yesterday).ToList();

        var todayCost = todayRecords.Sum(r => (decimal)r.Cost);
        var yesterdayCost = yesterdayRecords.Sum(r => (decimal)r.Cost);

        var todayTokens = todayRecords.Sum(r => (long)r.TotalTokens);
        var yesterdayTokens = yesterdayRecords.Sum(r => (long)r.TotalTokens);

        var todayReqs = todayRecords.Count;
        var yesterdayReqs = yesterdayRecords.Count;

        _store.CostChangePercent = yesterdayCost > 0
            ? Math.Round((todayCost - yesterdayCost) / yesterdayCost * 100)
            : (todayCost > 0 ? 100 : 0);

        _store.TokenChangePercent = yesterdayTokens > 0
            ? Math.Round((decimal)(todayTokens - yesterdayTokens) / yesterdayTokens * 100)
            : (todayTokens > 0 ? 100 : 0);

        _store.RequestChangePercent = yesterdayReqs > 0
            ? Math.Round((decimal)(todayReqs - yesterdayReqs) / yesterdayReqs * 100)
            : (todayReqs > 0 ? 100 : 0);
    }

    private static void UpdateChangePercentagesForStore(UsageDataStore store)
    {
        var today = DateTime.Today;
        var yesterday = today.AddDays(-1);

        var todayRecords = store.Records.Where(r => r.Timestamp.Date == today).ToList();
        var yesterdayRecords = store.Records.Where(r => r.Timestamp.Date == yesterday).ToList();

        var todayCost = todayRecords.Sum(r => (decimal)r.Cost);
        var yesterdayCost = yesterdayRecords.Sum(r => (decimal)r.Cost);

        var todayTokens = todayRecords.Sum(r => (long)r.TotalTokens);
        var yesterdayTokens = yesterdayRecords.Sum(r => (long)r.TotalTokens);

        var todayReqs = todayRecords.Count;
        var yesterdayReqs = yesterdayRecords.Count;

        store.CostChangePercent = yesterdayCost > 0
            ? Math.Round((todayCost - yesterdayCost) / yesterdayCost * 100)
            : (todayCost > 0 ? 100 : 0);

        store.TokenChangePercent = yesterdayTokens > 0
            ? Math.Round((decimal)(todayTokens - yesterdayTokens) / yesterdayTokens * 100)
            : (todayTokens > 0 ? 100 : 0);

        store.RequestChangePercent = yesterdayReqs > 0
            ? Math.Round((decimal)(todayReqs - yesterdayReqs) / yesterdayReqs * 100)
            : (todayReqs > 0 ? 100 : 0);
    }
}
