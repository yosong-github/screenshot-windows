using Pegline.Core.Domain;

namespace Pegline.Core.Tests;

public class LineLayoutTests
{
    [Fact]
    public void Rope_SagsInTheMiddle()
    {
        Assert.Equal(10d, LineLayout.RopeY(0, 1000));
        Assert.InRange(LineLayout.RopeY(500, 1000), 27.9, 28.1);
        Assert.Equal(10d, LineLayout.RopeY(1000, 1000));

        var curve = LineLayout.Rope(1000);
        Assert.Equal(500d, curve.ControlX);
        Assert.InRange(curve.ControlY, 45.9, 46.1);
    }

    [Fact]
    public void Items_AreCentred_UntilTheyMustSqueeze()
    {
        Assert.Equal(500, LineLayout.ItemX(0, 1, 1000, LineLayout.Spacing), 0);
        Assert.Equal(326, LineLayout.ItemX(0, 3, 1000, LineLayout.Spacing), 0);
        Assert.Equal(500, LineLayout.ItemX(1, 3, 1000, LineLayout.Spacing), 0);
        Assert.Equal(674, LineLayout.ItemX(2, 3, 1000, LineLayout.Spacing), 0);

        var squeezed = LineLayout.ItemX(0, 4, 400, LineLayout.Spacing);
        Assert.InRange(squeezed, 99, 101);
    }

    [Fact]
    public void Photo_KeepsItsAspectInsideTheFrame()
    {
        var (width, height) = LineLayout.PhotoSize(1920, 1080);
        Assert.Equal(LineLayout.CardWidth - LineLayout.FrameInset * 2, width, 0);
        Assert.InRange(height, 79, 81);
    }

    [Fact]
    public void Frame_HangsBelowAVisibleClip()
    {
        Assert.True(LineLayout.ClipHeight >= 36);
        Assert.True(LineLayout.ClipGap >= 8);
        Assert.Equal(LineLayout.ClipHeight + LineLayout.ClipGap, LineLayout.FrameTop);

        const double rope = 40;
        var frame = LineLayout.CardTop(rope) + LineLayout.FrameTop;
        Assert.True(frame > rope + 20);
        var panel = LineLayout.PanelHeightFor(rope);
        Assert.True(panel > frame + LineLayout.PhotoMaxHeight);
    }

    [Fact]
    public void CaptionFallback_LiftsTheRope_AndThePanelCoversTheGap()
    {
        Assert.Equal(32, LineLayout.CaptionFallback);
        Assert.Equal(LineLayout.CaptionFallback, CaptionBand.FallbackDip);
        Assert.Equal(LineLayout.CaptionFallback, LineLayout.RopeY(0, 1000, LineLayout.CaptionFallback));
        var panel = LineLayout.PanelHeightFor(LineLayout.CaptionFallback);
        Assert.True(panel > LineLayout.CaptionFallback + LineLayout.FrameTop + LineLayout.PhotoMaxHeight);
    }

    [Fact]
    public void TopBand_GrowsWithDpi()
    {
        var low = new MonitorBounds("DISPLAY", 0, 0, 1920, 1080, 96, 96);
        var high = low with { DpiY = 192 };
        Assert.Equal(18, low.TopBandPixels);
        Assert.Equal(36, high.TopBandPixels);
        Assert.True(low.InTopBand(10, 17));
        Assert.False(low.InTopBand(10, 18));
        Assert.False(low.InTopBand(-1, 0));
    }
}
