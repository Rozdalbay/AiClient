namespace AiDesktopClient.Contracts;

/// Backend owns token accounting — frontend only displays what the backend reports.
public sealed class TokenUsageDto
{
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public int TotalTokens => InputTokens + OutputTokens;
}
