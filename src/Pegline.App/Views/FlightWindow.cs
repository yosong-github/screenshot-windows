using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Pegline.Core.Domain;
using Pegline.Platform.Win32;

namespace Pegline.App.Views;

/// <summary>
/// Shrinks the capture toward its slot. The window is positioned once;
/// only a render transform moves the photo, so the frame rate is not tied to SetWindowPos.
/// </summary>
public sealed class FlightWindow : Window
{
    private readonly Border _card;
    private readonly ScaleTransform _scale = new(1, 1);
    private readonly TranslateTransform _move = new();
    private readonly RotateTransform _spin = new();
    private readonly DipRect _from;
    private readonly DipRect _to;
    private readonly double _tilt;
    private readonly double _homeLeft;
    private readonly double _homeTop;
    private readonly double _cardWidth;
    private readonly double _cardHeight;
    private readonly Action? _onArrive;
    private TimeSpan _started = TimeSpan.MinValue;
    private bool _fading;
    private TimeSpan _fadeStart;

    private FlightWindow(ImageSource image, DipRect from, DipRect to, double tilt, Rect home, Action? onArrive)
    {
        _from = from;
        _to = to;
        _tilt = tilt;
        _onArrive = onArrive;
        _homeLeft = home.X;
        _homeTop = home.Y;
        _cardWidth = Math.Max(1, to.Width);
        _cardHeight = Math.Max(1, to.Height);

        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        WindowStartupLocation = WindowStartupLocation.Manual;
        IsHitTestVisible = false;
        Left = home.X;
        Top = home.Y;
        Width = Math.Max(1, home.Width);
        Height = Math.Max(1, home.Height);

        var photo = new Image
        {
            Source = image,
            Stretch = Stretch.UniformToFill,
            CacheMode = new BitmapCache(),
        };
        RenderOptions.SetBitmapScalingMode(photo, BitmapScalingMode.HighQuality);
        var frame = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(244, 255, 255, 255)),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(4),
            Child = photo,
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 16,
                ShadowDepth = 6,
                Direction = 90,
                Opacity = 0.28,
            },
        };
        var pivoted = new Border
        {
            Width = _cardWidth,
            Height = _cardHeight,
            RenderTransformOrigin = new Point(0.5, 0),
            Child = frame,
            RenderTransform = new TransformGroup
            {
                Children = { _scale, _spin },
            },
        };
        _card = new Border
        {
            Child = pivoted,
            RenderTransform = _move,
        };
        Content = new Canvas { Children = { _card }, IsHitTestVisible = false };
        SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            WindowPlacement.MakeToolWindow(hwnd);
        };
        Apply(FlightPath.Sample(_from, _to, _tilt, 0));
    }

    public static Task Play(ImageSource image, Rect from, Rect to, double tiltDegrees, Action? onArrive = null)
    {
        if (from.Width < 2 || from.Height < 2 || to.Width < 2 || to.Height < 2)
        {
            onArrive?.Invoke();
            return Task.CompletedTask;
        }

        var source = new DipRect(from.X, from.Y, from.Width, from.Height);
        var dest = new DipRect(to.X, to.Y, to.Width, to.Height);
        var home = Rect.Union(from, to);
        home.Inflate(48, 64);
        var window = new FlightWindow(image, source, dest, tiltDegrees, home, onArrive);
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        window.Closed += (_, _) => done.TrySetResult();
        window.Show();
        CompositionTarget.Rendering += window.OnFrame;
        return done.Task;
    }

    private void OnFrame(object? sender, EventArgs e)
    {
        if (e is not RenderingEventArgs timing)
        {
            return;
        }

        if (_started == TimeSpan.MinValue)
        {
            _started = timing.RenderingTime;
        }

        if (!_fading)
        {
            var progress = (timing.RenderingTime - _started).TotalMilliseconds / Motion.FlightDuration.TotalMilliseconds;
            if (progress >= 1)
            {
                Apply(FlightPath.Sample(_from, _to, _tilt, 1));
                _fading = true;
                _fadeStart = timing.RenderingTime;
                _onArrive?.Invoke();
                return;
            }

            Apply(FlightPath.Sample(_from, _to, _tilt, progress));
            return;
        }

        var fade = (timing.RenderingTime - _fadeStart).TotalMilliseconds / Motion.FlightFade.TotalMilliseconds;
        Opacity = 1 - Easing.Clamp01(fade);
        if (fade >= 1)
        {
            CompositionTarget.Rendering -= OnFrame;
            Close();
        }
    }

    private void Apply(FlightSample sample)
    {
        var scaleX = _cardWidth <= 0 ? 1 : sample.Width / _cardWidth;
        var scaleY = _cardHeight <= 0 ? 1 : sample.Height / _cardHeight;
        _scale.ScaleX = scaleX;
        _scale.ScaleY = scaleY;
        _spin.Angle = sample.Angle;
        var visualLeft = sample.X - _homeLeft;
        var visualTop = sample.Y - _homeTop;
        _move.X = visualLeft - _cardWidth / 2 * (1 - scaleX);
        _move.Y = visualTop;
    }
}
