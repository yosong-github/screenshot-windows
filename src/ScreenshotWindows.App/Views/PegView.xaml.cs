using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ScreenshotWindows.App.Views;

/// <summary>
/// Flat clothespin. Two rounded jaws, a small head, and a rivet. The fill is a soft
/// vertical wash of one color; the outline is a step darker. No chrome highlight.
/// </summary>
public partial class PegView : UserControl
{
    public static readonly DependencyProperty BodyProperty = DependencyProperty.Register(
        nameof(Body),
        typeof(Color),
        typeof(PegView),
        new PropertyMetadata(Color.FromRgb(236, 236, 239), OnColorChanged));

    public static readonly DependencyProperty EdgeProperty = DependencyProperty.Register(
        nameof(Edge),
        typeof(Color),
        typeof(PegView),
        new PropertyMetadata(Color.FromRgb(176, 176, 182), OnColorChanged));

    public static readonly DependencyProperty ShineProperty = DependencyProperty.Register(
        nameof(Shine),
        typeof(Color),
        typeof(PegView),
        new PropertyMetadata(Color.FromRgb(250, 250, 252), OnColorChanged));

    public PegView()
    {
        InitializeComponent();
        ApplyColors();
    }

    public Color Body
    {
        get => (Color)GetValue(BodyProperty);
        set => SetValue(BodyProperty, value);
    }

    public Color Edge
    {
        get => (Color)GetValue(EdgeProperty);
        set => SetValue(EdgeProperty, value);
    }

    public Color Shine
    {
        get => (Color)GetValue(ShineProperty);
        set => SetValue(ShineProperty, value);
    }

    private static void OnColorChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is PegView peg && peg.IsInitialized)
        {
            peg.ApplyColors();
        }
    }

    private void ApplyColors()
    {
        var fill = new LinearGradientBrush
        {
            StartPoint = new Point(0.5, 0),
            EndPoint = new Point(0.5, 1),
            GradientStops =
            {
                new GradientStop(Shine, 0),
                new GradientStop(Body, 1),
            },
        };
        fill.Freeze();
        var outline = new SolidColorBrush(Edge);
        outline.Freeze();
        var rivet = new SolidColorBrush(Edge);
        rivet.Freeze();
        var hole = new SolidColorBrush(Shine);
        hole.Freeze();

        LeftJaw.Fill = fill;
        RightJaw.Fill = fill;
        Head.Fill = fill;
        LeftJaw.Stroke = outline;
        RightJaw.Stroke = outline;
        Head.Stroke = outline;
        Rivet.Fill = rivet;
        RivetHole.Fill = hole;
    }
}
