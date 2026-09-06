namespace AiDesktopClient.Contracts;

public sealed class ChatRequest
{
    public required string ChatId { get; init; }
    public required string ModelId { get; init; }
    public required string Message { get; init; }
    public List<AttachmentDto> Attachments { get; init; } = [];
    public string? SystemPrompt { get; init; }
}
