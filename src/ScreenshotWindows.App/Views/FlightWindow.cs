using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using ScreenshotWindows.App.Session;
using ScreenshotWindows.Core.Domain;
using ScreenshotWindows.Platform.Win32;

namespace ScreenshotWindows.App.Views;

/// <summary>
/// One already-shown transparent host per monitor. A flight only moves a cached
/// sprite with a render transform. The host is fitted once and is not resized
/// while frames are running, so the layered window is not rebuilt every frame.
/// </summary>
public sealed class FlightWindow : Window
{
    private const double ShadowPad = 24;

    private static readonly Dictionary<string, FlightWindow> Hosts = new(StringComparer.Ordinal);

    private static readonly SolidColorBrush FrameBrush = CreateFrameBrush();

    private readonly Canvas _stage;
    private MonitorBounds _monitor;
    private TaskCompletionSource? _done;
    private Func<(DipRect Frame, double Angle)?>? _live;
    private Action? _onShown;
    private Action? _onArrive;
    private Task? _hold;
    private DipRect _from;
    private DipRect _to;
    private double _angle;
    private double _slotW = 1;
    private double _slotH = 1;
    private double _photoW = 1;
    private double _photoH = 1;
    private double _inset;
    private TimeSpan _started = TimeSpan.MinValue;
    private TimeSpan _previousTime = TimeSpan.MinValue;
    private FlightSample _previous;
    private bool _shown;
    private bool _arrived;
    private bool _playing;
    private ScaleTransform? _cardScale;
    private RotateTransform? _cardSpin;
    private TranslateTransform? _cardMove;
    private ScaleTransform? _photoExtra;
    private UIElement? _frameHost;
    private RotateTransform? _pegSpin;
    private TranslateTransform? _pegMove;
    private PegView? _peg;

    private FlightWindow(MonitorBounds monitor)
    {
        _monitor = monitor;
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
        _stage = new Canvas { IsHitTestVisible = false };
        Content = _stage;
        SourceInitialized += (_, _) => Fit();
    }

    public static void Warm(IReadOnlyList<MonitorBounds> monitors)
    {
        foreach (var monitor in monitors)
        {
            if (monitor.Width < 2 || monitor.Height < 2)
            {
                continue;
            }

            GetHost(monitor);
        }
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

        return GetHost(monitor).StartFlight(image, from, live, hold, onShown, onArrive, clipEdge, clipBody, clipShine);
    }

    private static FlightWindow GetHost(MonitorBounds monitor)
    {
        if (Hosts.TryGetValue(monitor.DeviceName, out var host))
        {
            host.Adopt(monitor);
            return host;
        }

        host = new FlightWindow(monitor);
        Hosts[monitor.DeviceName] = host;
        host.Show();
        return host;
    }

    private void Adopt(MonitorBounds monitor)
    {
        var changed = !_monitor.Equals(monitor);
        _monitor = monitor;
        if (!IsVisible)
        {
            Show();
            return;
        }

        if (changed && !_playing)
        {
            Fit();
        }
    }

