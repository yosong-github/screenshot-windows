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

    [ObservableProperty] private bool _copied;

    [ObservableProperty] private bool _flying;

    [ObservableProperty] private double _opacity = 1;
}
