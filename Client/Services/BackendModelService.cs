using System.Net.Http;
using System.Net.Http.Json;
using AiDesktopClient.Models;

namespace AiDesktopClient.Services;

public sealed class BackendModelService : IModelService
{
    private readonly HttpClient _httpClient;
    private IReadOnlyList<ModelInfo>? _models;

    public BackendModelService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<ModelInfo>> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var models = await _httpClient.GetFromJsonAsync<List<BackendModelDto>>("api/models", cancellationToken) ?? [];
            _models = models.Select(ToModel).ToArray();
        }
        catch (HttpRequestException)
        {
            _models ??= [];
        }

        return _models;
    }

    public async Task<ModelInfo?> GetModelByIdAsync(string modelId, CancellationToken cancellationToken = default)
    {
        var models = _models ?? await GetModelsAsync(cancellationToken);
        return models.FirstOrDefault(model => model.Id.Equals(modelId, StringComparison.OrdinalIgnoreCase));
    }

    private static ModelInfo ToModel(BackendModelDto model) => new()
    {
        Id = model.Id,
        DisplayName = model.DisplayName,
        Provider = model.Provider,
        InputTokenPrice = (double)model.InputTokenPrice / 1_000_000,
        OutputTokenPrice = (double)model.OutputTokenPrice / 1_000_000,
        IsAvailable = model.IsAvailable,
        ContextWindow = model.ContextWindow,
        IsFree = model.IsFree,
        Description = model.Description,
        Icon = model.Provider.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase) ? "\uE99A" : "\uE99A"
    };

    private sealed class BackendModelDto
    {
        public string Id { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string Provider { get; init; } = string.Empty;
        public decimal InputTokenPrice { get; init; }
        public decimal OutputTokenPrice { get; init; }
        public bool IsAvailable { get; init; }
        public int ContextWindow { get; init; }
        public bool IsFree { get; init; }
        public string? Description { get; init; }
    }
}
