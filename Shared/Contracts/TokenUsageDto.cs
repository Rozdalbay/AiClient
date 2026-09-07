namespace AiDesktopClient.Contracts;

// для еблана: токены считает БЭКЕНД, фронт только красиво показывает - не перекладывай эту работу на клиент, у него и так дел по горло
public sealed class TokenUsageDto
{
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public int TotalTokens => InputTokens + OutputTokens;
}
