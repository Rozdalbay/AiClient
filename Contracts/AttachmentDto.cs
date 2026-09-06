namespace AiDesktopClient.Contracts;

public sealed class AttachmentDto
{
    public required string Id { get; init; }
    public required string FileName { get; init; }
    public long FileSize { get; init; }
    public string MimeType { get; init; } = string.Empty;
    public string? Base64Content { get; init; }
}
