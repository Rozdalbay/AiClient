using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using AiDesktopClient.Models;
using AiDesktopClient.ViewModels;

namespace AiDesktopClient.Views;

public partial class UsagePanel : UserControl
{
    private UsageViewModel? _viewModel;

    public UsagePanel()
    {
        InitializeComponent();
        DataContextChanged += UsagePanel_DataContextChanged;
        Loaded += UsagePanel_Loaded;
        SizeChanged += (_, _) => Redraw();
    }

    private void UsagePanel_Loaded(object sender, RoutedEventArgs e)
    {
        AttachViewModel();
        Redraw();
    }

    private void PeriodToggle_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        PeriodPopup.IsOpen = !PeriodPopup.IsOpen;
    }

    private void PeriodItem_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel is null || sender is not Button button)
            return;

        var period = (string)button.Tag switch
        {
            "7 days" => UsagePeriod.Week,
            "30 days" => UsagePeriod.Month,
            "All time" => UsagePeriod.AllTime,
            _ => UsagePeriod.Today
        };

        _viewModel.SelectedPeriod = period;
        PeriodPopup.IsOpen = false;
    }

    private void UsagePanel_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        AttachViewModel();
        if (IsLoaded)
            Redraw();
    }

    private void AttachViewModel()
    {
        if (_viewModel is not null)
            _viewModel.DataRefreshed -= OnDataRefreshed;

        _viewModel = DataContext as UsageViewModel;

        if (_viewModel is not null)
            _viewModel.DataRefreshed += OnDataRefreshed;
    }

    private void OnDataRefreshed(object? sender, EventArgs e)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.InvokeAsync(Redraw);
            return;
        }
        Redraw();
    }

    private void Redraw()
    {
        if (_viewModel is null || !IsLoaded)
            return;

        var data = _viewModel.Data;
        var points = data?.DailyCosts ?? [];
        var hasData = data?.HasAnyUsage == true && points.Count > 0;

        GraphCanvas.Children.Clear();

        if (!hasData || points.Count == 0)
        {
            GraphEmptyLabel.Visibility = Visibility.Visible;
            GraphCanvas.Visibility = Visibility.Collapsed;
            GraphLabels.Visibility = Visibility.Collapsed;
            return;
        }

        GraphEmptyLabel.Visibility = Visibility.Collapsed;
        GraphCanvas.Visibility = Visibility.Visible;

        var values = points.Select(p => (double)p.Cost).ToList();
        var labels = BuildLabels(points, values.Count);
        DrawLabels(labels);
        GraphCanvas.Visibility = Visibility.Visible;
        GraphLabels.Visibility = Visibility.Visible;

        var canvasWidth = Math.Max(100.0, ActualWidth - 40.0);
        var canvasHeight = 110.0;

        if (values.Count == 1)
        {
            DrawSinglePoint(values[0], canvasWidth, canvasHeight);
            return;
        }

        DrawPolyline(values, canvasWidth, canvasHeight);
    }

    private static List<string> BuildLabels(IReadOnlyList<DailyCostPoint> points, int count)
    {
        var labels = new List<string>();
        if (count <= 4)
        {
            foreach (var p in points)
                labels.Add(p.Date.ToString("MMM d"));
            return labels;
        }

        var step = Math.Max(1, (int)Math.Ceiling(count / 5.0));
        for (var i = 0; i < count; i += step)
        {
            if (labels.Count < 6)
                labels.Add(points[i].Date.ToString("MMM d"));
        }
        return labels;
    }

    private void DrawLabels(List<string> labels)
    {
        GraphLabels.Children.Clear();
        foreach (var label in labels)
        {
            GraphLabels.Children.Add(new TextBlock
            {
                Text = label,
                Foreground = (Brush)Application.Current.FindResource("TertiaryTextBrush"),
                FontSize = 10,
                Margin = new Thickness(0, 0, 12, 0)
            });
        }
    }

    private void DrawSinglePoint(double cost, double canvasWidth, double canvasHeight)
    {
        var accentColor = GetAccentColor();
        var centerX = canvasWidth / 2;
        var centerY = canvasHeight - 10;
        var radius = Math.Max(3.0, Math.Min(12.0, 6.0 + Math.Sqrt(cost) * 2.0));

        var dot = new Ellipse
        {
            Width = radius * 2,
            Height = radius * 2,
            Fill = new SolidColorBrush(accentColor)
        };
        Canvas.SetLeft(dot, centerX - radius);
        Canvas.SetTop(dot, centerY - radius);
        GraphCanvas.Children.Add(dot);
    }

    private void DrawPolyline(List<double> costs, double canvasWidth, double canvasHeight)
    {
        var accentColor = GetAccentColor();
        var bgColor = (Application.Current.FindResource("PrimaryBackgroundBrush") as SolidColorBrush)?.Color
            ?? (Color)ColorConverter.ConvertFromString("#0D0F18");
        var padding = 10.0;

        var allZero = costs.All(c => c <= 0);
        var maxCost = allZero ? 1.0 : Math.Max(costs.Max(), 1e-9);

        var points = new List<Point>();
        var n = costs.Count;
        for (var i = 0; i < n; i++)
        {
            var x = padding + (i * (canvasWidth - 2 * padding) / Math.Max(n - 1, 1));
            var y = allZero
                ? canvasHeight - padding
                : canvasHeight - padding - (costs[i] / maxCost) * (canvasHeight - 2 * padding);
            points.Add(new Point(x, y));
        }

        var fillBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1)
        };
        fillBrush.GradientStops.Add(new GradientStop(Color.FromArgb(60, accentColor.R, accentColor.G, accentColor.B), 0));
        fillBrush.GradientStops.Add(new GradientStop(Color.FromArgb(10, accentColor.R, accentColor.G, accentColor.B), 1));

        var fillGeometry = new StreamGeometry();
        using (var ctx = fillGeometry.Open())
        {
            ctx.BeginFigure(points[0], true, true);
            for (var i = 1; i < points.Count; i++)
            {
                var prev = points[i - 1];
                var curr = points[i];
                var cp1 = new Point(prev.X + (curr.X - prev.X) / 3, prev.Y);
                var cp2 = new Point(curr.X - (curr.X - prev.X) / 3, curr.Y);
                ctx.BezierTo(cp1, cp2, curr, true, false);
            }
            ctx.LineTo(new Point(points[^1].X, canvasHeight), true, false);
            ctx.LineTo(new Point(points[0].X, canvasHeight), true, false);
        }
        fillGeometry.Freeze();

        GraphCanvas.Children.Add(new Path { Data = fillGeometry, Fill = fillBrush });

        var lineGeometry = new StreamGeometry();
        using (var ctx = lineGeometry.Open())
        {
            ctx.BeginFigure(points[0], false, false);
            for (var i = 1; i < points.Count; i++)
            {
                var prev = points[i - 1];
                var curr = points[i];
                var cp1 = new Point(prev.X + (curr.X - prev.X) / 3, prev.Y);
                var cp2 = new Point(curr.X - (curr.X - prev.X) / 3, curr.Y);
                ctx.BezierTo(cp1, cp2, curr, true, false);
            }
        }
        lineGeometry.Freeze();

        GraphCanvas.Children.Add(new Path
        {
            Data = lineGeometry,
            Stroke = new SolidColorBrush(accentColor),
            StrokeThickness = 2
        });

        foreach (var point in points)
        {
            var dot = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = new SolidColorBrush(accentColor),
                Stroke = new SolidColorBrush(bgColor),
                StrokeThickness = 2
            };
            Canvas.SetLeft(dot, point.X - 3);
            Canvas.SetTop(dot, point.Y - 3);
            GraphCanvas.Children.Add(dot);
        }
    }

    private static Color GetAccentColor()
    {
        var brush = Application.Current.FindResource("PrimaryAccentBrush") as SolidColorBrush;
        return brush?.Color ?? (Color)ColorConverter.ConvertFromString("#7C5CFC");
    }
}