    private Task StartFlight(
        ImageSource image,
        DipRect from,
        Func<(DipRect Frame, double Angle)?> live,
        Task hold,
        Action onShown,
        Action onArrive,
        Color clipEdge,
        Color clipBody,
        Color clipShine)
    {
        Finish();
        _from = from;
        _to = from;
        _angle = 0;
        _live = live;
        _hold = hold;
        _onShown = onShown;
        _onArrive = onArrive;
        _started = TimeSpan.MinValue;
        _previousTime = TimeSpan.MinValue;
        _previous = FlightPath.Sample(from, from, 0, 0);
        _shown = false;
        _arrived = false;
        _stage.Opacity = 1;

        var slot = live();
        if (slot is { } laidOut && laidOut.Frame.Width >= 2 && laidOut.Frame.Height >= 2)
        {
            _slotW = laidOut.Frame.Width;
            _slotH = laidOut.Frame.Height;
        }
        else if (image is BitmapSource bitmap && bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0)
        {
            var photo = LineLayout.PhotoSize(bitmap.PixelWidth, bitmap.PixelHeight);
            var extra = (LineLayout.FrameInset + LineLayout.FrameBorder) * 2;
            _slotW = photo.Width + extra;
            _slotH = photo.Height + extra;
        }
        else
        {
            _slotW = LineLayout.CardWidth;
            _slotH = LineLayout.PhotoMaxHeight + (LineLayout.FrameInset + LineLayout.FrameBorder) * 2;
        }

        _inset = LineLayout.FrameInset + LineLayout.FrameBorder;
        _photoW = Math.Max(1, _slotW - _inset * 2);
        _photoH = Math.Max(1, _slotH - _inset * 2);
        BuildSprite(image, clipEdge, clipBody, clipShine);
        Raise();
        var pixels = image is BitmapSource source ? $"{source.PixelWidth}x{source.PixelHeight}" : "n/a";
        FlightTrace.NoteRenderTier();
        FlightTrace.BeginFlight(
            FormattableString.Invariant(
                $"flight monitor={_monitor.Width}x{_monitor.Height} dpi={_monitor.DpiX}x{_monitor.DpiY} slot={_slotW:0.0}x{_slotH:0.0} bitmap={pixels}"));

        _playing = true;
        _done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CompositionTarget.Rendering += OnFrame;
        return _done.Task;
    }

    private void BuildSprite(ImageSource image, Color clipEdge, Color clipBody, Color clipShine)
    {
        var dpi = DisplayScale.Factor(_monitor.DpiY);
        var brush = new ImageBrush(image)
        {
            Stretch = Stretch.UniformToFill,
            AlignmentX = AlignmentX.Center,
            AlignmentY = AlignmentY.Center,
        };
        RenderOptions.SetBitmapScalingMode(brush, BitmapScalingMode.HighQuality);
        if (brush.CanFreeze)
        {
            brush.Freeze();
        }

        var shadow = new DropShadowEffect
        {
            BlurRadius = 12,
            ShadowDepth = 5,
            Direction = 90,
            Opacity = 0.22,
            Color = Colors.Black,
        };
        if (shadow.CanFreeze)
        {
            shadow.Freeze();
        }

        var frame = new Border
        {
            Width = _slotW,
            Height = _slotH,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            CornerRadius = new CornerRadius(LineLayout.FrameRadius),
            BorderThickness = new Thickness(LineLayout.FrameBorder),
            Background = FrameBrush,
            BorderBrush = FrameBrush,
            Effect = shadow,
            IsHitTestVisible = false,
        };
        var shadowHost = new Border
        {
            Width = _slotW + ShadowPad * 2,
            Height = _slotH + ShadowPad * 2,
            Background = Brushes.Transparent,
            IsHitTestVisible = false,
            CacheMode = new BitmapCache(dpi) { EnableClearType = false },
            Opacity = 0,
            Child = frame,
        };
        Canvas.SetLeft(shadowHost, -ShadowPad);
        Canvas.SetTop(shadowHost, -ShadowPad);
        _frameHost = shadowHost;

        _photoExtra = new ScaleTransform(1, 1);
        var photo = new Border
        {
            Width = _photoW,
            Height = _photoH,
            Background = brush,
            CornerRadius = new CornerRadius(LineLayout.PhotoRadius),
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = _photoExtra,
            CacheMode = new BitmapCache(dpi) { EnableClearType = false },
            IsHitTestVisible = false,
        };
        Canvas.SetLeft(photo, _inset);
        Canvas.SetTop(photo, _inset);

        _cardScale = new ScaleTransform(1, 1);
        _cardSpin = new RotateTransform();
        _cardMove = new TranslateTransform();
        var card = new Canvas
        {
            Width = _slotW,
            Height = _slotH,
            IsHitTestVisible = false,
            RenderTransform = new TransformGroup
            {
                Children = { _cardScale, _cardSpin, _cardMove },
            },
            Children = { shadowHost, photo },
        };

        _pegSpin = new RotateTransform { CenterX = LineLayout.ClipWidth / 2 };
        _pegMove = new TranslateTransform();
        _peg = new PegView
        {
            Width = LineLayout.ClipWidth,
            Height = LineLayout.ClipHeight,
            IsHitTestVisible = false,
            Opacity = 0,
            Body = clipBody,
            Edge = clipEdge,
            Shine = clipShine,
            CacheMode = new BitmapCache(dpi) { EnableClearType = false },
            RenderTransform = new TransformGroup
            {
                Children = { _pegSpin, _pegMove },
            },
        };

        _stage.Children.Clear();
        _stage.Children.Add(card);
        _stage.Children.Add(_peg);
    }

