namespace AiDesktopClient.Contracts;

// DTO сообщения для передачи туда-обратно; вложения - список AttachmentDto, TokenUsage опциональный - бэкенд может его не слать
public sealed class ChatMessageDto
{
    public required string Id { get; init; }
    public required string Role { get; init; }
    public required string Content { get; init; }
    public DateTime CreatedAt { get; init; }
    public TokenUsageDto? TokenUsage { get; init; }
    public double? ResponseTimeMs { get; init; }
    public CostInfoDto? CostInfo { get; init; }
    public List<AttachmentDto>? Attachments { get; init; }
}
