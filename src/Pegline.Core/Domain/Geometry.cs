namespace Pegline.Core.Domain;

public readonly record struct MousePoint(int X, int Y);

/// <summary>A monitor rectangle in physical pixels, virtual-screen coordinates.</summary>
public readonly record struct MonitorBounds(
    string DeviceName,
    int Left,
    int Top,
    int Width,
    int Height,
    uint DpiX,
    uint DpiY)
{
    public bool Contains(int x, int y) =>
        x >= Left && x < Left + Width && y >= Top && y < Top + Height;

    /// <summary>About 18 device-independent pixels, so the top edge is easy to rest on at any scale.</summary>
    public int TopBandPixels => Math.Max(8, (int)Math.Round(18 * (DpiY / 96.0)));

    public bool InTopBand(int x, int y) =>
        x >= Left && x < Left + Width && y >= Top && y < Top + TopBandPixels;
}

public readonly record struct PixelRect(int X, int Y, int Width, int Height)
{
    public bool IsEmpty => Width <= 0 || Height <= 0;
}

public readonly record struct RopeCurve(
    double StartX,
    double StartY,
    double ControlX,
    double ControlY,
    double EndX,
    double EndY);
