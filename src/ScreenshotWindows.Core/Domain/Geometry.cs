namespace ScreenshotWindows.Core.Domain;

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

    /// <summary>A few physical pixels at the top edge. The band does not grow with DPI, so a title bar is not the edge.</summary>
    public int TopBandPixels => LineLayout.EdgeBandPixels;

    public bool InTopBand(int x, int y) =>
        x >= Left && x < Left + Width && y >= Top && y < Top + TopBandPixels;
}

public readonly record struct PixelRect(int X, int Y, int Width, int Height)
{
    public bool IsEmpty => Width <= 0 || Height <= 0;

    public bool Contains(int x, int y) =>
        x >= X && x < X + Width && y >= Y && y < Y + Height;
}

public readonly record struct RopeCurve(
    double StartX,
    double StartY,
    double ControlX,
    double ControlY,
    double EndX,
    double EndY);
