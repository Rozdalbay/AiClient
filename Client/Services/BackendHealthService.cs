using System.Diagnostics;
using System.Net.Http;
using AiDesktopClient.Contracts;

namespace AiDesktopClient.Services;

public sealed class BackendHealthService : IBackendService
{
    private readonly HttpClient _httpClient;

    public BackendHealthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<BackendStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/");
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            sw.Stop();

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
    }

    public async Task<bool> TestConnectionAsync(string backendUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            using var testClient = new HttpClient { BaseAddress = new Uri(backendUrl), Timeout = TimeSpan.FromSeconds(5) };
            using var request = new HttpRequestMessage(HttpMethod.Get, "/");
            using var response = await testClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
