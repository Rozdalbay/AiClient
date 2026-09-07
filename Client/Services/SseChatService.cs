using System.Collections.Concurrent;
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
