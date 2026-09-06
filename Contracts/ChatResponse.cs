namespace AiDesktopClient.Contracts;

public sealed class ChatResponse
{
    public required string ChatId { get; init; }
    public required string Content { get; init; }
    public required string ModelId { get; init; }
    public TokenUsageDto TokenUsage { get; init; } = new();
    public CostInfoDto CostInfo { get; init; } = new();
    public double ResponseTimeMs { get; init; }
}
