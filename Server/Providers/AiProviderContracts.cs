using System.Text.Json.Serialization;

namespace AiBackend.Providers;

public sealed record AiModel(
    string Id,
    string DisplayName,
    string Provider,
    decimal InputTokenPrice,
    decimal OutputTokenPrice,
    bool IsAvailable,
    int ContextWindow,
    bool IsFree,
    string? Description = null);

public sealed record AiStreamEvent(
    string? Text = null,
    int? InputTokens = null,
    int? OutputTokens = null,
    string? ModelId = null);

public interface IAiProvider
{
    string Name { get; }
    bool IsConfigured { get; }
    Task<IReadOnlyList<AiModel>> GetModelsAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<AiStreamEvent> StreamAsync(
        string modelId,
        string message,
        CancellationToken cancellationToken = default);
}

public sealed class AiProviderException : Exception
{
    public int StatusCode { get; }

    public AiProviderException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}

public sealed class OpenRouterOptions
{
    public bool Enabled { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";
    public int ModelsCacheMinutes { get; set; } = 15;
}

internal sealed class OpenRouterModelsResponse
{
    public List<OpenRouterModel> Data { get; set; } = [];
}

internal sealed class OpenRouterModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ContextLength { get; set; }
    public OpenRouterPricing Pricing { get; set; } = new();
}

internal sealed class OpenRouterPricing
{
    public string Prompt { get; set; } = string.Empty;
    public string Completion { get; set; } = string.Empty;
}

internal sealed class OpenRouterStreamResponse
{
    public string? Model { get; set; }
    public List<OpenRouterChoice> Choices { get; set; } = [];
    public OpenRouterUsage? Usage { get; set; }
}

internal sealed class OpenRouterChoice
{
    public OpenRouterDelta? Delta { get; set; }
}

internal sealed class OpenRouterDelta
{
    public string? Content { get; set; }
}

internal sealed class OpenRouterUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }
}
