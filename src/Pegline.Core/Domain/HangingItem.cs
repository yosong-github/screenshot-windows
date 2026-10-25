namespace Pegline.Core.Domain;

public sealed class HangingItem
{
    public HangingItem(Guid id, string path, int pixelWidth, int pixelHeight, double tiltDegrees)
    {
        Id = id;
        Path = path;
        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
        TiltDegrees = tiltDegrees;
    }

    public Guid Id { get; }

    public string Path { get; }

    public int PixelWidth { get; }

    public int PixelHeight { get; }

    public double TiltDegrees { get; }
}
