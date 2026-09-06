using AiDesktopClient.Models;

namespace AiDesktopClient.Services;

public interface IModelService
{
    Task<IReadOnlyList<ModelInfo>> GetModelsAsync(CancellationToken cancellationToken = default);
    Task<ModelInfo?> GetModelByIdAsync(string modelId, CancellationToken cancellationToken = default);
}
