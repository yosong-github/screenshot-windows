using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Pegline.App.Session;
using Pegline.App.ViewModels;
using Pegline.Core.Abstractions;
using Pegline.Core.Domain;
using Pegline.Platform.Win32;

namespace Pegline.App.Views;

public partial class LineWindow : Window
{
    private const int MouseActivate = 0x0021;
    private const int NoActivate = 3;

    private readonly LineViewModel _model;
    private readonly IDisplayEnvironment _displays;
    private readonly Dictionary<Guid, CardMotion> _motion = new();
    private IntPtr _hwnd;
    private bool _clickThrough = true;
    private bool _allowClose;
    private bool _placing;
    private bool _clock;
    private bool _followUp;
    private TimeSpan _lastFrame;
    private Spring1D _slide = new(-LineLayout.PanelHeightFor(LineLayout.CaptionFallback), 0);
    private double _slideTarget = -LineLayout.PanelHeightFor(LineLayout.CaptionFallback);
    private double _ropeTop = LineLayout.CaptionFallback;
    private double _panelHeight = LineLayout.PanelHeightFor(LineLayout.CaptionFallback);
    private MonitorBounds? _anchor;

    public LineWindow(LineViewModel model, IDisplayEnvironment displays)
    {
        InitializeComponent();
        _model = model;
        _displays = displays;
        DataContext = model;
        model.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(LineViewModel.IsRevealed))
            {
                ApplyReveal(model.IsRevealed);
            }
        };
    }

    public MonitorBounds? Anchor
    {
        get => _anchor;
        set => _anchor = value;
    }

    public void AllowClose() => _allowClose = true;

    public void HideImmediate()
    {
        StopClock();
        _slide = new Spring1D(-_panelHeight, 0);
        _slideTarget = -_panelHeight;
        Slide.Y = -_panelHeight;
        if (IsVisible)
        {
            Hide();
        }
    }

    public bool IsInZone(MousePoint cursor)
    {
        if (!IsVisible || _anchor is not { } monitor)
        {
            return false;
        }

        var panel = Math.Max(1, DisplayScale.ToPhysical(_panelHeight, monitor.DpiY));
        return cursor.X >= monitor.Left
            && cursor.X < monitor.Left + monitor.Width
            && cursor.Y >= monitor.Top
            && cursor.Y < monitor.Top + panel;
    }

    public void UpdateClickThrough(MousePoint cursor)
    {
        if (_hwnd == IntPtr.Zero || !IsVisible)
        {
            return;
        }

        var through = !IsOverCard(cursor);
        if (through == _clickThrough)
        {
            return;
        }

        WindowPlacement.SetClickThrough(_hwnd, through);
        _clickThrough = through;
    }

    public Rect RestingScreenRect(Guid id)
    {
        var card = _model.Find(id);
        if (card is null || card.ChromeWidth < 2 || card.ChromeHeight < 2)
        {
            return Rect.Empty;
        }

        var origin = PointToScreen(new Point(card.Left, card.Top));
        return new Rect(origin.X, origin.Y, card.ChromeWidth, card.ChromeHeight);
    }

    public Task FlyAsync(ImageSource image, Rect from, Rect to, double tilt, Action? onArrive = null)
    {
        return FlightWindow.Play(image, from, to, tilt, onArrive);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwnd = new WindowInteropHelper(this).Handle;
        WindowPlacement.MakeToolWindow(_hwnd);
        _clickThrough = true;
        HwndSource.FromHwnd(_hwnd)?.AddHook(Hook);
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        if (IsVisible)
        {
            Place();
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!_allowClose)
        {
            e.Cancel = true;
            return;
        }

        StopClock();
        base.OnClosing(e);
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        DrawRope();
        _model.Layout(ActualWidth, _ropeTop);
    }

    private void CardHover(object sender, MouseEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: HangingCardViewModel card })
        {
            return;
        }

        card.Hovered = e.RoutedEvent == Mouse.MouseEnterEvent;
        StartClock();
    }

    private void CardClicked(object sender, MouseButtonEventArgs e)
    {
        if (IsInsideButton(e.OriginalSource as DependencyObject))
        {
            return;
        }

        if (sender is FrameworkElement { DataContext: HangingCardViewModel card })
        {
            _model.RequestCopy(card.Id);
        }
    }

    private void RemoveClicked(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (sender is FrameworkElement { DataContext: HangingCardViewModel card })
        {
            _model.RequestRemove(card.Id);
        }
    }

    private void ApplyReveal(bool show)
    {
        if (show)
        {
            var hidden = !IsVisible;
            if (hidden && _anchor is { } monitor)
            {
                _ropeTop = _displays.CaptionHeightDip(monitor.DpiY);
                _panelHeight = LineLayout.PanelHeightFor(_ropeTop);
                var scaleX = DisplayScale.Factor(monitor.DpiX);
                var scaleY = DisplayScale.Factor(monitor.DpiY);
                Left = monitor.Left / scaleX;
                Top = monitor.Top / scaleY;
                Width = Math.Max(1, monitor.Width / scaleX);
                Height = _panelHeight;
                _slide = new Spring1D(-_panelHeight, 0);
                Slide.Y = -_panelHeight;
            }

            if (hidden)
            {
                Show();
            }

            Place();
            if (hidden)
            {
                _slide = new Spring1D(-_panelHeight, 0);
                Slide.Y = -_panelHeight;
            }

            _slideTarget = 0;
            StartClock();
            return;
        }

        if (!IsVisible)
        {
            return;
        }

        _slideTarget = -_panelHeight;
        _slide = _slide.Kick(Math.Max(_slide.Velocity, Motion.RetractKick));
        StartClock();
    }

    private void Place()
    {
        if (_anchor is null)
        {
            return;
        }

        ApplyPlace();
        if (_followUp)
        {
            return;
        }

        _followUp = true;
        Dispatcher.BeginInvoke(() =>
        {
            _followUp = false;
            if (IsVisible)
            {
                ApplyPlace();
            }
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void ApplyPlace()
    {
        if (_placing || _anchor is not { } monitor)
        {
            return;
        }

        _placing = true;
        try
        {
            _ropeTop = _displays.CaptionHeightDip(monitor.DpiY);
            _panelHeight = LineLayout.PanelHeightFor(_ropeTop);
            if (_slideTarget < 0)
            {
                _slideTarget = -_panelHeight;
            }

            Height = _panelHeight;
            MonitorFit.Apply(this, monitor, _panelHeight, activate: false);
            RopeHost.Height = _ropeTop + LineLayout.MaxSag + 24;
            Hint.Margin = new Thickness(24, _ropeTop + 28, 24, 0);
            DrawRope();
            _model.Layout(Width, _ropeTop);
        }
        finally
        {
            _placing = false;
        }
    }

    private void DrawRope()
    {
        var width = ActualWidth > 0 ? ActualWidth : Width;
        if (width <= 0)
        {
            return;
        }

        var curve = LineLayout.Rope(width, _ropeTop);
        var geometry = new PathGeometry();
        var figure = new PathFigure
        {
            StartPoint = new Point(curve.StartX, curve.StartY),
            IsFilled = false,
        };
        figure.Segments.Add(new QuadraticBezierSegment(
            new Point(curve.ControlX, curve.ControlY),
            new Point(curve.EndX, curve.EndY),
            true));
        geometry.Figures.Add(figure);
        geometry.Freeze();
        RopeShadow.Data = geometry;
        RopeCore.Data = geometry;
        RopeHighlight.Data = geometry;
    }

    private void StartClock()
    {
        if (_clock)
        {
            return;
        }

        _clock = true;
        _lastFrame = TimeSpan.Zero;
        CompositionTarget.Rendering += OnFrame;
    }

    private void StopClock()
    {
        if (!_clock)
        {
            return;
        }

        _clock = false;
        CompositionTarget.Rendering -= OnFrame;
    }

    private void OnFrame(object? sender, EventArgs e)
    {
        if (e is not RenderingEventArgs timing)
        {
            return;
        }

        var dt = _lastFrame == TimeSpan.Zero ? 1.0 / 60 : (timing.RenderingTime - _lastFrame).TotalSeconds;
        _lastFrame = timing.RenderingTime;
        dt = Math.Clamp(dt, 0.001, 0.05);

        _slide = _slide.Step(_slideTarget, dt, Motion.Slide);
        Slide.Y = _slide.Value;
        var lag = Math.Clamp(-_slide.Velocity * Motion.SwingLag, -Motion.MaxLagSwing, Motion.MaxLagSwing);
        var busy = !_slide.IsResting(_slideTarget, Motion.Slide);

        var live = new HashSet<Guid>();
        foreach (var card in _model.Cards)
        {
            live.Add(card.Id);
            if (!_motion.TryGetValue(card.Id, out var motion))
            {
                motion = new CardMotion
                {
                    Scale = new Spring1D(1, 0),
                    Shadow = new Spring1D(0, 0),
                };
                _motion[card.Id] = motion;
            }

            if (card.ShiftKickX != 0 || card.ShiftKickY != 0)
            {
                motion.ShiftX = new Spring1D(motion.ShiftX.Value + card.ShiftKickX, motion.ShiftX.Velocity);
                motion.ShiftY = new Spring1D(motion.ShiftY.Value + card.ShiftKickY, motion.ShiftY.Velocity);
                card.ShiftKickX = 0;
                card.ShiftKickY = 0;
            }

            if (card.SwingKick != 0)
            {
                motion.Swing = new Spring1D(card.SwingKick, motion.Swing.Velocity);
                card.SwingKick = 0;
            }

            var swingTarget = lag + (card.Hovered ? Motion.HoverSwing : 0);
            motion.Swing = motion.Swing.Step(swingTarget, dt, Motion.Swing);
            var scaleTarget = card.Hovered ? Motion.HoverScale : 1;
            motion.Scale = motion.Scale.Step(scaleTarget, dt, Motion.Hover);
            motion.ShiftX = motion.ShiftX.Step(0, dt, Motion.Shift);
            motion.ShiftY = motion.ShiftY.Step(0, dt, Motion.Shift);
            motion.Close = motion.Close.Step(card.Hovered ? 1 : 0, dt, Motion.Fade);
            motion.Shadow = motion.Shadow.Step(card.Hovered ? 1 : 0, dt, Motion.Hover);

            card.VisualAngle = card.Tilt + motion.Swing.Value;
            card.HoverScale = motion.Scale.Value;
            card.ShiftX = motion.ShiftX.Value;
            card.ShiftY = motion.ShiftY.Value;
            card.CloseOpacity = motion.Close.Value;
            card.ShadowBlur = 12 + 8 * motion.Shadow.Value;
            card.ShadowDepth = 5 + 3 * motion.Shadow.Value;
            card.ShadowOpacity = 0.22 + 0.16 * motion.Shadow.Value;

            busy |= card.Hovered
                || !motion.Swing.IsResting(swingTarget, Motion.Swing)
                || !motion.Scale.IsResting(scaleTarget, Motion.Hover)
                || !motion.ShiftX.IsResting(0, Motion.Shift)
                || !motion.ShiftY.IsResting(0, Motion.Shift)
                || !motion.Close.IsResting(card.Hovered ? 1 : 0, Motion.Fade);
        }

        foreach (var id in _motion.Keys.Where(id => !live.Contains(id)).ToArray())
        {
            _motion.Remove(id);
        }

        if (_slideTarget < 0 && _slide.IsResting(_slideTarget, Motion.Slide) && !_model.IsRevealed && IsVisible)
        {
            Hide();
            busy = false;
        }

        if (!busy)
        {
            StopClock();
        }
    }

    private bool IsOverCard(MousePoint cursor)
    {
        if (!WindowPlacement.TryClientPoint(_hwnd, cursor, out var x, out var y))
        {
            return false;
        }

        var dpi = VisualTreeHelper.GetDpi(this);
        var hit = InputHitTest(new Point(x / dpi.DpiScaleX, y / dpi.DpiScaleY)) as DependencyObject;
        while (hit is not null)
        {
            if (hit is FrameworkElement { DataContext: HangingCardViewModel })
            {
                return true;
            }

            hit = VisualTreeHelper.GetParent(hit);
        }

        return false;
    }

    private IntPtr Hook(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message != MouseActivate)
        {
            return IntPtr.Zero;
        }

        handled = true;
        return new IntPtr(NoActivate);
    }

    private static bool IsInsideButton(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is Button)
            {
                return true;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return false;
    }

    private sealed class CardMotion
    {
        public Spring1D Swing;
        public Spring1D Scale;
        public Spring1D ShiftX;
        public Spring1D ShiftY;
        public Spring1D Close;
        public Spring1D Shadow;
    }
}
