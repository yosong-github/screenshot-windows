namespace Pegline.Core.Domain;

/// <summary>
/// Where the rope sits and where each card hangs. Proportions follow the clothesline interaction
/// of the MIT-licensed macOS project this app reimplements; the drawing is original.
/// </summary>
public static class LineLayout
{
    public const double PhotoMaxHeight = 104;
    public const double CardWidth = 150;
    public const double FrameInset = 4;
    public const double Spacing = CardWidth + 24;
    public const double RopeTop = 10;
    public const double PanelHeight = PhotoMaxHeight + 106;
    public const double MinTilt = -2.5;
    public const double MaxTilt = 2.5;

    public static double Sag(double width) => Math.Min(30, Math.Max(0, width) * 0.018);

    public static double RopeY(double x, double width)
    {
        if (width <= 0)
        {
            return RopeTop;
        }

        var f = x / width;
        return RopeTop + 4 * Sag(width) * f * (1 - f);
    }

    public static RopeCurve Rope(double width)
    {
        var sag = Sag(width);
        return new RopeCurve(-20, RopeTop, width / 2, RopeTop + 2 * sag, width + 20, RopeTop);
    }

    /// <summary>
    /// Cards are centred. When they would pass the edges they step closer together.
    /// </summary>
    public static double ItemX(int index, int count, double width, double spacing)
    {
        var gaps = Math.Max(count - 1, 0);
        var step = gaps > 0 ? Math.Min(spacing, Math.Max(0, width - 200) / gaps) : spacing;
        return width / 2 - gaps * step / 2.0 + index * step;
    }

    public static (double Width, double Height) PhotoSize(int pixelWidth, int pixelHeight)
    {
        var maxW = CardWidth - FrameInset * 2;
        var maxH = PhotoMaxHeight;
        if (pixelWidth <= 0 || pixelHeight <= 0)
        {
            return (maxW, maxH);
        }

        var scale = Math.Min(maxW / pixelWidth, maxH / pixelHeight);
        return (pixelWidth * scale, pixelHeight * scale);
    }
}
