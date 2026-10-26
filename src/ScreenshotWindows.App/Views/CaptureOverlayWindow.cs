using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using ScreenshotWindows.App.Imaging;
using ScreenshotWindows.App.Session;
using ScreenshotWindows.Core.Domain;
using ScreenshotWindows.Platform.Win32;

namespace ScreenshotWindows.App.Views;

public readonly record struct SelectedRegion(PixelRect Region, Rect ScreenDip);

public partial class CaptureOverlayWindow : Window
{
    private readonly FrozenMonitor _frame;
    private Point _origin;
    private bool _dragging;
    private bool _silent;
    private bool _closing;
    private bool _fitting;
    private Task? _fade;

    public CaptureOverlayWindow(FrozenMonitor frame)
    {
        _frame = frame;
        InitializeComponent();
        Shot.Source = ImageLoader.FromFrame(frame);
        SourceInitialized += (_, _) => Fit(activate: false);
        Loaded += (_, _) => Fit(activate: false);
        SizeChanged += (_, _) => Cover();
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        Fit(activate: false);
    }

    public event EventHandler<SelectedRegion>? Committed;

    public event EventHandler? Cancelled;

    public void Dismiss()
    {
        _silent = true;
        if (!_closing)
        {
            Close();
        }
    }

    public Task FadeOutAsync()
    {
        if (_fade is not null)
        {
            return _fade;
        }

        if (_closing)
        {
            return Task.CompletedTask;
        }

        _silent = true;
        _dragging = false;
        if (IsMouseCaptured)
        {
            ReleaseMouseCapture();
        }

        IsHitTestVisible = false;
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
        {
            WindowPlacement.SetClickThrough(hwnd, true);
        }

        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _fade = done.Task;
        var start = TimeSpan.MinValue;
        EventHandler handler = null!;
        handler = (_, args) =>
        {
            if (args is not RenderingEventArgs timing)
            {
                return;
            }

            if (_closing)
            {
                CompositionTarget.Rendering -= handler;
                done.TrySetResult();
                return;
            }

            if (start == TimeSpan.MinValue)
            {
                start = timing.RenderingTime;
            }

            var progress = (timing.RenderingTime - start).TotalMilliseconds / Motion.OverlayFade.TotalMilliseconds;
            var opacity = 1 - Easing.OutCubic(progress);
            Shade.Opacity = opacity;
            Hint.Opacity = opacity;
            if (progress < 1)
            {
                return;
            }

            CompositionTarget.Rendering -= handler;
            Dismiss();
            done.TrySetResult();
        };
        CompositionTarget.Rendering += handler;
        return _fade;
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        _closing = true;
        if (!_silent)
        {
            _silent = true;
            Cancelled?.Invoke(this, EventArgs.Empty);
        }

        base.OnClosing(e);
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        Focus();
        if (e.ClickCount >= 2)
        {
            _dragging = false;
            ReleaseMouseCapture();
            Commit(CaptureCrop.FullFrame(_frame.PixelWidth, _frame.PixelHeight));
            return;
        }

        _dragging = true;
        _origin = e.GetPosition(Shot);
        Hint.Visibility = Visibility.Collapsed;
        CaptureMouse();
        ShowRegion(RegionBetween(_origin, _origin));
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        ShowRegion(RegionBetween(_origin, e.GetPosition(Shot)));
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        _dragging = false;
        ReleaseMouseCapture();
        var region = RegionBetween(_origin, e.GetPosition(Shot));
        if (CaptureCrop.CanEncode(region))
        {
            Commit(region);
        }
    }

