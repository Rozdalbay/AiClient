using AiDesktopClient.Contracts;

namespace AiDesktopClient.Services;

public interface IBackendService
{
    Task<BackendStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<bool> TestConnectionAsync(string backendUrl, CancellationToken cancellationToken = default);
}
