namespace AiDesktopClient.Contracts;

public sealed class ModelInfoDto
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required string Provider { get; init; }
    public double InputTokenPrice { get; init; }
    public double OutputTokenPrice { get; init; }
    public bool IsAvailable { get; init; }
    public int ContextWindow { get; init; }
}
