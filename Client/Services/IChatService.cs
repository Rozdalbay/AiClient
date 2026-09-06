using AiDesktopClient.Models;

namespace AiDesktopClient.Services;

public interface IChatService
{
    IAsyncEnumerable<string> StreamResponseAsync(
        string chatId,
        string modelId,
        string message,
        IReadOnlyList<Attachment>? attachments = null,
        CancellationToken cancellationToken = default);

    Task<ChatResponse> SendMessageAsync(
        string chatId,
        string modelId,
        string message,
        IReadOnlyList<Attachment>? attachments = null,
        CancellationToken cancellationToken = default);
}

public sealed class ChatResponse
{
    public string Content { get; init; } = string.Empty;
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public double ResponseTimeMs { get; init; }
    public decimal Cost { get; init; }
}
