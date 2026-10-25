namespace Pegline.Core.Domain;

/// <summary>
/// One monitor frozen at the moment of the shortcut, in physical pixels.
/// <see cref="Bgra32"/> is top-down and forced opaque.
/// </summary>
public sealed class FrozenMonitor
{
    public FrozenMonitor(MonitorBounds monitor, byte[] bgra32, int stride)
    {
        Monitor = monitor;
        Bgra32 = bgra32;
        Stride = stride;
    }

    public MonitorBounds Monitor { get; }

    public byte[] Bgra32 { get; }

    public int Stride { get; }

    public int PixelWidth => Monitor.Width;

    public int PixelHeight => Monitor.Height;
}
