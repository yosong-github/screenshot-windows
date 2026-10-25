namespace Pegline.Core.Domain;

/// <summary>
/// Maps a drag on the frozen preview onto the bitmap's physical pixels.
/// The preview is stretched over the monitor, so the ratio — not the DIP coordinate — is the scale.
/// </summary>
public static class CaptureMapping
{
    public static int ToPixel(double position, double viewLength, int pixels)
    {
        if (viewLength <= 0 || pixels <= 0)
        {
            return 0;
        }

        var value = (int)Math.Round(position / viewLength * pixels);
        return Math.Clamp(value, 0, pixels);
    }

    public static double ToView(int pixel, int pixels, double viewLength)
    {
        if (pixels <= 0)
        {
            return 0;
        }

        return pixel * viewLength / pixels;
    }

    public static PixelRect FromPoints(int x1, int y1, int x2, int y2, int maxWidth, int maxHeight)
    {
        var left = Math.Clamp(Math.Min(x1, x2), 0, Math.Max(0, maxWidth));
        var top = Math.Clamp(Math.Min(y1, y2), 0, Math.Max(0, maxHeight));
        var right = Math.Clamp(Math.Max(x1, x2), 0, Math.Max(0, maxWidth));
        var bottom = Math.Clamp(Math.Max(y1, y2), 0, Math.Max(0, maxHeight));
        return new PixelRect(left, top, right - left, bottom - top);
    }
}

public static class CaptureCrop
{
    public const int MinimumEdge = 2;

    public static PixelRect Clamp(PixelRect region, int pixelWidth, int pixelHeight)
    {
        var width = Math.Max(0, pixelWidth);
        var height = Math.Max(0, pixelHeight);
        var x = Math.Clamp(region.X, 0, width);
        var y = Math.Clamp(region.Y, 0, height);
        var w = Math.Clamp(region.Width, 0, width - x);
        var h = Math.Clamp(region.Height, 0, height - y);
        return new PixelRect(x, y, w, h);
    }

    public static PixelRect FullFrame(int pixelWidth, int pixelHeight) =>
        new(0, 0, Math.Max(0, pixelWidth), Math.Max(0, pixelHeight));

    public static bool CanEncode(PixelRect region) =>
        region.Width >= MinimumEdge && region.Height >= MinimumEdge;
}
