using System.Diagnostics;
using System.Net.Http;
using AiDesktopClient.Contracts;

namespace AiDesktopClient.Services;

// здоровье бэкенда: GET / раз в 5 секунд (см. MainViewModel), мерим пинг и отдаём статус-сводку; если бросить путь - бросит исключение, тут всё обработано
public sealed class BackendHealthService : IBackendService
{
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _requestLock = new(1, 1);

    public BackendHealthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // статус с замером времени через Stopwatch; умное условие: 4хх тоже считаем "подключем" потому что сервер жив, просто злой
    public async Task<BackendStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        await _requestLock.WaitAsync(cancellationToken);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/");
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            sw.Stop();
            Debug.WriteLine($"[ConnectionStatus] URL={_httpClient.BaseAddress} Endpoint=/ StatusCode={(int)response.StatusCode}");

            return new BackendStatusDto
            {
                Status = response.IsSuccessStatusCode || (int)response.StatusCode >= 400
                    ? BackendStatus.Connected
                    : BackendStatus.Error,
                LatencyMs = sw.Elapsed.TotalMilliseconds,
                LastChecked = DateTime.Now,
                BackendVersion = response.Headers.Server?.ToString() ?? "unknown",
                ErrorMessage = null
            };
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            return new BackendStatusDto
            {
                Status = BackendStatus.Connecting,
                LatencyMs = sw.Elapsed.TotalMilliseconds,
                LastChecked = DateTime.Now,
                ErrorMessage = "Timeout"
            };
        }
        catch
        {
            sw.Stop();
            return new BackendStatusDto
            {
                Status = BackendStatus.Disconnected,
                LatencyMs = sw.Elapsed.TotalMilliseconds,
                LastChecked = DateTime.Now,
                ErrorMessage = "Backend is unreachable"
            };
        }
        finally
        {
            _requestLock.Release();
        }
    }

    public async Task<bool> TestConnectionAsync(string backendUrl, CancellationToken cancellationToken = default)
    {
        var backendUri = CreateBackendUri(backendUrl);
        await _requestLock.WaitAsync(cancellationToken);
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(backendUri, "/"));
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            Debug.WriteLine($"[TestConnection] URL={backendUri} Endpoint=/ StatusCode={(int)response.StatusCode}");

            return response.IsSuccessStatusCode || (int)response.StatusCode >= 400;
        }
        catch
        {
            return false;
        }
        finally
        {
            _requestLock.Release();
        }
    }

    private static Uri CreateBackendUri(string backendUrl)
    {
        var value = backendUrl.Trim();
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            uri = new Uri($"http://{value}", UriKind.Absolute);
        }

        return new UriBuilder(uri) { Path = uri.AbsolutePath.EndsWith('/') ? uri.AbsolutePath : uri.AbsolutePath + '/' }.Uri;
    }
}
