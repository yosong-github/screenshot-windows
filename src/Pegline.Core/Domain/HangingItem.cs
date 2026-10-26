namespace Pegline.Core.Domain;

public sealed class HangingItem
{
    public HangingItem(Guid id, string path, int pixelWidth, int pixelHeight, double tiltDegrees, bool accentKnown = false, int? accentRgb = null)
    {
        Id = id;
        Path = path;
        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
        TiltDegrees = tiltDegrees;
        AccentKnown = accentKnown;
        AccentRgb = accentRgb;
    }

    public Guid Id { get; }

    public string Path { get; }

    public int PixelWidth { get; }

    public int PixelHeight { get; }

    public double TiltDegrees { get; }

    /// <summary>True once the peg color has been chosen, including the metal fallback.</summary>
    public bool AccentKnown { get; private set; }

    /// <summary>Packed RGB accent, or null when the peg stays the default metal.</summary>
    public int? AccentRgb { get; private set; }

    public void RememberAccent(int? rgb)
    {
        AccentKnown = true;
        AccentRgb = rgb;
    }
}
