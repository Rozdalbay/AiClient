using System.Collections.Concurrent;
using System.Diagnostics;
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

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = response.StatusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized => "OpenRouter API key is invalid or missing.",
                System.Net.HttpStatusCode.Forbidden => "OpenRouter access was forbidden for this request.",
                System.Net.HttpStatusCode.TooManyRequests => "OpenRouter rate limit reached. Please try again later.",
                System.Net.HttpStatusCode.BadRequest => "OpenRouter rejected the request.",
                System.Net.HttpStatusCode.RequestTimeout => "OpenRouter request timed out. Please try again.",
                >= System.Net.HttpStatusCode.InternalServerError => "OpenRouter is temporarily unavailable. Please try again later.",
                _ => "Backend request failed."
            };
            throw new HttpRequestException(errorMessage, null, response.StatusCode);
        }

        await using var stream =
            await response.Content.ReadAsStreamAsync(cancellationToken);

        using var reader = new StreamReader(stream);

        // чанки текста отдаём наружу as-yield возвращая, а usage складываем в словарь и в конце отдаём отдельным чанком - чтобы VM не гадала
        StreamUsage? capturedUsage = null;
        var eventCount = 0;
        var contentChunkCount = 0;
        var responseLength = 0;

        var parser = new SseEventParser();
        var buffer = new char[1024];
        while (true)
        {
            var count = await reader.ReadAsync(buffer.AsMemory(), cancellationToken);
            if (count == 0)
                break;

            foreach (var data in parser.Append(buffer.AsSpan(0, count)))
            {
                eventCount++;
                Debug.WriteLine($"SSE event received; length: {data.Length}");

                if (data == "[DONE]")
                    goto StreamCompleted;

                if (string.IsNullOrWhiteSpace(data))
                    continue;

                var sseEvent = JsonSerializer.Deserialize<SseEvent>(data, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (sseEvent is null)
                    continue;

                if (sseEvent.Error is not null)
                    throw new HttpRequestException(sseEvent.Error);

                if (sseEvent.Usage is not null)
                    capturedUsage = sseEvent.Usage;

                var text = sseEvent.Delta ?? sseEvent.Chunk ?? sseEvent.Choices?.FirstOrDefault()?.Delta?.Content;
                if (text is not null)
                {
                    contentChunkCount++;
                    responseLength += text.Length;
                    Debug.WriteLine($"Content chunk length: {text.Length}");
                    yield return new StreamChunk { Text = text };
                }
            }
        }

    StreamCompleted:
        Debug.WriteLine($"Assistant response length: {responseLength}; SSE events: {eventCount}; content chunks: {contentChunkCount}");
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
        public string? Chunk { get; init; }
        public List<SseChoice>? Choices { get; init; }
        public StreamUsage? Usage { get; init; }
        public string? Error { get; init; }
    }

    private sealed class SseChoice
    {
        public SseDelta? Delta { get; init; }
    }

    private sealed class SseDelta
    {
        public string? Content { get; init; }
    }
}
