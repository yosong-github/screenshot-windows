using Pegline.Core.Domain;

namespace Pegline.Core.Tests;

public class DisplayScaleTests
{
    [Theory]
    [InlineData(96u)]
    [InlineData(120u)]
    [InlineData(144u)]
    [InlineData(168u)]
    [InlineData(192u)]
    public void DipSlot_CoversThePhysicalMonitor(uint dpi)
    {
        const int physical = 2560;
        var dip = DisplayScale.ToDip(physical, dpi);
        Assert.Equal(physical, CaptureMapping.ToPixel(dip, dip, physical));
        Assert.Equal(0, CaptureMapping.ToPixel(0, dip, physical));
        Assert.InRange(DisplayScale.ToPhysical(dip, dpi), physical - 1, physical + 1);
    }

    [Fact]
    public void OneHundredAndTwentyFivePercent_IsFourFifthsOfThePixels()
    {
        Assert.Equal(1.25, DisplayScale.Factor(120), 3);
        Assert.Equal(2048, DisplayScale.ToDip(2560, 120), 3);
        Assert.Equal(1152, DisplayScale.ToDip(1440, 120), 3);
        Assert.Equal(2560, DisplayScale.ToPhysical(2048, 120));
    }

    [Fact]
    public void Caption_UsesTheSystemMetrics_OrTheFallback()
    {
        Assert.Equal(CaptionBand.FallbackDip, CaptionBand.FromPhysical(0, 0, 0, 96));
        Assert.Equal(35, CaptionBand.FromPhysical(23, 8, 4, 96), 3);
        Assert.Equal(35, CaptionBand.FromPhysical(46, 16, 8, 192), 3);
        Assert.Equal(CaptionBand.FallbackDip, CaptionBand.FromPhysical(1, 0, 0, 96));
    }
}
