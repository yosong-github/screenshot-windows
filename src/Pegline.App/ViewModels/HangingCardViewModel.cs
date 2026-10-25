using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Pegline.Core.Domain;

namespace Pegline.App.ViewModels;

public partial class HangingCardViewModel : ObservableObject
{
    public HangingCardViewModel(HangingItem item, ImageSource image)
    {
        Id = item.Id;
        Path = item.Path;
        Tilt = item.TiltDegrees;
        Image = image;
        PixelWidth = item.PixelWidth;
        PixelHeight = item.PixelHeight;
        VisualAngle = item.TiltDegrees;
    }

    public Guid Id { get; }

    public string Path { get; }

    public double Tilt { get; }

    public ImageSource Image { get; }

    public int PixelWidth { get; }

    public int PixelHeight { get; }

    [ObservableProperty] private double _left;

    [ObservableProperty] private double _top;

    [ObservableProperty] private double _photoWidth = 142;

    [ObservableProperty] private double _photoHeight = 104;

    [ObservableProperty] private double _chromeWidth = 150;

    [ObservableProperty] private double _chromeHeight = 160;

    [ObservableProperty] private double _clipLeft;

    [ObservableProperty] private double _frameTop = LineLayout.FrameTop;

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

    public bool Hovered { get; set; }

    public bool Placed { get; set; }

    public double ShiftKickX { get; set; }

    public double ShiftKickY { get; set; }

    public double SwingKick { get; set; }
}