    private void OnFrame(object? sender, EventArgs e)
    {
        if (!_playing || e is not RenderingEventArgs timing || _cardScale is null || _cardSpin is null || _cardMove is null || _photoExtra is null || _frameHost is null || _peg is null || _pegSpin is null || _pegMove is null)
        {
            return;
        }

        var now = timing.RenderingTime;
        var parked = _hold is { IsCompleted: false };
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

        if (!parked && _live?.Invoke() is { } live && live.Frame.Width >= 2 && live.Frame.Height >= 2)
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
            _onShown?.Invoke();
        }

        if (_arrived || parked || progress < 1)
        {
            return;
        }

        _arrived = true;
        _stage.Opacity = 0;
        _onArrive?.Invoke();
        Finish();
    }

    private void Apply(FlightSample sample)
    {
        var chrome = sample.Chrome;
        var sampleW = Math.Max(1, sample.Width);
        var sampleH = Math.Max(1, sample.Height);
        _cardScale!.ScaleX = sampleW / _slotW;
        _cardScale.ScaleY = sampleH / _slotH;
        _cardSpin!.Angle = sample.Angle;
        _cardSpin.CenterX = sampleW / 2;
        _cardSpin.CenterY = LineLayout.FramePivotOffset * chrome;
        _cardMove!.X = sample.X;
        _cardMove.Y = sample.Y;

        var innerW = Math.Max(0, sampleW - 2 * _inset * chrome);
        var innerH = Math.Max(0, sampleH - 2 * _inset * chrome);
        _photoExtra!.ScaleX = innerW * _slotW / (_photoW * sampleW);
        _photoExtra.ScaleY = innerH * _slotH / (_photoH * sampleH);
        _frameHost!.Opacity = chrome;

        _pegSpin!.Angle = sample.Angle;
        _pegSpin.CenterY = LineLayout.FrameTop + LineLayout.FramePivotOffset * chrome;
        _pegMove!.X = sample.X + (sampleW - LineLayout.ClipWidth) / 2;
        _pegMove.Y = sample.Y - LineLayout.FrameTop;
        _peg!.Opacity = chrome;
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
        FlightTrace.Frame(
            dt,
            FormattableString.Invariant(
                $"{jump}dt={dt:0.0} progress={progress:0.000} x={sample.X:0.0} y={sample.Y:0.0} w={sample.Width:0.0} h={sample.Height:0.0} angle={sample.Angle:0.00} chrome={sample.Chrome:0.00} target={_to.X:0.0},{_to.Y:0.0},{_to.Width:0.0},{_to.Height:0.0} step={step:0.0}"));
    }

    private void Finish()
    {
        if (!_playing && _done is null)
        {
            return;
        }

        _playing = false;
        CompositionTarget.Rendering -= OnFrame;
        ClearSprite();
        FlightTrace.EndFlight();
        var done = _done;
        _done = null;
        _live = null;
        _onShown = null;
        _onArrive = null;
        _hold = null;
        done?.TrySetResult();
    }

    private void ClearSprite()
    {
        _stage.Children.Clear();
        _cardScale = null;
        _cardSpin = null;
        _cardMove = null;
        _photoExtra = null;
        _frameHost = null;
        _pegSpin = null;
        _pegMove = null;
        _peg = null;
    }

    private void Raise()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
        {
            WindowPlacement.RaiseTopmost(hwnd);
        }
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

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        if (!_playing)
        {
            Fit();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        Finish();
        Hosts.Remove(_monitor.DeviceName);
        base.OnClosed(e);
    }

    private static SolidColorBrush CreateFrameBrush()
    {
        var brush = new SolidColorBrush(Color.FromArgb(244, 255, 255, 255));
        brush.Freeze();
        return brush;
    }
}
