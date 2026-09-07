using System.Text.Json;
using OpenAI.Responses;
#pragma warning disable OPENAI001

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();



string key = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;
ResponsesClient client = new(key);

IAsyncEnumerable<StreamingResponseUpdate> GetOpenAiStream(string prompt)
{
    return client.CreateResponseStreamingAsync("gpt-5.6-luna", prompt);
}

app.MapPost("/stream", async (
    ChatRequest request,
    HttpResponse response,
    CancellationToken cancellationToken) =>
{
    response.ContentType = "text/event-stream";
    response.Headers.CacheControl = "no-cache";

    Console.WriteLine($"Received message: {request.Message}");
    Console.WriteLine($"Model: {request.ModelId}");
    Console.WriteLine($"Chat: {request.ChatId}");

    var stream = GetOpenAiStream(request.Message);

    await foreach (StreamingResponseUpdate rsp in stream)
    {
        if (rsp is StreamingResponseOutputTextDeltaUpdate delta)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var json = JsonSerializer.Serialize(new
            {
                delta.Delta
            });

            await response.WriteAsync(
                $"data: {json}\n\n",
                cancellationToken);

            await response.Body.FlushAsync(cancellationToken);

            await Task.Delay(500, cancellationToken);
            }

    }

    var usageEvent = JsonSerializer.Serialize(new
    {
        usage = new
        {
            input_tokens = inputTokens,
            output_tokens = outputTokens
        }
    });

    await response.WriteAsync(
        $"data: {usageEvent}\n\n",
        cancellationToken);

    await response.Body.FlushAsync(cancellationToken);

    await response.WriteAsync(
        "data: [DONE]\n\n",
        cancellationToken);

    await response.Body.FlushAsync(cancellationToken);
});

app.Run("http://localhost:5000");

internal sealed record ChatRequest(
    string ChatId,
    string ModelId,
    string Message);
