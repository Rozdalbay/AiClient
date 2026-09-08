using AiBackend.Providers;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<OpenRouterOptions>(builder.Configuration.GetSection("OpenRouter"));
builder.Services.AddHttpClient("openrouter");
builder.Services.AddSingleton<OpenRouterProvider>(serviceProvider =>
    new OpenRouterProvider(
        serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("openrouter"),
        serviceProvider.GetRequiredService<IOptions<OpenRouterOptions>>()));
builder.Services.AddSingleton<OpenAiProvider>();

var app = builder.Build();
app.MapGet("/", () => Results.Ok(new { status = "ok", service = "AiBackend" }));
app.MapGet("/api/models", async (OpenRouterProvider router, OpenAiProvider openAi, CancellationToken ct) =>
{
    var models = new List<AiModel>();
    models.AddRange(await openAi.GetModelsAsync(ct));
    models.AddRange(await router.GetModelsAsync(ct));
    return Results.Ok(models);
});
app.MapPost("/stream", async (ChatRequest request, HttpResponse response, OpenRouterProvider router, OpenAiProvider openAi, CancellationToken ct) =>
{
    response.ContentType = "text/event-stream";
    response.Headers.CacheControl = "no-cache";
    IAiProvider provider = request.ModelId.Equals("openrouter/free", StringComparison.OrdinalIgnoreCase) || request.ModelId.Contains('/', StringComparison.Ordinal) ? router : openAi;
    try
    {
        await foreach (var item in provider.StreamAsync(request.ModelId, request.Message, ct))
        {
            if (item.Text is not null)
                await WriteEventAsync(response, new { delta = item.Text, model = item.ModelId }, ct);
            if (item.InputTokens is not null || item.OutputTokens is not null)
                await WriteEventAsync(response, new { usage = new { input_tokens = item.InputTokens, output_tokens = item.OutputTokens }, model = item.ModelId }, ct);
        }
        await response.WriteAsync("data: [DONE]\n\n", ct);
        await response.Body.FlushAsync(ct);
    }
    catch (AiProviderException ex)
    {
        if (!response.HasStarted) response.StatusCode = ex.StatusCode;
        else await WriteEventAsync(response, new { error = ex.Message }, CancellationToken.None);
    }
});
app.Run("http://localhost:5000");

static async Task WriteEventAsync(HttpResponse response, object payload, CancellationToken ct)
{
    await response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(payload)}\n\n", ct);
    await response.Body.FlushAsync(ct);
}

internal sealed record ChatRequest(string ChatId, string ModelId, string Message);
