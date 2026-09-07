using System.Text.Json;
using OpenAI.Responses;
#pragma warning disable OPENAI001

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// ЕДИНСТВЕННЫЙ бэкенд-фасад: проксируем промпт в OpenAI и раздаём SSE, никаких баз и прочей хуйни

// ключ придёт только из ENV OPENAI_API_KEY; нет переменной - сервер подохнет ещё до первого запроса, так и задумано
string key = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;
ResponsesClient client = new(key);

// модель захардкожена, потому что похуй: настоящий роутинг по modelId появится в 2099 году, пока все ходят на одну и ту же
IAsyncEnumerable<StreamingResponseUpdate> GetOpenAiStream(string prompt)
{
    return client.CreateResponseStreamingAsync("gpt-5.6-luna", prompt);
}

// SSE-эндпоинт для чата: клиент долбит POST /stream и жрёт поток events через text/event-stream
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

    // сюда складываем токены из финального события; не соберёшь - юзеру насчитают по нулям и аналитика заплачет
    int inputTokens = 0;
    int outputTokens = 0;

    await foreach (StreamingResponseUpdate rsp in stream)
    {
        // ДЛЯ ЕБЛАНА НА БУДУЩЕЕ: usage приходит ТОЛЬКО в финальном событии CompletedUpdate.Response.Usage (InputTokenCount/OutputTokenCount), не ищи его в дельтах
        if (rsp is StreamingResponseCompletedUpdate completed)
        {
            var usage = completed.Response.Usage;
            if (usage is not null)
            {
                inputTokens = usage.InputTokenCount;
                outputTokens = usage.OutputTokenCount;
            }
        }
        // каждая дельта текста шлётся отдельным SSE-событием data: {...}\n\n, без этого клиент не покажет эффект печатания
        else if (rsp is StreamingResponseOutputTextDeltaUpdate delta)
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

            // да, тут 500мс пауза - дешёвый тайпинг-эффект, на проде можно выпилить, но наше демо должно выглядеть живым
            await Task.Delay(500, cancellationToken);
            }

    }

    // финальный usage-ивент: клиент считает по нему деньги для сервиса Usage, УДАЛИШЬ - затраты обнулятся и тебя попросят всё переписать
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

    // маркер конца стрима; без него клиент будет ждать сообщение вечность - прямо как та сплэшка, которую чинили вчера
    await response.WriteAsync(
        "data: [DONE]\n\n",
        cancellationToken);

    await response.Body.FlushAsync(cancellationToken);
});

// слушаем http://localhost:5000; сдвинешь порт - клиент (BaseAddress 127.0.0.1:5000) сдохнет, и ты будешь долго искать не тот баг
app.Run("http://localhost:5000");

// DTO от фронта: chatId/modelId/message; не добавляй обязательные поля без нужды - старый клиент сразу словит разъеб в десериализации
internal sealed record ChatRequest(
    string ChatId,
    string ModelId,
    string Message);
