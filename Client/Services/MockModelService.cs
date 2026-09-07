using AiDesktopClient.Models;

namespace AiDesktopClient.Services;

public sealed class MockModelService : IModelService
{
    private static readonly List<ModelInfo> MockModels =
    [
        new()
        {
            Id = "model-a",
            DisplayName = "GPT-5.6 Luna",
            Provider = "OpenAI",
            InputTokenPrice = 0.000003,
            OutputTokenPrice = 0.000015,
            IsAvailable = true,
            ContextWindow = 128000,
            Icon = "\uE99A"
        },
        new()
        {
            Id = "model-b",
            DisplayName = "GPT-6 Astra Fast",
            Provider = "OpenAI",
            InputTokenPrice = 0.000001,
            OutputTokenPrice = 0.000002,
            IsAvailable = true,
            ContextWindow = 64000,
            Icon = "\uE99B"
        },
        new()
        {
            Id = "model-c",
            DisplayName = "MiMo V2.5",
            Provider = "Xiaomi",
            InputTokenPrice = 0.000010,
            OutputTokenPrice = 0.000030,
            IsAvailable = true,
            ContextWindow = 32000,
            Icon = "\uE99C"
        },
        new()
        {
            Id = "model-d",
            DisplayName = "Claude Opus 4.5",
            Provider = "Anthropic",
            InputTokenPrice = 0.000005,
            OutputTokenPrice = 0.000010,
            IsAvailable = false,
            ContextWindow = 200000,
            Icon = "\uE99D"
        }
    ];

    public Task<IReadOnlyList<ModelInfo>> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<ModelInfo>>(MockModels.AsReadOnly());
    }

    public Task<ModelInfo?> GetModelByIdAsync(string modelId, CancellationToken cancellationToken = default)
    {
        var model = MockModels.FirstOrDefault(m => m.Id == modelId);
        return Task.FromResult(model);
    }
}
