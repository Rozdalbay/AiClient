using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using AiDesktopClient.Models;

namespace AiDesktopClient.Views;

public partial class UsagePanel : UserControl
{
    public UsagePanel()
    {
        InitializeComponent();
        Loaded += UsagePanel_Loaded;
    }

    private void UsagePanel_Loaded(object sender, RoutedEventArgs e)
    {
        DrawGraph();
    }

    private void DrawGraph()
    {
        GraphCanvas.Children.Clear();

        if (DataContext is not UsageInfo usage)
            return;

        var costs = new double[] { 0.4, 1.2, 0.8, 1.5, 0.6, 1.8, 1.42 };
        var maxCost = costs.Max();
        var canvasWidth = 290.0;
        var canvasHeight = 110.0;
        var padding = 10.0;

        if (costs.Length < 2) return;

        var points = new List<Point>();
        for (int i = 0; i < costs.Length; i++)
        {
            var x = padding + (i * (canvasWidth - 2 * padding) / (costs.Length - 1));
            var y = canvasHeight - padding - ((costs[i] / maxCost) * (canvasHeight - 2 * padding));
            points.Add(new Point(x, y));
        }

        var fillBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1)
        };
        fillBrush.GradientStops.Add(new GradientStop(Color.FromArgb(60, 124, 92, 252), 0));
        fillBrush.GradientStops.Add(new GradientStop(Color.FromArgb(10, 124, 92, 252), 1));

        var fillGeometry = new StreamGeometry();
        using (var ctx = fillGeometry.Open())
        {
            ctx.BeginFigure(points[0], true, true);
            for (int i = 1; i < points.Count; i++)
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

        var fillPath = new Path
        {
            Data = fillGeometry,
            Fill = fillBrush
        };
        GraphCanvas.Children.Add(fillPath);

        var lineGeometry = new StreamGeometry();
        using (var ctx = lineGeometry.Open())
        {
            ctx.BeginFigure(points[0], false, false);
            for (int i = 1; i < points.Count; i++)
            {
                var prev = points[i - 1];
                var curr = points[i];
                var cp1 = new Point(prev.X + (curr.X - prev.X) / 3, prev.Y);
                var cp2 = new Point(curr.X - (curr.X - prev.X) / 3, curr.Y);
                ctx.BezierTo(cp1, cp2, curr, true, false);
            }
        }
        lineGeometry.Freeze();

        var linePath = new Path
        {
            Data = lineGeometry,
            Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7C5CFC")),
            StrokeThickness = 2
        };
        GraphCanvas.Children.Add(linePath);

        foreach (var point in points)
        {
            var dot = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7C5CFC")),
                Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0D0F18")),
                StrokeThickness = 2
            };
            Canvas.SetLeft(dot, point.X - 3);
            Canvas.SetTop(dot, point.Y - 3);
            GraphCanvas.Children.Add(dot);
        }
    }
}
