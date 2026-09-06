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

    Console.WriteLine($"Получено сообщение: {request.Message}");
    Console.WriteLine($"Модель: {request.ModelId}");
    Console.WriteLine($"Чат: {request.ChatId}");

    string[] chunks =
    [
        "hi ",
        "see",
        "working ",
        "good."
    ];

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