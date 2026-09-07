using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using AiDesktopClient.Models;

namespace AiDesktopClient.Services;

// SSE-клиент к бэкенду: читает поток data: {...}\n\n, ловит delta текста и usage в конце; для ебланов: без этого ни информирования, ни денег
public sealed class SseChatService : IChatService
{
    private readonly HttpClient _httpClient;
    // словарик chatId -> usage на случай, если SendMessageAsync спросит после стрима; хранение временное
    private readonly ConcurrentDictionary<string, StreamUsage?> _usageResults = new();

    public SseChatService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async IAsyncEnumerable<StreamChunk> StreamResponseAsync(
        string chatId,
        string modelId,
        string message,
        IReadOnlyList<Attachment>? attachments = null,
        [EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            chatId,
            modelId,
            message
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "stream");

        request.Content = JsonContent.Create(requestBody);
        request.Headers.Accept.ParseAdd("text/event-stream");

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream =
            await response.Content.ReadAsStreamAsync(cancellationToken);

        using var reader = new StreamReader(stream);

        // чанки текста отдаём наружу as-yield возвращая, а usage складываем в словарь и в конце отдаём отдельным чанком - чтобы VM не гадала
        StreamUsage? capturedUsage = null;

        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);

            if (line is null)
                break;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (!line.StartsWith("data:"))
                continue;

            var data = line["data:".Length..].TrimStart();

            // маркер конца стрима; [DONE] - всё, шабаш; иначе распарсили JSON и раздали по карманам (delta → текст, usage → словарь)
            if (data == "[DONE]")
                break;

            var sseEvent = JsonSerializer.Deserialize<SseEvent>(
                data,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (sseEvent is null)
                continue;

            if (sseEvent.Usage is not null)
            {
                capturedUsage = sseEvent.Usage;
            }

            if (sseEvent.Delta is not null)
            {
                yield return new StreamChunk { Text = sseEvent.Delta };
            }
        }

        _usageResults[chatId] = capturedUsage;

        yield return new StreamChunk { Usage = capturedUsage };
    }

    public async Task<ChatResponse> SendMessageAsync(
        string chatId,
        string modelId,
        string message,
        IReadOnlyList<Attachment>? attachments = null,
        CancellationToken cancellationToken = default)
    {
        var content = new StringBuilder();

        await foreach (var chunk in StreamResponseAsync(
            chatId,
            modelId,
            message,
            attachments,
            cancellationToken))
        {
            if (chunk.Text is not null)
                content.Append(chunk.Text);
        }

        // не-стрим метод: собирает всё в кучу и забирает usage из словаря по chatId; не забудь TryRemove, иначе словарь раздуется как бюджет маркетинга
        var usage = _usageResults.TryRemove(chatId, out var u) ? u : null;

        return new ChatResponse
        {
            Content = content.ToString(),
            InputTokens = usage?.InputTokens ?? 0,
            OutputTokens = usage?.OutputTokens ?? 0
        };
    }

    private sealed class SseEvent
    {
        public string? Delta { get; init; }
        public StreamUsage? Usage { get; init; }
    }
}
