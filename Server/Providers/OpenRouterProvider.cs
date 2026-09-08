using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace AiBackend.Providers;

public sealed class OpenRouterProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    private readonly OpenRouterOptions _options;
    private readonly SemaphoreSlim _modelsLock = new(1, 1);
    private IReadOnlyList<AiModel>? _cachedModels;
    private DateTimeOffset _cacheExpiresAt;

    public OpenRouterProvider(HttpClient httpClient, IOptions<OpenRouterOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    public string Name => "OpenRouter";
    public bool IsConfigured => _options.Enabled && !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<IReadOnlyList<AiModel>> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return [];

        if (_cachedModels is not null && DateTimeOffset.UtcNow < _cacheExpiresAt)
            return _cachedModels;

        await _modelsLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedModels is not null && DateTimeOffset.UtcNow < _cacheExpiresAt)
                return _cachedModels;

            using var response = await _httpClient.GetAsync("models", cancellationToken);
            await EnsureSuccessAsync(response);
            var payload = await response.Content.ReadFromJsonAsync<OpenRouterModelsResponse>(cancellationToken: cancellationToken)
                ?? new OpenRouterModelsResponse();

            var models = payload.Data
                .Where(model => !string.IsNullOrWhiteSpace(model.Id))
                .Select(ToModel)
                .Where(model => model.IsFree)
                .Prepend(new AiModel("openrouter/free", "OpenRouter Free", Name, 0, 0, true, 0, true, "Automatic free-model routing"))
                .ToArray();

            _cachedModels = models;
            _cacheExpiresAt = DateTimeOffset.UtcNow.AddMinutes(Math.Max(1, _options.ModelsCacheMinutes));
            return models;
        }
        finally
        {
            _modelsLock.Release();
        }
    }

    public async IAsyncEnumerable<AiStreamEvent> StreamAsync(
        string modelId,
        string message,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            throw new AiProviderException(503, "OpenRouter is not configured.");

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = JsonContent.Create(new
            {
                model = modelId,
                messages = new[] { new { role = "user", content = message } },
                stream = true,
                stream_options = new { include_usage = true }
            })
        };
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await EnsureSuccessAsync(response);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                continue;

            var data = line[5..].Trim();
            if (data == "[DONE]")
                yield break;

            OpenRouterStreamResponse? item;
            try
            {
                item = JsonSerializer.Deserialize<OpenRouterStreamResponse>(data, JsonOptions);
            }
            catch (JsonException)
            {
                continue;
            }

            if (item is null)
                continue;

            var text = item.Choices.FirstOrDefault()?.Delta?.Content;
            if (!string.IsNullOrEmpty(text))
                yield return new AiStreamEvent(text, ModelId: item.Model);

            if (item.Usage is not null)
                yield return new AiStreamEvent(InputTokens: item.Usage.PromptTokens, OutputTokens: item.Usage.CompletionTokens, ModelId: item.Model);
        }
    }

    private static AiModel ToModel(OpenRouterModel model)
    {
        var input = ParsePrice(model.Pricing.Prompt);
        var output = ParsePrice(model.Pricing.Completion);
        return new AiModel(model.Id, string.IsNullOrWhiteSpace(model.Name) ? model.Id : model.Name, "OpenRouter", input, output, true,
            model.ContextLength, input == 0 && output == 0, model.Description);
    }

    private static decimal ParsePrice(string value) => decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var price)
        ? price * 1_000_000m
        : 0;

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var message = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "OpenRouter API key is invalid or missing.",
            HttpStatusCode.Forbidden => "OpenRouter access was forbidden for this request.",
            HttpStatusCode.TooManyRequests => "OpenRouter rate limit reached. Please try again later.",
            HttpStatusCode.BadRequest => "OpenRouter rejected the request.",
            HttpStatusCode.RequestTimeout => "OpenRouter request timed out. Please try again.",
            >= HttpStatusCode.InternalServerError => "OpenRouter is temporarily unavailable. Please try again later.",
            _ => "OpenRouter request failed."
        };
        throw new AiProviderException((int)response.StatusCode, message);
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
