namespace AiDesktopClient.Contracts;

// файл в запросе/ответе; Base64Content тащим только когда нужен реальный контент - иначе сервер захлебнётся байтами
public sealed class AttachmentDto
{
    public required string Id { get; init; }
    public required string FileName { get; init; }
    public long FileSize { get; init; }
    public string MimeType { get; init; } = string.Empty;
    public string? Base64Content { get; init; }
}
