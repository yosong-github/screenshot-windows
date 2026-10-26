using ScreenshotWindows.Core.Domain;

namespace ScreenshotWindows.Core.Tests;

public class DominantColorTests
{
    [Fact]
    public void SolidRed_StaysRed_AndThePegKeepsAHighlight()
    {
        var color = DominantColor.Extract(Fill(16, 16, 10, 10, 220), 16, 16, 16 * 4);
        Assert.NotNull(color);
        Assert.True(color.Value.R > color.Value.G + 40);
        Assert.True(color.Value.R > color.Value.B + 40);

        var peg = PegPalette.FromAccent(color.Value);
        Assert.True(peg.Highlight.Luminance > peg.Body.Luminance);
        Assert.True(peg.Body.Luminance > peg.Shadow.Luminance);
        Assert.True(peg.Body.R > peg.Body.G);
        Assert.True(peg.Body.R > peg.Body.B);
        var body = Hsl.FromRgb(peg.Body);
        Assert.InRange(body.S, 0.35, 0.78);
        Assert.InRange(body.L, 0.38, 0.62);
    }

    [Fact]
    public void TwoTone_PicksTheLargerRegion()
    {
        var pixels = new byte[48 * 24 * 4];
        for (var y = 0; y < 24; y++)
        {
            for (var x = 0; x < 48; x++)
            {
                var i = (y * 48 + x) * 4;
                if (x < 36)
                {
                    Paint(pixels, i, 220, 40, 20);
                }
                else
                {
                    Paint(pixels, i, 20, 120, 220);
                }
            }
        }

        var color = DominantColor.Extract(pixels, 48, 24, 48 * 4);
        Assert.NotNull(color);
        Assert.True(color.Value.B > color.Value.R);
        Assert.True(color.Value.B > color.Value.G);
    }

    [Fact]
    public void MostlyWhite_UsesTheColoredRegion()
    {
        var pixels = Fill(64, 64, 250, 250, 250);
        for (var y = 24; y < 40; y++)
        {
            for (var x = 24; x < 40; x++)
            {
                Paint(pixels, (y * 64 + x) * 4, 40, 180, 30);
            }
        }

        var color = DominantColor.Extract(pixels, 64, 64, 64 * 4);
        Assert.NotNull(color);
        Assert.True(color.Value.G > color.Value.R + 40);
        Assert.True(color.Value.G > color.Value.B + 40);
    }

    [Fact]
    public void Grayscale_AndEmpty_FallBack()
    {
        Assert.Null(DominantColor.Extract(Fill(32, 32, 128, 128, 128), 32, 32, 32 * 4));
        Assert.Null(DominantColor.Extract(Fill(16, 16, 250, 250, 250), 16, 16, 16 * 4));
        Assert.Null(DominantColor.Extract(Fill(16, 16, 4, 4, 4), 16, 16, 16 * 4));
        Assert.Null(DominantColor.Extract(Fill(8, 8, 0, 0, 220, 0), 8, 8, 8 * 4));
        Assert.Null(DominantColor.Extract(Array.Empty<byte>(), 0, 0, 0));
    }

    [Fact]
    public void TransparentPixels_AreIgnored()
    {
        var pixels = Fill(24, 16, 0, 0, 220, 0);
        for (var y = 0; y < 16; y++)
        {
            for (var x = 0; x < 8; x++)
            {
                Paint(pixels, (y * 24 + x) * 4, 220, 30, 20, 255);
            }
        }

        var color = DominantColor.Extract(pixels, 24, 16, 24 * 4);
        Assert.NotNull(color);
        Assert.True(color.Value.B > color.Value.R);
    }

    [Fact]
    public void PaletteLerp_HoldsTheEndpoints()
    {
        var accent = PegPalette.FromAccent(new Rgb(20, 140, 40));
        var start = PegPalette.Lerp(PegPalette.Metal, accent, 0);
        var end = PegPalette.Lerp(PegPalette.Metal, accent, 1);
        Assert.Equal(PegPalette.Metal.Body, start.Body);
        Assert.Equal(accent.Body, end.Body);
        Assert.True(PegPalette.Metal.Highlight.Luminance > PegPalette.Metal.Shadow.Luminance);
    }

    private static byte[] Fill(int width, int height, byte b, byte g, byte r, byte a = 255)
    {
        var pixels = new byte[width * height * 4];
        for (var i = 0; i < pixels.Length; i += 4)
        {
            Paint(pixels, i, b, g, r, a);
        }

        return pixels;
    }

    private static void Paint(byte[] pixels, int i, byte b, byte g, byte r, byte a = 255)
    {
        pixels[i] = b;
        pixels[i + 1] = g;
        pixels[i + 2] = r;
        pixels[i + 3] = a;
    }
}
