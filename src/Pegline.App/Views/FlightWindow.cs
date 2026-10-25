using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Pegline.App.Session;
using Pegline.Core.Domain;
using Pegline.Platform.Win32;

namespace Pegline.App.Views;

/// <summary>
/// The capture shrinking toward its slot, in the monitor's own DIP space.
/// The window is fitted once to that monitor. Only the photo's transform changes,
/// and the slot is read again every frame so the hand-off lands on the live card.
/// </summary>
public sealed class FlightWindow : Window
{
    private readonly MonitorBounds _monitor;
    private readonly DipRect _from;
    private readonly Func<(DipRect Frame, double Angle)?> _live;
    private readonly Task _hold;
    private readonly Action _onShown;
    private readonly Action _onArrive;
    private readonly Canvas _root;
    private readonly Border _outer;
    private readonly Border _inner;
    private readonly Border _clip;
    private readonly SolidColorBrush _fill;
    private readonly SolidColorBrush _edge;
    private readonly RotateTransform _spin = new();
    private readonly DropShadowEffect _shadow;
    private DipRect _to;
    private double _angle;
    private TimeSpan _started = TimeSpan.MinValue;
    private TimeSpan _previousTime = TimeSpan.MinValue;
    private FlightSample _previous;
    private bool _visible;
    private bool _shown;
    private bool _arrived;
    private bool _refitPosted;

