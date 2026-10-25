using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Pegline.Core.Domain;

namespace Pegline.Platform.Imaging;

public readonly record struct BgraImage(int Width, int Height, byte[] Pixels, int Stride);

/// <summary>Decodes and downscales a PNG off the UI thread.</summary>
public static class PngImages
{
    public static BgraImage Thumbnail(byte[] png, int maxEdge)
    {
        using var input = new MemoryStream(png);
        using var source = new Bitmap(input);
        var longest = Math.Max(source.Width, source.Height);
        var scale = longest <= 0 ? 1 : Math.Min(1.0, maxEdge / (double)longest);
        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));
        using var dest = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(dest))
        {
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.DrawImage(source, 0, 0, width, height);
        }

        var locked = dest.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var stride = locked.Stride;
            var pixels = new byte[stride * height];
            Marshal.Copy(locked.Scan0, pixels, 0, pixels.Length);
            BgraBuffer.ForceOpaque(pixels);
            return new BgraImage(width, height, pixels, stride);
        }
        finally
        {
            dest.UnlockBits(locked);
        }
    }
}