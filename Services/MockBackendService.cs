using AiDesktopClient.Contracts;

namespace AiDesktopClient.Services;

public sealed class MockBackendService : IBackendService
{
    private readonly Random _random = new();
    private bool _isConnected = true;

    public Task<BackendStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        _isConnected = _random.Next(100) > 5;

        var status = new BackendStatusDto
        {
            Status = _isConnected ? BackendStatus.Connected : BackendStatus.Disconnected,
            LatencyMs = _random.Next(50, 300),
            LastChecked = DateTime.Now,
            BackendVersion = "1.0.0-mock",
            ErrorMessage = _isConnected ? null : "Backend is unreachable"
        };

        return Task.FromResult(status);
    }

    public Task<bool> TestConnectionAsync(string backendUrl, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_random.Next(100) > 10);
    }
}
