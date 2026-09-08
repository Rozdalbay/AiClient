using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace AiDesktopClient.Controls;

public partial class GeneratingIndicator : UserControl
{
    private Storyboard? _typingStoryboard;

    public GeneratingIndicator()
    {
        InitializeComponent();
        IsVisibleChanged += OnIsVisibleChanged;
        Unloaded += OnUnloaded;
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
            StartAnimation();
        else
            StopAnimation();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        StopAnimation();
    }

    private void StartAnimation()
    {
        if (!IsLoaded)
            return;

        _typingStoryboard ??= (Storyboard)Resources["TypingStoryboard"];
        _typingStoryboard.Begin(this, true);
    }

    private void StopAnimation()
    {
        _typingStoryboard?.Remove(this);
    }
}
