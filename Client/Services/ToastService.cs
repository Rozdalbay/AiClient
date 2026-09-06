using System.Windows;
using System.Windows.Threading;

namespace AiDesktopClient.Services;

public enum ToastType
{
    Success,
    Error,
    Warning,
    Info
}

public sealed class ToastEventArgs : EventArgs
{
    public string Message { get; init; } = string.Empty;
    public ToastType Type { get; init; }
    public int DurationMs { get; init; } = 3000;
}

public interface IToastService
{
    event EventHandler<ToastEventArgs>? ToastRequested;
    void ShowSuccess(string message, int durationMs = 3000);
    void ShowError(string message, int durationMs = 4000);
    void ShowWarning(string message, int durationMs = 3500);
    void ShowInfo(string message, int durationMs = 3000);
}

public sealed class ToastService : IToastService
{
    private readonly Dispatcher _dispatcher;

    public ToastService()
    {
        _dispatcher = Application.Current.Dispatcher;
    }

    public event EventHandler<ToastEventArgs>? ToastRequested;

    public void ShowSuccess(string message, int durationMs = 3000)
        => RaiseToast(message, ToastType.Success, durationMs);

    public void ShowError(string message, int durationMs = 4000)
        => RaiseToast(message, ToastType.Error, durationMs);

    public void ShowWarning(string message, int durationMs = 3500)
        => RaiseToast(message, ToastType.Warning, durationMs);

    public void ShowInfo(string message, int durationMs = 3000)
        => RaiseToast(message, ToastType.Info, durationMs);

    private void RaiseToast(string message, ToastType type, int durationMs)
    {
        var args = new ToastEventArgs { Message = message, Type = type, DurationMs = durationMs };
        if (_dispatcher.CheckAccess())
            ToastRequested?.Invoke(this, args);
        else
            _dispatcher.Invoke(() => ToastRequested?.Invoke(this, args));
    }
}