    private FlightWindow(
        ImageSource image,
        MonitorBounds monitor,
        DipRect from,
        Func<(DipRect Frame, double Angle)?> live,
        Task hold,
        Action onShown,
        Action onArrive,
        Color clipEdge,
        Color clipBody,
        Color clipShine)
    {
        _monitor = monitor;
        _from = from;
        _to = from;
        _live = live;
        _hold = hold;
        _onShown = onShown;
        _onArrive = onArrive;
        _previous = FlightPath.Sample(from, from, 0, 0);

        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        WindowStartupLocation = WindowStartupLocation.Manual;
        IsHitTestVisible = false;
        UseLayoutRounding = true;
        Width = Math.Max(1, DisplayScale.ToDip(monitor.Width, monitor.DpiX));
        Height = Math.Max(1, DisplayScale.ToDip(monitor.Height, monitor.DpiY));

        var photo = new Image
        {
            Source = image,
            Stretch = Stretch.Uniform,
            CacheMode = new BitmapCache(Math.Max(1, DisplayScale.Factor(monitor.DpiY))),
        };
        RenderOptions.SetBitmapScalingMode(photo, BitmapScalingMode.HighQuality);
        _inner = new Border
        {
            ClipToBounds = true,
            Child = photo,
        };
        _fill = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));
        _edge = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));
        _shadow = new DropShadowEffect
        {
            BlurRadius = 12,
            ShadowDepth = 5,
            Direction = 90,
            Opacity = 0,
            Color = Colors.Black,
        };
        _outer = new Border
        {
            Background = _fill,
            BorderBrush = _edge,
            Child = _inner,
            Effect = _shadow,
        };
        _clip = new Border
        {
            Width = LineLayout.ClipWidth,
            Height = LineLayout.ClipHeight,
            CornerRadius = new CornerRadius(4),
            BorderThickness = new Thickness(0.8),
            IsHitTestVisible = false,
            Opacity = 0,
            Background = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new(clipEdge, 0),
                    new(clipShine, 0.35),
                    new(clipBody, 0.62),
                    new(clipEdge, 1),
                },
                new Point(0, 0.5),
                new Point(1, 0.5)),
            BorderBrush = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new(Color.FromArgb(242, 255, 255, 255), 0),
                    new(Color.FromArgb(153, 0, 0, 0), 1),
                },
                new Point(0, 0),
                new Point(0, 1)),
            Effect = new DropShadowEffect
            {
                BlurRadius = 5,
                ShadowDepth = 1.5,
                Direction = 90,
                Opacity = 0.65,
                Color = Colors.Black,
            },
            Child = new Border
            {
                Width = 8,
                Height = 3.5,
                CornerRadius = new CornerRadius(1.5),
                Background = new SolidColorBrush(Color.FromArgb(204, 26, 26, 26)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 11, 0, 0),
            },
        };
        _root = new Canvas
        {
            RenderTransform = _spin,
            RenderTransformOrigin = new Point(0, 0),
            Children = { _outer, _clip },
        };
        Content = new Canvas { Children = { _root }, IsHitTestVisible = false };
        Opacity = 0;
        SourceInitialized += (_, _) => Fit();
        Apply(_previous);
    }

    public static Task Play(
        ImageSource image,
        MonitorBounds monitor,
        DipRect from,
        Func<(DipRect Frame, double Angle)?> live,
        Task hold,
        Action onShown,
        Action onArrive,
        Color clipEdge,
        Color clipBody,
        Color clipShine)
    {
        if (from.Width < 2 || from.Height < 2 || monitor.Width < 2 || monitor.Height < 2)
        {
            onShown();
            onArrive();
            return Task.CompletedTask;
        }

        var window = new FlightWindow(image, monitor, from, live, hold, onShown, onArrive, clipEdge, clipBody, clipShine);
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        window.Closed += (_, _) => done.TrySetResult();
        window.Show();
        window.Dispatcher.BeginInvoke(() =>
        {
            window.Fit();
            window.Apply(FlightPath.Sample(from, from, 0, 0));
            window.UpdateLayout();
            window._visible = true;
            window.Opacity = 1;
        }, System.Windows.Threading.DispatcherPriority.Loaded);
        CompositionTarget.Rendering += window.OnFrame;
        return done.Task;
    }

    private void Fit()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        WindowPlacement.MakeToolWindow(hwnd);
        MonitorFit.Apply(this, _monitor, null, activate: false);
    }

    private void OnFrame(object? sender, EventArgs e)
    {
        if (e is not RenderingEventArgs timing)
        {
            return;
        }

        if (!_visible)
        {
            return;
        }

        if (!_refitPosted)
        {
            _refitPosted = true;
            Fit();
            Apply(FlightPath.Sample(_from, _from, 0, 0));
        }

        var now = timing.RenderingTime;
        var parked = !_hold.IsCompleted;
        double progress;
        if (parked)
        {
            progress = 0;
            _started = TimeSpan.MinValue;
        }
        else
        {
            if (_started == TimeSpan.MinValue)
            {
                _started = now;
            }

            progress = (now - _started).TotalMilliseconds / Motion.FlightDuration.TotalMilliseconds;
        }

        if (!parked && _live() is { } live && live.Frame.Width >= 2 && live.Frame.Height >= 2)
        {
            _to = live.Frame;
            _angle = live.Angle;
        }

        var sample = FlightPath.Sample(_from, parked ? _from : _to, parked ? 0 : _angle, Math.Min(progress, 1));
        Apply(sample);
        Trace(now, progress, sample);

        if (!_shown)
        {
            _shown = true;
            _onShown();
        }

        if (_arrived || parked || progress < 1)
        {
            return;
        }

        _arrived = true;
        Apply(FlightPath.Sample(_from, _to, _angle, 1));
        _onArrive();
        CompositionTarget.Rendering -= OnFrame;
        Opacity = 0;
        Close();
    }

    private void Apply(FlightSample sample)
    {
        var chrome = sample.Chrome;
        var width = Math.Max(1, sample.Width);
        var height = Math.Max(1, sample.Height);
        _outer.Width = width;
        _outer.Height = height;
        _outer.Padding = new Thickness(LineLayout.FrameInset * chrome);
        _outer.BorderThickness = new Thickness(LineLayout.FrameBorder * chrome);
        _outer.CornerRadius = new CornerRadius(LineLayout.FrameRadius * chrome);
        _inner.CornerRadius = new CornerRadius(Math.Max(0, LineLayout.PhotoRadius * chrome));
        var alpha = (byte)Math.Round(244 * chrome);
        _fill.Color = Color.FromArgb(alpha, 255, 255, 255);
        _edge.Color = Color.FromArgb(alpha, 255, 255, 255);
        _shadow.Opacity = 0.22 * chrome;
        Canvas.SetLeft(_outer, 0);
        Canvas.SetTop(_outer, 0);
        Canvas.SetLeft(_clip, (width - LineLayout.ClipWidth) / 2);
        Canvas.SetTop(_clip, -LineLayout.FrameTop);
        _clip.Opacity = chrome;
        Canvas.SetLeft(_root, sample.X);
        Canvas.SetTop(_root, sample.Y);
        _spin.Angle = sample.Angle;
        _spin.CenterX = width / 2;
        _spin.CenterY = LineLayout.FramePivotOffset * chrome;
    }

    private void Trace(TimeSpan now, double progress, FlightSample sample)
    {
        if (!FlightTrace.Enabled)
        {
            return;
        }

        var dt = _previousTime == TimeSpan.MinValue ? 0 : (now - _previousTime).TotalMilliseconds;
        _previousTime = now;
        var step = Math.Abs(sample.X - _previous.X) + Math.Abs(sample.Y - _previous.Y)
            + Math.Abs(sample.Width - _previous.Width) + Math.Abs(sample.Height - _previous.Height);
        var jump = step > 200 ? "JUMP " : "";
        _previous = sample;
        FlightTrace.Write(
            $"{jump}dt={dt:0.0} progress={progress:0.000} x={sample.X:0.0} y={sample.Y:0.0} w={sample.Width:0.0} h={sample.Height:0.0} angle={sample.Angle:0.00} chrome={sample.Chrome:0.00} target={_to.X:0.0},{_to.Y:0.0},{_to.Width:0.0},{_to.Height:0.0} step={step:0.0}");
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        Fit();
    }

    protected override void OnClosed(EventArgs e)
    {
        CompositionTarget.Rendering -= OnFrame;
        base.OnClosed(e);
    }
}
