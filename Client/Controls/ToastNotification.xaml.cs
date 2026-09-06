using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using AiDesktopClient.Services;

namespace AiDesktopClient.Controls;

public partial class ToastNotification : UserControl
{
    private System.Windows.Threading.DispatcherTimer? _timer;

    public ToastNotification()
    {
        InitializeComponent();
    }

    public void Show(string message, ToastType type, int durationMs = 3000)
    {
        MessageText.Text = message;

        switch (type)
        {
            case ToastType.Success:
                IconText.Text = "\u2713";
                IconText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5CFCB0"));
                break;
            case ToastType.Error:
                IconText.Text = "\u2717";
                IconText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FC5C7C"));
                break;
            case ToastType.Warning:
                IconText.Text = "\u26A0";
                IconText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCE55C"));
                break;
            case ToastType.Info:
                IconText.Text = "\u2139";
                IconText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5CD4FC"));
                break;
        }

        Visibility = Visibility.Visible;

        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250));
        var slideIn = new DoubleAnimation(-20, 0, TimeSpan.FromMilliseconds(300));
        slideIn.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };

        BeginAnimation(OpacityProperty, fadeIn);
        Translate.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slideIn);

        _timer?.Stop();
        _timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(durationMs)
        };
        _timer.Tick += (s, e) =>
        {
            _timer.Stop();
            Hide();
        };
        _timer.Start();
    }

    public void Hide()
    {
        var fadeOut = new DoubleAnimation(0, TimeSpan.FromMilliseconds(250));
        var slideOut = new DoubleAnimation(-20, TimeSpan.FromMilliseconds(200));

        fadeOut.Completed += (s, e) => Visibility = Visibility.Collapsed;

        BeginAnimation(OpacityProperty, fadeOut);
        Translate.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slideOut);
    }
}
