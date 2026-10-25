using Pegline.Core.Domain;

namespace Pegline.Core.Tests;

public class BgraBufferTests
{
    [Fact]
    public void FlipVertical_SwapsRows()
    {
        var pixels = new byte[] { 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3 };
        BgraBuffer.FlipVertical(pixels, stride: 4, height: 3);
        Assert.Equal(new byte[] { 3, 3, 3, 3, 2, 2, 2, 2, 1, 1, 1, 1 }, pixels);
    }

    [Fact]
    public void ForceOpaque_SetsEveryAlphaByte()
    {
        var pixels = new byte[] { 9, 8, 7, 0, 1, 2, 3, 4 };
        BgraBuffer.ForceOpaque(pixels);
        Assert.Equal(255, pixels[3]);
        Assert.Equal(255, pixels[7]);
        Assert.Equal(9, pixels[0]);
    }
}
