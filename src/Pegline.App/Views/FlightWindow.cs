using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Pegline.Platform.Win32;

namespace Pegline.App.Views;

/// <summary>A short flight from the selection to the card that will hold it.</summary>
public sealed class FlightWindow : Window
{
    private FlightWindow(ImageSource image)
    {
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Background = Brushes.White;
        Content = new Image
        {
            Source = image,
            Stretch = Stretch.Uniform,
        };
        SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            WindowPlacement.MakeToolWindow(hwnd);
        };
    }

    public static Task Play(ImageSource image, Rect from, Rect to)
    {
        if (from.Width < 2 || from.Height < 2 || to.Width < 2 || to.Height < 2)
        {
            return Task.CompletedTask;
        }

        var window = new FlightWindow(image)
        {
            Left = from.X,
            Top = from.Y,
            Width = from.Width,
            Height = from.Height,
        };
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        window.Show();
        var ease = new QuadraticEase { EasingMode = EasingMode.EaseInOut };
        var duration = TimeSpan.FromMilliseconds(460);
        window.BeginAnimation(LeftProperty, Move(from.X, to.X, duration, ease));
        window.BeginAnimation(TopProperty, Move(from.Y, to.Y, duration, ease));
        window.BeginAnimation(WidthProperty, Move(from.Width, to.Width, duration, ease));
        var height = Move(from.Height, to.Height, duration, ease);
        height.Completed += (_, _) =>
        {
            try
            {
                window.Close();
            }
            catch (InvalidOperationException)
            {
            }

            done.TrySetResult();
        };
        window.BeginAnimation(HeightProperty, height);
        return done.Task;
    }

    private static DoubleAnimation Move(double from, double to, TimeSpan duration, IEasingFunction ease) =>
        new(from, to, duration) { EasingFunction = ease };
}
