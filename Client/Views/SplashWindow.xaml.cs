using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AiDesktopClient.Views;

public partial class SplashWindow : Window
{
    private static readonly Duration AnimDuration = new(TimeSpan.FromMilliseconds(450));

    public SplashWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public void SetStatus(string text)
    {
        StatusText.Text = text;
    }

    public void UpdateProgress(double fraction, string? status = null)
    {
        fraction = Math.Clamp(fraction, 0.0, 1.0);
        var targetWidth = Math.Max(0, ProgressFill.Parent is FrameworkElement p ? p.ActualWidth : 0) * fraction;

        var anim = new DoubleAnimation(
            ProgressFill.ActualWidth,
            targetWidth,
            new Duration(TimeSpan.FromMilliseconds(300)))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        ProgressFill.BeginAnimation(WidthProperty, anim);

        if (status is not null)
            SetStatus(status);
    }

    public void AnimateClose(Action? completed)
    {
        var fade = new DoubleAnimation(1.0, 0.0, new Duration(TimeSpan.FromMilliseconds(300)))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        fade.Completed += (_, _) =>
        {
            completed?.Invoke();
            Close();
        };
        RootBorder.BeginAnimation(OpacityProperty, fade);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var root = new DoubleAnimation(0.0, 1.0, AnimDuration)
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        RootBorder.BeginAnimation(OpacityProperty, root);

        var logoFade = new DoubleAnimation(0.0, 1.0, AnimDuration)
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        LogoContainer.BeginAnimation(OpacityProperty, logoFade);

        var logoScale = new DoubleAnimation(0.9, 1.0, AnimDuration)
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        LogoScaler.BeginAnimation(ScaleTransform.ScaleXProperty, logoScale);
        LogoScaler.BeginAnimation(ScaleTransform.ScaleYProperty, logoScale);

        BeginDelayedFade(TitleText, TimeSpan.FromMilliseconds(140));
        BeginDelayedFade(SubtitleText, TimeSpan.FromMilliseconds(240));
    }

    private static void BeginDelayedFade(FrameworkElement element, TimeSpan delay)
    {
        var fade = new DoubleAnimation(0.0, 1.0, AnimDuration)
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            BeginTime = delay
        };
        element.BeginAnimation(OpacityProperty, fade);
    }
}
