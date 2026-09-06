namespace AiDesktopClient.Contracts;

public enum BackendStatus
{
    Connected,
    Connecting,
    Disconnected,
    Error
}

public sealed class BackendStatusDto
{
    public BackendStatus Status { get; init; }
    public double LatencyMs { get; init; }
    public DateTime LastChecked { get; init; }
    public string? ErrorMessage { get; init; }
    public string BackendVersion { get; init; } = string.Empty;
}
