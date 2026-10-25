namespace Pegline.Core.Domain;

/// <summary>
/// Converts between physical pixels and device-independent pixels.
/// The monitor's own DPI is the only scale. A window's layout size is
/// <see cref="ToDip"/> of its physical size, and the HWND stays in physical pixels.
/// </summary>
public static class DisplayScale
{
    public const double BaselineDpi = 96;

    public static double Factor(uint dpi) => (dpi == 0 ? BaselineDpi : dpi) / BaselineDpi;

    public static double ToDip(int physical, uint dpi) => physical / Factor(dpi);

    public static int ToPhysical(double dip, uint dpi) => (int)Math.Round(dip * Factor(dpi));
}
