using AiDesktopClient.Contracts;

namespace AiDesktopClient.Services;

// контракт проверки бэкенда: GetStatusAsync - полная сводка, TestConnectionAsync - дешёвая проба на всякий случай
public interface IBackendService
{
    Task<BackendStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<bool> TestConnectionAsync(string backendUrl, CancellationToken cancellationToken = default);
}
