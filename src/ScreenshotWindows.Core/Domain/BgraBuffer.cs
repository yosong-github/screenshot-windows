namespace ScreenshotWindows.Core.Domain;

/// <summary>
/// Pixel helpers for a 32-bit BGRA buffer. Screen captures come back bottom-up with a zero alpha,
/// which would make the frozen preview and the saved PNG invisible.
/// </summary>
public static class BgraBuffer
{
    public static void FlipVertical(byte[] pixels, int stride, int height)
    {
        if (height < 2 || stride <= 0)
        {
            return;
        }

        var row = new byte[stride];
        for (var y = 0; y < height / 2; y++)
        {
            var top = y * stride;
            var bottom = (height - 1 - y) * stride;
            Buffer.BlockCopy(pixels, top, row, 0, stride);
            Buffer.BlockCopy(pixels, bottom, pixels, top, stride);
            Buffer.BlockCopy(row, 0, pixels, bottom, stride);
        }
    }

    public static void ForceOpaque(byte[] bgra)
    {
        for (var i = 3; i < bgra.Length; i += 4)
        {
            bgra[i] = 255;
        }
    }
}