    private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        Cancel();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape)
        {
            e.Handled = true;
            Cancel();
            return;
        }

        if (key is Key.Return or Key.Enter)
        {
            e.Handled = true;
            Commit(CaptureCrop.FullFrame(_frame.PixelWidth, _frame.PixelHeight));
        }
    }

    private void Commit(PixelRect region)
    {
        if (_silent || _closing || !CaptureCrop.CanEncode(region))
        {
            return;
        }

        var screen = ScreenDip(region);
        _silent = true;
        Committed?.Invoke(this, new SelectedRegion(region, screen));
    }

    private void Cancel()
    {
        if (_silent || _closing)
        {
            return;
        }

        _silent = true;
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    private bool _refit;

    private void Fit(bool activate)
    {
        ApplyFit(activate);
        if (_refit)
        {
            return;
        }

        _refit = true;
        Dispatcher.BeginInvoke(() =>
        {
            _refit = false;
            ApplyFit(activate);
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void ApplyFit(bool activate)
    {
        if (_fitting)
        {
            return;
        }

        _fitting = true;
        try
        {
            MonitorFit.Apply(this, _frame.Monitor, null, activate);
            Shot.Width = Width;
            Shot.Height = Height;
            Cover();
        }
        finally
        {
            _fitting = false;
        }
    }

    private void Cover()
    {
        if (_dragging)
        {
            return;
        }

        var width = Shade.ActualWidth;
        var height = Shade.ActualHeight;
        DimTop.Width = width;
        DimTop.Height = height;
        Canvas.SetLeft(DimTop, 0);
        Canvas.SetTop(DimTop, 0);
        DimLeft.Width = 0;
        DimRight.Width = 0;
        DimBottom.Height = 0;
    }

    private void ShowRegion(PixelRect region)
    {
        var viewWidth = ViewWidth;
        var viewHeight = ViewHeight;
        var left = CaptureMapping.ToView(region.X, _frame.PixelWidth, viewWidth);
        var top = CaptureMapping.ToView(region.Y, _frame.PixelHeight, viewHeight);
        var right = CaptureMapping.ToView(region.X + region.Width, _frame.PixelWidth, viewWidth);
        var bottom = CaptureMapping.ToView(region.Y + region.Height, _frame.PixelHeight, viewHeight);
        var width = Math.Max(0, right - left);
        var height = Math.Max(0, bottom - top);

        Place(DimTop, 0, 0, viewWidth, top);
        Place(DimLeft, 0, top, left, height);
        Place(DimRight, right, top, Math.Max(0, viewWidth - right), height);
        Place(DimBottom, 0, bottom, viewWidth, Math.Max(0, viewHeight - bottom));

        Stroke.Visibility = Visibility.Visible;
        Place(Stroke, left, top, width, height);

        SizeText.Text = region.Width + " × " + region.Height;
        Badge.Visibility = Visibility.Visible;
        Badge.UpdateLayout();
        var badgeLeft = right - Badge.ActualWidth - 6;
        var badgeTop = bottom + 6;
        if (badgeTop + Badge.ActualHeight > viewHeight)
        {
            badgeTop = Math.Max(0, top - Badge.ActualHeight - 6);
        }

        Canvas.SetLeft(Badge, Math.Max(0, badgeLeft));
        Canvas.SetTop(Badge, badgeTop);
    }

    private static void Place(FrameworkElement element, double x, double y, double width, double height)
    {
        Canvas.SetLeft(element, x);
        Canvas.SetTop(element, y);
        element.Width = Math.Max(0, width);
        element.Height = Math.Max(0, height);
    }

    /// <summary>
    /// Selection in this monitor's DIP space. The overlay's client origin is the monitor corner,
    /// the same space the line and the flight use, so the flight starts on the hole.
    /// </summary>
    private Rect ScreenDip(PixelRect region)
    {
        var viewWidth = ViewWidth;
        var viewHeight = ViewHeight;
        var left = CaptureMapping.ToView(region.X, _frame.PixelWidth, viewWidth);
        var top = CaptureMapping.ToView(region.Y, _frame.PixelHeight, viewHeight);
        var width = CaptureMapping.ToView(region.Width, _frame.PixelWidth, viewWidth);
        var height = CaptureMapping.ToView(region.Height, _frame.PixelHeight, viewHeight);
        return new Rect(left, top, Math.Max(0, width), Math.Max(0, height));
    }

    private PixelRect RegionBetween(Point start, Point end)
    {
        var a = PixelAt(start);
        var b = PixelAt(end);
        return CaptureMapping.FromPoints(a.X, a.Y, b.X, b.Y, _frame.PixelWidth, _frame.PixelHeight);
    }

    private (int X, int Y) PixelAt(Point view)
    {
        return (
            CaptureMapping.ToPixel(view.X, ViewWidth, _frame.PixelWidth),
            CaptureMapping.ToPixel(view.Y, ViewHeight, _frame.PixelHeight));
    }

    private double ViewWidth => Shot.ActualWidth > 1 ? Shot.ActualWidth : ActualWidth;

    private double ViewHeight => Shot.ActualHeight > 1 ? Shot.ActualHeight : ActualHeight;
}
