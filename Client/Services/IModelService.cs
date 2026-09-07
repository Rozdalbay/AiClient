using AiDesktopClient.Models;

namespace AiDesktopClient.Services;

// контракт каталога моделей; пока реализуется только моком, есть куда расти до настоящего API
public interface IModelService
{
    Task<IReadOnlyList<ModelInfo>> GetModelsAsync(CancellationToken cancellationToken = default);
    Task<ModelInfo?> GetModelByIdAsync(string modelId, CancellationToken cancellationToken = default);
}
