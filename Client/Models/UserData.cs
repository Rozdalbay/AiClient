namespace AiDesktopClient.Models;

public sealed class UserData
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class SessionData
{
    public bool IsAuthenticated { get; set; }
    public string Username { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
    public DateTime CreatedAt { get; set; }
}
