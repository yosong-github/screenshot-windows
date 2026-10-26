using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using ScreenshotWindows.Core.Domain;

namespace ScreenshotWindows.App.ViewModels;

public partial class HangingCardViewModel : ObservableObject
{
    private PegPalette _tintFrom = PegPalette.Metal;
    private PegPalette _tintTo = PegPalette.Metal;
    private double _tint = 1;

    public HangingCardViewModel(HangingItem item, ImageSource image)
    {
        Id = item.Id;
        Path = item.Path;
        Tilt = item.TiltDegrees;
        Image = image;
        PhotoBrush = new ImageBrush(image)
        {
            Stretch = Stretch.UniformToFill,
            AlignmentX = AlignmentX.Center,
            AlignmentY = AlignmentY.Center,
        };
        RenderOptions.SetBitmapScalingMode(PhotoBrush, BitmapScalingMode.HighQuality);
        PixelWidth = item.PixelWidth;
        PixelHeight = item.PixelHeight;
        VisualAngle = item.TiltDegrees;
        PivotY = LineLayout.ClipOverhang;
        Publish(PegPalette.Metal);
        if (item.AccentKnown)
        {
            SetAccent(item.AccentRgb, animate: false);
        }
    }

    public Guid Id { get; }

    public string Path { get; }

    public double Tilt { get; }

    public ImageSource Image { get; }

    /// <summary>
    /// Drawn as the frame's background so the rounded corner clips the pixels.
    /// A child Image is only clipped to the rectangular slot.
    /// </summary>
    public ImageBrush PhotoBrush { get; }

    public int PixelWidth { get; }

    public int PixelHeight { get; }

    public Thickness FramePadding { get; } = new(LineLayout.FrameInset);

    public CornerRadius FrameCorner { get; } = new(LineLayout.FrameRadius);

    public CornerRadius PhotoCorner { get; } = new(LineLayout.PhotoRadius);

    [ObservableProperty] private double _left;

    [ObservableProperty] private double _top;

    [ObservableProperty] private double _photoWidth = 142;

    [ObservableProperty] private double _photoHeight = 104;

    [ObservableProperty] private double _chromeWidth = 150;

    [ObservableProperty] private double _chromeHeight = 160;

    [ObservableProperty] private double _frameHeight = 112;

    [ObservableProperty] private double _clipLeft;

    [ObservableProperty] private double _frameTop = LineLayout.FrameTop;

    [ObservableProperty] private double _pivotX = 75;

    [ObservableProperty] private double _pivotY = LineLayout.ClipOverhang;

    [ObservableProperty] private bool _copied;

    [ObservableProperty] private bool _flying;

    [ObservableProperty] private double _opacity = 1;

    [ObservableProperty] private double _hoverScale = 1;

    [ObservableProperty] private double _visualAngle;

    [ObservableProperty] private double _shiftX;

    [ObservableProperty] private double _shiftY;

    [ObservableProperty] private double _shadowBlur = 12;

    [ObservableProperty] private double _shadowDepth = 5;

    [ObservableProperty] private double _shadowOpacity = 0.22;

    [ObservableProperty] private double _closeOpacity;

    [ObservableProperty] private double _cacheScale = 1;

    [ObservableProperty] private Color _clipEdge;

    [ObservableProperty] private Color _clipShine;

    [ObservableProperty] private Color _clipBody;

    public bool Hovered { get; set; }

    public bool Placed { get; set; }

    public double ShiftKickX { get; set; }

    public double ShiftKickY { get; set; }

    public double SwingKick { get; set; }

    public bool SwingArm { get; set; }

    public void SetAccent(int? rgb, bool animate)
    {
        var target = rgb is int packed ? PegPalette.FromAccent(Rgb.Unpack(packed)) : PegPalette.Metal;
        if (!animate)
        {
            _tintFrom = target;
            _tintTo = target;
            _tint = 1;
            Publish(target);
            return;
        }

        _tintFrom = PegPalette.Lerp(_tintFrom, _tintTo, Easing.SmoothStep(_tint));
        _tintTo = target;
        _tint = 0;
    }

    public bool AdvanceTint(double dt)
    {
        if (_tint >= 1)
        {
            return false;
        }

        var duration = Motion.TintFade.TotalSeconds;
        _tint = duration <= 0 ? 1 : Math.Min(1, _tint + dt / duration);
        Publish(PegPalette.Lerp(_tintFrom, _tintTo, Easing.SmoothStep(_tint)));
        return _tint < 1;
    }

    private void Publish(PegPalette palette)
    {
        ClipEdge = ToColor(palette.Shadow);
        ClipBody = ToColor(palette.Body);
        ClipShine = ToColor(palette.Highlight);
    }

    private static Color ToColor(Rgb rgb) => Color.FromRgb(rgb.R, rgb.G, rgb.B);
}
