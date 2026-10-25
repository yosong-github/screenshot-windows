using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Pegline.App.Session;
using Pegline.App.ViewModels;
using Pegline.Core.Domain;
using Pegline.Platform.Win32;

namespace Pegline.App.Views;

public partial class LineWindow : Window
{
    private const int MouseActivate = 0x0021;
    private const int NoActivate = 3;

    private readonly LineViewModel _model;
    private IntPtr _hwnd;
    private bool _clickThrough = true;
    private bool _allowClose;
    private bool _placing;
    private int _revealGeneration;
    private MonitorBounds? _anchor;

    public LineWindow(LineViewModel model)
    {
        InitializeComponent();
        _model = model;
        DataContext = model;
        model.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(LineViewModel.IsRevealed))
            {
                ApplyReveal(model.IsRevealed);
            }
        };
        DpiChanged += (_, _) =>
        {
            if (IsVisible)
            {
                Place();
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
        _revealGeneration++;
        Slide.BeginAnimation(TranslateTransform.YProperty, null);
        Slide.Y = -LineLayout.PanelHeight;
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

        var panel = (int)Math.Round(LineLayout.PanelHeight * (monitor.DpiY / 96.0));
        return cursor.X >= monitor.Left
            && cursor.X < monitor.Left + monitor.Width
            && cursor.Y >= monitor.Top
            && cursor.Y < monitor.Top + Math.Max(panel, 1);
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

    public async Task FlyAsync(ImageSource image, Rect from, Guid id)
    {
        try
        {
            UpdateLayout();
            _model.Layout(Width);
            var to = CardScreenRect(id);
            if (to is { } destination)
            {
                await FlightWindow.Play(image, from, destination);
            }
        }
        finally
        {
            _model.MarkLanded(id);
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwnd = new WindowInteropHelper(this).Handle;
        WindowPlacement.MakeToolWindow(_hwnd);
        _clickThrough = true;
        HwndSource.FromHwnd(_hwnd)?.AddHook(Hook);
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!_allowClose)
        {
            e.Cancel = true;
            return;
        }

        base.OnClosing(e);
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        DrawRope();
        _model.Layout(ActualWidth);
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
        var generation = ++_revealGeneration;
        if (show)
        {
            if (!IsVisible && _anchor is { } monitor)
            {
                var scaleX = Math.Max(1, monitor.DpiX) / 96.0;
                var scaleY = Math.Max(1, monitor.DpiY) / 96.0;
                Left = monitor.Left / scaleX;
                Top = monitor.Top / scaleY;
                Width = Math.Max(1, monitor.Width / scaleX);
                Height = LineLayout.PanelHeight;
            }

            if (!IsVisible)
            {
                Show();
            }

            Place();
            Slide.BeginAnimation(TranslateTransform.YProperty, null);
            Slide.Y = -LineLayout.PanelHeight;
            Slide.BeginAnimation(TranslateTransform.YProperty, SlideTo(0, 240, EasingMode.EaseOut));
            return;
        }

        if (!IsVisible)
        {
            return;
        }

        var hide = SlideTo(-LineLayout.PanelHeight, 180, EasingMode.EaseIn);
        hide.Completed += (_, _) =>
        {
            if (generation == _revealGeneration && !_model.IsRevealed && IsVisible)
            {
                Hide();
            }
        };
        Slide.BeginAnimation(TranslateTransform.YProperty, hide);
    }

    private void Place()
    {
        if (_placing || _anchor is not { } monitor)
        {
            return;
        }

        _placing = true;
        try
        {
            MonitorFit.Apply(this, monitor, LineLayout.PanelHeight, activate: false);
            DrawRope();
            _model.Layout(Width);
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

        var curve = LineLayout.Rope(width);
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

    private Rect? CardScreenRect(Guid id)
    {
        for (var index = 0; index < CardList.Items.Count; index++)
        {
            if (CardList.ItemContainerGenerator.ContainerFromIndex(index) is not ContentPresenter presenter)
            {
                continue;
            }

            if (presenter.DataContext is not HangingCardViewModel card || card.Id != id)
            {
                continue;
            }

            presenter.UpdateLayout();
            if (presenter.ActualWidth < 2 || presenter.ActualHeight < 2)
            {
                return null;
            }

            var origin = presenter.PointToScreen(new Point(0, 0));
            // PointToScreen includes the glide transform. Land on the resting slot.
            return new Rect(origin.X, origin.Y - Slide.Y, presenter.ActualWidth, presenter.ActualHeight);
        }

        return null;
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

    private static DoubleAnimation SlideTo(double to, int milliseconds, EasingMode mode) =>
        new(to, TimeSpan.FromMilliseconds(milliseconds))
        {
            EasingFunction = new QuadraticEase { EasingMode = mode },
        };

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
}
