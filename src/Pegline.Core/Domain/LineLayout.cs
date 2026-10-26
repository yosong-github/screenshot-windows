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

    /// <summary>
    /// Resting gap when caption metrics are unavailable. About the title-bar height
    /// of a maximized window at 100% (SM_CYCAPTION + SM_CYFRAME + SM_CXPADDEDBORDER).
    /// </summary>
    public const double CaptionFallback = 32;
    public const double MinTilt = -2.5;
    public const double MaxTilt = 2.5;
    public const double MaxSag = 30;

    /// <summary>Aluminium clip. The top grips the rope; the jaws overlap the frame.</summary>
    public const double ClipWidth = 16;

    public const double ClipHeight = 42;

    /// <summary>How far the top of the clip sits above the rope. The swing pivot is this grip.</summary>
    public const double ClipOverhang = 10;

    /// <summary>How far the jaws cover the top edge of the frame.</summary>
    public const double JawOverlap = 10;

    public const double FrameRadius = 12;

    public const double FrameBorder = 1;

    /// <summary>Inner corner of the photo: the frame radius minus the white border and padding.</summary>
    public const double PhotoRadius = FrameRadius - FrameInset - FrameBorder;

    public const double ShadowRoom = 40;

    /// <summary>Y of the frame inside the pegged view. The clip continues past this edge.</summary>
    public static double FrameTop => ClipHeight - JawOverlap;

    /// <summary>
    /// Rotation center of the frame, relative to the frame's top-left.
    /// Negative: the grip is above the frame, on the rope.
    /// </summary>
    public static double FramePivotOffset => ClipOverhang - FrameTop;

    public static double CardTop(double ropeY) => ropeY - ClipOverhang;

    public static double Sag(double width) => Math.Min(MaxSag, Math.Max(0, width) * 0.018);

    public static double RopeY(double x, double width, double ropeTop = RopeTop)
    {
        if (width <= 0)
        {
            return ropeTop;
        }

        var f = x / width;
        return ropeTop + 4 * Sag(width) * f * (1 - f);
    }

    public static RopeCurve Rope(double width, double ropeTop = RopeTop)
    {
        var sag = Sag(width);
        return new RopeCurve(-20, ropeTop, width / 2, ropeTop + 2 * sag, width + 20, ropeTop);
    }

    /// <summary>
    /// Window height for a rope whose resting top is <paramref name="ropeTop"/> DIPs
    /// below the top of the monitor. The caption gap is included, so the hit area
    /// covers the space between the screen edge and the photos.
    /// </summary>
    public static double PanelHeightFor(double ropeTop)
    {
        var lowestRope = ropeTop + MaxSag;
        var cardTop = CardTop(lowestRope);
        return cardTop + FrameTop + PhotoMaxHeight + FrameInset * 2 + ShadowRoom;
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
