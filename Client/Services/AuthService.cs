using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AiDesktopClient.Models;

namespace AiDesktopClient.Services;

public sealed class AuthService
{
    private static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AiDesktopClient");

    private static readonly string UsersPath = Path.Combine(DataDir, "users.json");
    private static readonly string SessionPath = Path.Combine(DataDir, "session.json");

    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    private List<UserData> _users = [];

    public AuthService()
    {
        Directory.CreateDirectory(DataDir);
        LoadUsers();
    }

    public bool HasUsers => _users.Count > 0;

    public bool UserExists(string username)
    {
        return _users.Any(u =>
            string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
    }

    public bool Register(string username, string password, out string error)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
        {
            error = "Username must be at least 3 characters";
            return false;
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            error = "Password must be at least 6 characters";
            return false;
        }

        if (UserExists(username))
        {
            error = "Username already exists";
            return false;
        }

        var salt = GenerateSalt();
        var hash = HashPassword(password, salt);

        _users.Add(new UserData
        {
            Username = username,
            PasswordHash = Convert.ToBase64String(hash),
            Salt = Convert.ToBase64String(salt),
            CreatedAt = DateTime.UtcNow
        });

        SaveUsers();
        error = string.Empty;
        return true;
    }

    public bool Login(string username, string password, out string error)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            error = "Please enter both username and password";
            return false;
        }

        var user = _users.FirstOrDefault(u =>
            string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));

        if (user is null)
        {
            error = "Invalid username or password";
            return false;
        }

        var salt = Convert.FromBase64String(user.Salt);
        var hash = HashPassword(password, salt);
        var storedHash = Convert.FromBase64String(user.PasswordHash);

        if (!CryptographicOperations.FixedTimeEquals(hash, storedHash))
        {
            error = "Invalid username or password";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public void SaveSession(string username, bool rememberMe)
    {
        var session = new SessionData
        {
            IsAuthenticated = true,
            Username = username,
            RememberMe = rememberMe,
            CreatedAt = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SessionPath, json);
    }

    public SessionData? LoadSession()
    {
        try
        {
            if (File.Exists(SessionPath))
            {
                var json = File.ReadAllText(SessionPath);
                var session = JsonSerializer.Deserialize<SessionData>(json);
                if (session is { IsAuthenticated: true })
                    return session;
            }
        }
        catch { }
        return null;
    }

    public void ClearSession()
    {
        try
        {
            if (File.Exists(SessionPath))
                File.Delete(SessionPath);
        }
        catch { }
    }

    private void LoadUsers()
    {
        try
        {
            if (File.Exists(UsersPath))
            {
                var json = File.ReadAllText(UsersPath);
                _users = JsonSerializer.Deserialize<List<UserData>>(json) ?? [];
            }
        }
        catch
        {
            _users = [];
        }
    }

    private void SaveUsers()
    {
        try
        {
            var json = JsonSerializer.Serialize(_users, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(UsersPath, json);
        }
        catch { }
    }

    private static byte[] GenerateSalt()
    {
        var salt = new byte[SaltSize];
        RandomNumberGenerator.Fill(salt);
        return salt;
    }

    private static byte[] HashPassword(string password, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            Algorithm,
            HashSize);
    }
}
