using ScreenshotWindows.Core.Domain;

namespace ScreenshotWindows.Core.Tests;

public class CaptureMappingTests
{
    [Fact]
    public void ToPixel_ScalesTheViewOntoTheBitmap()
    {
        // A 1920-wide bitmap shown in 960 DIPs: the midpoint is pixel 960.
        Assert.Equal(960, CaptureMapping.ToPixel(480, viewLength: 960, pixels: 1920));
        Assert.Equal(1920, CaptureMapping.ToPixel(960, viewLength: 960, pixels: 1920));
        Assert.Equal(0, CaptureMapping.ToPixel(-10, viewLength: 960, pixels: 1920));
    }

    [Fact]
    public void FromPoints_NormalizesAndClamps()
    {
        var dragged = CaptureMapping.FromPoints(80, 40, 10, 10, 200, 100);
        Assert.Equal(new PixelRect(10, 10, 70, 30), dragged);

        var past = CaptureMapping.FromPoints(-5, -5, 250, 150, 200, 100);
        Assert.Equal(new PixelRect(0, 0, 200, 100), past);
    }

    [Fact]
    public void Crop_RejectsATinySelection_AndClampsTheRegion()
    {
        Assert.False(CaptureCrop.CanEncode(new PixelRect(0, 0, 1, 40)));
        Assert.True(CaptureCrop.CanEncode(CaptureCrop.FullFrame(1920, 1080)));

        var clamped = CaptureCrop.Clamp(new PixelRect(-10, 5, 100, 100), 50, 40);
        Assert.Equal(new PixelRect(0, 5, 50, 35), clamped);
    }

    [Fact]
    public void ViewRoundTrip_StaysCloseToThePixel()
    {
        const int pixels = 2560;
        const double view = 1280;
        var pixel = CaptureMapping.ToPixel(400, view, pixels);
        var back = CaptureMapping.ToView(pixel, pixels, view);
        Assert.InRange(back, 399, 401);
    }
}
