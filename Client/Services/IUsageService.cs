using AiDesktopClient.Models;

namespace AiDesktopClient.Services;

public interface IUsageService
{
    Task<UsageInfo> GetUsageAsync(DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ModelUsageStat>> GetModelStatsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DailyCostPoint>> GetDailyCostsAsync(int days = 7, CancellationToken cancellationToken = default);
}
