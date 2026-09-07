namespace AiDesktopClient.Models;

// юзер в локальном users.json: пароль только в виде PBKDF2-хэша, НИКОГДА не храни пароль в открытом виде, иначе получишь пизды от юзеров и от меня
public sealed class UserData
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// сохранённая сессия в session.json: IsAuthenticated + RememberMe решают, показывать ли MainWindow после сплэша
public sealed class SessionData
{
    public bool IsAuthenticated { get; set; }
    public string Username { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
    public DateTime CreatedAt { get; set; }
}
