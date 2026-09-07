using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

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

    var inputTokens = request.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length * 2;
    string[] chunks =
    [
        "hi ",
        "see",
        "working ",
        "good."
    ];

    var outputText = string.Concat(chunks);
    var outputTokens = outputText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length * 2;

    foreach (var chunk in chunks)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var json = JsonSerializer.Serialize(new
        {
            chunk
        });

        await response.WriteAsync(
            $"data: {json}\n\n",
            cancellationToken);

        await response.Body.FlushAsync(cancellationToken);

        await Task.Delay(500, cancellationToken);
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
