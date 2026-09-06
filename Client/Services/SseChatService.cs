using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using AiDesktopClient.Models;

namespace AiDesktopClient.Services;

public sealed class SseChatService : IChatService
{
    private readonly HttpClient _httpClient;

    public SseChatService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async IAsyncEnumerable<string> StreamResponseAsync(
        string chatId,
        string modelId,
        string message,
        IReadOnlyList<Attachment>? attachments = null,
        [EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {
        // Это объект, который мы отправим бэку.
        var requestBody = new
        {
            chatId,
            modelId,
            message
        };

        // Пост.
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "stream");

        // Превращаем requestBody в JSON.
        request.Content = JsonContent.Create(requestBody);

        // Говорим серверу, что хотим получить SSE.
        request.Headers.Accept.ParseAdd("text/event-stream");

        // Отправляем запрос.
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        // Если сервер вернул 404, 500 и тому подобное то плачем и сжимаем кулаки от злости
        response.EnsureSuccessStatusCode();

        // Хватаем поток.
        await using var stream =
            await response.Content.ReadAsStreamAsync(cancellationToken);

        using var reader = new StreamReader(stream);

        // Читаем пока не опрокинут.
        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);

            // Вот тут если нас опрокинули.
            if (line is null)
                yield break;

            // Оно срёт говном
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Хаваем всё что дата.
            if (!line.StartsWith("data:"))
                continue;

            // Убираем дату и получаем содержимое события.
            var data = line["data:".Length..].TrimStart();

            // Сервер сообщил, что генерация закончена.
            if (data == "[DONE]")
                yield break;

            // Превращаем JSON в объект SseChunk.

            var sseChunk = JsonSerializer.Deserialize<SseChunk>(
                data,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (sseChunk?.Chunk is not null)
            {
                yield return sseChunk.Chunk;
            }
        }
    }

    public async Task<ChatResponse> SendMessageAsync(
        string chatId,
        string modelId,
        string message,
        IReadOnlyList<Attachment>? attachments = null,
        CancellationToken cancellationToken = default)
    {
        // гуишная залупенция, гуи хуи соси
        var content = new StringBuilder();

        await foreach (var chunk in StreamResponseAsync(
            chatId,
            modelId,
            message,
            attachments,
            cancellationToken))
        {
            content.Append(chunk);
        }

        return new ChatResponse
        {
            Content = content.ToString()
        };
    }

    private sealed class SseChunk
    {
        public string? Chunk { get; init; }
    }
}