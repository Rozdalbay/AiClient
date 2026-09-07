using AiDesktopClient.Models;

namespace AiDesktopClient.Services;

// контракт учёта использования: GetPeriodDataAsync отдаёт агрегат за период, RecordRequest пишет ОДИН запрос (идемпотентно по requestId), Save люнит JSON на диск
public interface IUsageService
{
    Task<UsagePeriodData> GetPeriodDataAsync(UsagePeriod period, CancellationToken cancellationToken = default);
    void RecordRequest(string requestId, string modelId, string modelName, int inputTokens, int outputTokens, double responseTimeMs);
    void Save();
}