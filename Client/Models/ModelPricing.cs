using System.Collections.Frozen;

namespace AiDesktopClient.Models;

public sealed class ModelPricing
{
    public required string ModelId { get; init; }
    public required string DisplayName { get; init; }
    public double InputPricePerMillion { get; init; }
    public double OutputPricePerMillion { get; init; }
}

public static class PricingCatalog
{
    private static readonly FrozenDictionary<string, ModelPricing> PricingMap;

    static PricingCatalog()
    {
        var entries = new Dictionary<string, ModelPricing>
        {
            ["model-a"] = new ModelPricing
            {
                ModelId = "model-a",
                DisplayName = "Model A",
                InputPricePerMillion = 3.0,
                OutputPricePerMillion = 15.0
            },
            ["model-b"] = new ModelPricing
            {
                ModelId = "model-b",
                DisplayName = "Model B",
                InputPricePerMillion = 1.0,
                OutputPricePerMillion = 2.0
            },
            ["model-c"] = new ModelPricing
            {
                ModelId = "model-c",
                DisplayName = "Model C",
                InputPricePerMillion = 10.0,
                OutputPricePerMillion = 30.0
            },
            ["model-d"] = new ModelPricing
            {
                ModelId = "model-d",
                DisplayName = "Model D",
                InputPricePerMillion = 5.0,
                OutputPricePerMillion = 10.0
            }
        };

        PricingMap = entries.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    public static ModelPricing? GetPricing(string modelId)
    {
        return PricingMap.TryGetValue(modelId, out var pricing) ? pricing : null;
    }

    public static double CalculateCost(string modelId, int inputTokens, int outputTokens)
    {
        var pricing = GetPricing(modelId);
        if (pricing is null) return -1;

        var inputCost = inputTokens / 1_000_000.0 * pricing.InputPricePerMillion;
        var outputCost = outputTokens / 1_000_000.0 * pricing.OutputPricePerMillion;
        return inputCost + outputCost;
    }

    public static IReadOnlyList<ModelPricing> GetAllPricing()
    {
        return PricingMap.Values.ToList().AsReadOnly();
    }
}
