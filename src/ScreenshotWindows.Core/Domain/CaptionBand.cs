namespace ScreenshotWindows.Core.Domain;

/// <summary>
/// How far below the top of the monitor the rope rests: the caption of a maximized window.
/// The three inputs are what <c>GetSystemMetricsForDpi</c> returns for
/// <c>SM_CYCAPTION</c>, <c>SM_CYFRAME</c>, and <c>SM_CXPADDEDBORDER</c>.
/// </summary>
public static class CaptionBand
{
    public const double FallbackDip = LineLayout.CaptionFallback;

    public static double FromPhysical(int caption, int frame, int paddedBorder, uint dpi)
    {
        var sum = caption + frame + paddedBorder;
        if (sum <= 0)
        {
            return FallbackDip;
        }

        var dip = DisplayScale.ToDip(sum, dpi);
        return dip < 16 ? FallbackDip : dip;
    }
}
