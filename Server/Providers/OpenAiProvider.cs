using OpenAI.Responses;
#pragma warning disable OPENAI001

namespace AiBackend.Providers;

public sealed class OpenAiProvider : IAiProvider
{
    private readonly ResponsesClient? _client;
    private readonly string _defaultModel;

    public OpenAiProvider(IConfiguration configuration)
    {
        var key = configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        _defaultModel = configuration["OpenAI:DefaultModel"] ?? "gpt-5.6-luna";
        if (!string.IsNullOrWhiteSpace(key))
            _client = new ResponsesClient(key);
    }

    public string Name => "OpenAI";
    public bool IsConfigured => _client is not null;

    public Task<IReadOnlyList<AiModel>> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AiModel> models = IsConfigured
            ? [new AiModel(_defaultModel, _defaultModel, Name, 0, 0, true, 0, false)]
            : [];
        return Task.FromResult(models);
    }

    public async IAsyncEnumerable<AiStreamEvent> StreamAsync(
        string modelId,
        string message,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_client is null)
            throw new AiProviderException(503, "OpenAI is not configured.");

        await foreach (var update in _client.CreateResponseStreamingAsync(
            string.IsNullOrWhiteSpace(modelId) ? _defaultModel : modelId,
            message).WithCancellation(cancellationToken))
        {
            if (update is StreamingResponseOutputTextDeltaUpdate delta)
                yield return new AiStreamEvent(delta.Delta);
            else if (update is StreamingResponseCompletedUpdate completed && completed.Response.Usage is { } usage)
                yield return new AiStreamEvent(InputTokens: usage.InputTokenCount, OutputTokens: usage.OutputTokenCount);
        }
    }
}
