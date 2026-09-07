using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AiDesktopClient.Services;

public enum AppTheme
{
    Dark,
    Light
}

// подменяет ResourceDictionary (DarkTheme/LightTheme) в рантайме с wipe-анимацией; настройка темы хранится в settings.json
public sealed class ThemeManager
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AiDesktopClient",
        "settings.json");

    private const string DarkThemeUri = "pack://application:,,,/Client/Themes/DarkTheme.xaml";
    private const string LightThemeUri = "pack://application:,,,/Client/Themes/LightTheme.xaml";

    private AppTheme _currentTheme = AppTheme.Dark;
    private System.Windows.Documents.Adorner? _currentAdorner;

    public AppTheme CurrentTheme => _currentTheme;

    public void ApplyInitialTheme()
    {
        var saved = LoadSavedTheme();
        _currentTheme = saved;
        ApplyThemeInternal(saved, false);
    }

    public void SwitchTheme(AppTheme newTheme)
    {
        if (newTheme == _currentTheme) return;
        _currentTheme = newTheme;
        SaveTheme(newTheme);
        ApplyThemeInternal(newTheme, true);
    }

    public void SetTheme(AppTheme theme)
    {
        _currentTheme = theme;
        SaveTheme(theme);
        ApplyThemeInternal(theme, false);
    }

    // убираем старый словарь, добавляем новый; при animate=true налегаем на шторку, чтобы глаза не болели
    private void ApplyThemeInternal(AppTheme theme, bool animate)
    {
        var app = Application.Current;
        var dicts = app.Resources.MergedDictionaries;

        var oldThemeDict = dicts.FirstOrDefault(d =>
            d.Source != null &&
            (d.Source.OriginalString.Contains("DarkTheme.xaml") || d.Source.OriginalString.Contains("LightTheme.xaml")));

        var newThemeDict = new ResourceDictionary
        {
            Source = new Uri(theme == AppTheme.Dark ? DarkThemeUri : LightThemeUri)
        };

        if (!animate || app.MainWindow is null)
        {
            if (oldThemeDict != null)
                dicts.Remove(oldThemeDict);
            dicts.Add(newThemeDict);
            return;
        }

        var oldBgBrush = oldThemeDict?.Contains("PrimaryBackgroundBrush") == true
            ? oldThemeDict["PrimaryBackgroundBrush"] as SolidColorBrush
            : null;
        var oldColor = oldBgBrush?.Color ?? Color.FromRgb(0x0D, 0x0F, 0x18);

        RunWipeAnimation(app.MainWindow, oldColor, newThemeDict, oldThemeDict);
    }

    private void RunWipeAnimation(
        Window window,
        Color oldColor,
        ResourceDictionary newThemeDict,
        ResourceDictionary? oldThemeDict)
    {
        var root = window.Content as FrameworkElement;
        if (root is null) return;

        var w = root.ActualWidth;
        var h = root.ActualHeight;
        if (w <= 0 || h <= 0) return;

        var adornerLayer = System.Windows.Documents.AdornerLayer.GetAdornerLayer(root);
        if (adornerLayer is null) return;

        var overlay = new Border
        {
            Background = new SolidColorBrush(oldColor),
            IsHitTestVisible = false
        };

        var clip = new RectangleGeometry(new Rect(0, 0, w, h));
        overlay.Clip = clip;

        var adorner = new ThemeWipeAdorner(root, overlay);
        adornerLayer.Add(adorner);
        _currentAdorner = adorner;

        var dicts = Application.Current.Resources.MergedDictionaries;
        if (oldThemeDict != null)
            dicts.Remove(oldThemeDict);
        dicts.Add(newThemeDict);

        var rectAnim = new RectAnimation(
            new Rect(0, 0, w, h),
            new Rect(w, 0, 0, h),
            new Duration(TimeSpan.FromMilliseconds(450)))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
        };

        rectAnim.Completed += (_, _) =>
        {
            try
            {
                adornerLayer.Remove(adorner);
                _currentAdorner = null;
            }
            catch { }
        };

        clip.BeginAnimation(RectangleGeometry.RectProperty, rectAnim);
    }

    private static AppTheme LoadSavedTheme()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var data = JsonSerializer.Deserialize<SettingsData>(json);
                if (data?.Theme == "Light")
                    return AppTheme.Light;
            }
        }
        catch { }
        return AppTheme.Dark;
    }

    private static void SaveTheme(AppTheme theme)
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath)!;
            Directory.CreateDirectory(dir);

            SettingsData data;
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                data = JsonSerializer.Deserialize<SettingsData>(json) ?? new SettingsData();
            }
            else
            {
                data = new SettingsData();
            }

            data.Theme = theme.ToString();
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(data, options));
        }
        catch { }
    }

    private class SettingsData
    {
        public string Theme { get; set; } = "Dark";
    }

    private sealed class ThemeWipeAdorner : System.Windows.Documents.Adorner
    {
        private readonly UIElement _child;

        public ThemeWipeAdorner(UIElement adornedElement, UIElement child)
            : base(adornedElement)
        {
            _child = child;
            IsHitTestVisible = false;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            _child.Measure(constraint);
            return constraint;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _child.Arrange(new Rect(finalSize));
            return finalSize;
        }

        protected override Visual GetVisualChild(int index) => _child as Visual ?? AdornedElement;
        protected override int VisualChildrenCount => 1;
    }
}
