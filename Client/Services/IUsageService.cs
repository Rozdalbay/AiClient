using AiDesktopClient.Models;

namespace AiDesktopClient.Services;

public interface IUsageService
{
    Task<UsagePeriodData> GetPeriodDataAsync(UsagePeriod period, CancellationToken cancellationToken = default);
    void RecordRequest(string requestId, string modelId, string modelName, int inputTokens, int outputTokens, double responseTimeMs);
    void Save();
}