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

    public void RecordRequest(string requestId, string modelId, string modelName, int inputTokens, int outputTokens, double responseTimeMs)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            requestId = Guid.NewGuid().ToString("N");

        var totalTokens = inputTokens + outputTokens;
        var pricing = PricingCatalog.GetPricing(modelId);
        var cost = pricing is not null
            ? inputTokens / 1_000_000.0 * pricing.InputPricePerMillion + outputTokens / 1_000_000.0 * pricing.OutputPricePerMillion
            : 0.0;

        var record = new UsageRecord
        {
            RequestId = requestId,
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
            if (_store.Records.Any(r => r.RequestId == requestId))
                return;

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

    public Task<UsagePeriodData> GetPeriodDataAsync(UsagePeriod period, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var now = DateTime.Now;
            var periodTo = now.Date.AddDays(1);
            DateTime periodFrom;
            int maxBuckets;

            switch (period)
            {
                case UsagePeriod.Today:
                    periodFrom = now.Date;
                    maxBuckets = 1;
                    break;
                case UsagePeriod.Week:
                    periodFrom = now.Date.AddDays(-6);
                    maxBuckets = 7;
                    break;
                case UsagePeriod.Month:
                    periodFrom = now.Date.AddDays(-29);
                    maxBuckets = 30;
                    break;
                default:
                    periodFrom = _store.Records.Count > 0
                        ? _store.Records.Min(r => r.Timestamp.Date)
                        : now.Date;
                    maxBuckets = 90;
                    break;
            }

            var currentRecords = _store.Records
                .Where(r => r.Timestamp >= periodFrom && r.Timestamp < periodTo)
                .ToList();

            var previousFrom = period == UsagePeriod.AllTime
                ? periodFrom
                : periodFrom - (periodTo - periodFrom);

            var previousRecords = period == UsagePeriod.AllTime
                ? []
                : _store.Records
                    .Where(r => r.Timestamp >= previousFrom && r.Timestamp < periodFrom)
                    .ToList();

            var cost = currentRecords.Sum(r => (decimal)r.Cost);
            var tokens = currentRecords.Sum(r => (long)r.TotalTokens);
            var requests = currentRecords.Count;

            var previousCost = previousRecords.Sum(r => (decimal)r.Cost);
            var previousTokens = previousRecords.Sum(r => (long)r.TotalTokens);
            var previousRequests = previousRecords.Count;

            decimal? costChange = previousCost > 0
                ? Math.Round((cost - previousCost) / previousCost * 100, 1)
                : cost > 0 ? 100m : null;

            decimal? tokenChange = previousTokens > 0
                ? Math.Round((decimal)(tokens - previousTokens) / previousTokens * 100, 1)
                : tokens > 0 ? 100m : null;

            decimal? requestChange = previousRequests > 0
                ? Math.Round((decimal)(requests - previousRequests) / previousRequests * 100, 1)
                : requests > 0 ? 100m : null;

            var stats = BuildModelStats(currentRecords);
            var dailyCosts = BuildDailyPoints(periodFrom, periodTo, maxBuckets);

            return Task.FromResult(new UsagePeriodData
            {
                Period = period,
                Cost = cost,
                Tokens = tokens,
                Requests = requests,
                BudgetLimit = _store.BudgetLimit,
                CostChangePercent = costChange,
                TokenChangePercent = tokenChange,
                RequestChangePercent = requestChange,
                HasAnyUsage = _store.Records.Count > 0,
                ModelStats = stats.AsReadOnly(),
                DailyCosts = dailyCosts.AsReadOnly()
            });
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

    private List<ModelUsageStat> BuildModelStats(List<UsageRecord> records)
    {
        var modelGroups = records
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

        var totalModelCost = modelGroups.Sum(m => m.TotalCost);
        var stats = new List<ModelUsageStat>();

        for (var i = 0; i < modelGroups.Count; i++)
        {
            var m = modelGroups[i];
            stats.Add(new ModelUsageStat
            {
                ModelId = m.ModelId,
                ModelName = m.ModelName,
                Cost = m.TotalCost,
                Percentage = totalModelCost > 0 ? (double)(m.TotalCost / totalModelCost * 100) : 0,
                RequestCount = m.RequestCount,
                TotalTokens = m.TotalTokens,
                Color = ModelColors[i % ModelColors.Length]
            });
        }

        return stats;
    }

    private List<DailyCostPoint> BuildDailyPoints(DateTime from, DateTime to, int maxBuckets)
    {
        var points = new List<DailyCostPoint>();
        var spanDays = Math.Max(1, (int)((to - from).TotalDays));
        var bucketSize = Math.Max(1, (int)Math.Ceiling(spanDays / (double)Math.Max(1, maxBuckets)));

        var start = from;
        while (start < to)
        {
            var end = start.AddDays(bucketSize);
            if (end > to) end = to;

            var sum = _store.Records
                .Where(r => r.Timestamp >= start && r.Timestamp < end)
                .Sum(r => (decimal)r.Cost);

            points.Add(new DailyCostPoint
            {
                Date = start,
                Cost = sum
            });
            start = end;
        }

        return points;
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
        UpdateChangePercentagesForStore(_store);
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