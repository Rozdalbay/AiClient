using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AiDesktopClient.Controls;

public partial class SkeletonControl : UserControl
{
    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register(nameof(CornerRadius), typeof(double), typeof(SkeletonControl), new PropertyMetadata(4.0));

    private bool _isShimmering;

    public SkeletonControl()
    {
        InitializeComponent();
        IsVisibleChanged += SkeletonControl_IsVisibleChanged;
        SizeChanged += SkeletonControl_SizeChanged;
        Loaded += (_, _) => StartShimmer();
        Unloaded += (_, _) => StopShimmer();
    }

    public double CornerRadius
    {
        get => (double)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    private void SkeletonControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
            StartShimmer();
        else
            StopShimmer();
    }

    private void SkeletonControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (IsVisible && _isShimmering)
            StartShimmer();
    }

    private void StartShimmer()
    {
        if (!IsVisible) return;

        var width = Math.Max(Root.ActualWidth, 1);
        ShimmerBand.Width = width;

        var animation = new DoubleAnimation(-width, width, TimeSpan.FromMilliseconds(1400))
        {
            RepeatBehavior = RepeatBehavior.Forever
        };

        ShimmerTransform.BeginAnimation(TranslateTransform.XProperty, animation);
        _isShimmering = true;
    }

    private void StopShimmer()
    {
        ShimmerTransform.BeginAnimation(TranslateTransform.XProperty, null);
        _isShimmering = false;
    }
}