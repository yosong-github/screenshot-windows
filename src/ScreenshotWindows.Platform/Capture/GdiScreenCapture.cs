using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ScreenshotWindows.Core.Abstractions;
using ScreenshotWindows.Core.Domain;
using ScreenshotWindows.Platform.Display;
using ScreenshotWindows.Platform.Win32;

namespace ScreenshotWindows.Platform.Capture;

/// <summary>
/// Freezes each monitor with BitBlt. The process is per-monitor DPI aware, so
/// <see cref="MonitorBounds"/> is in physical pixels and the bitmap matches the screen 1:1.
/// </summary>
public sealed class GdiScreenCapture : IScreenCapture
{
    public IReadOnlyList<FrozenMonitor> Freeze()
    {
        var frames = new List<FrozenMonitor>();
        foreach (var monitor in MonitorEnum.All())
        {
            var pixels = CaptureBgra(monitor.Left, monitor.Top, monitor.Width, monitor.Height);
            if (pixels.Length == 0)
            {
                continue;
            }

            frames.Add(new FrozenMonitor(monitor, pixels, monitor.Width * 4));
        }

        return frames;
    }

    public byte[] CropToPng(FrozenMonitor monitor, PixelRect region)
    {
        var rect = CaptureCrop.Clamp(region, monitor.PixelWidth, monitor.PixelHeight);
        if (!CaptureCrop.CanEncode(rect))
        {
            throw new ArgumentOutOfRangeException(nameof(region), "The selection is too small to save.");
        }

        var expected = monitor.Stride * monitor.PixelHeight;
        if (monitor.Bgra32.Length < expected || rect.X * 4 + rect.Width * 4 > monitor.Stride)
        {
            throw new ArgumentException("The frozen frame is smaller than the selection.", nameof(monitor));
        }

        using var crop = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
        var data = crop.LockBits(new Rectangle(0, 0, rect.Width, rect.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try
        {
            var rowBytes = rect.Width * 4;
            for (var y = 0; y < rect.Height; y++)
            {
                var source = (rect.Y + y) * monitor.Stride + rect.X * 4;
                Marshal.Copy(monitor.Bgra32, source, IntPtr.Add(data.Scan0, y * data.Stride), rowBytes);
            }
        }
        finally
        {
            crop.UnlockBits(data);
        }

        using var stream = new MemoryStream();
        crop.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }

    private static byte[] CaptureBgra(int x, int y, int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return Array.Empty<byte>();
        }

        var screen = NativeMethods.GetDC(IntPtr.Zero);
        if (screen == IntPtr.Zero)
        {
            return Array.Empty<byte>();
        }

        var memory = NativeMethods.CreateCompatibleDC(screen);
        var bitmap = NativeMethods.CreateCompatibleBitmap(screen, width, height);
        if (memory == IntPtr.Zero || bitmap == IntPtr.Zero)
        {
            if (bitmap != IntPtr.Zero)
            {
                NativeMethods.DeleteObject(bitmap);
            }

            if (memory != IntPtr.Zero)
            {
                NativeMethods.DeleteDC(memory);
            }

            NativeMethods.ReleaseDC(IntPtr.Zero, screen);
            return Array.Empty<byte>();
        }

        var previous = NativeMethods.SelectObject(memory, bitmap);
        try
        {
            if (!NativeMethods.BitBlt(memory, 0, 0, width, height, screen, x, y, NativeMethods.SrcCopy))
            {
                return Array.Empty<byte>();
            }

            var header = new NativeMethods.BitmapInfoHeader
            {
                Size = Marshal.SizeOf<NativeMethods.BitmapInfoHeader>(),
                Width = width,
                Height = height,
                Planes = 1,
                BitCount = 32,
                Compression = 0,
            };
            var stride = width * 4;
            var bottomUp = new byte[stride * height];
            var rows = NativeMethods.GetDIBits(memory, bitmap, 0, (uint)height, bottomUp, ref header, 0);
            if (rows == 0)
            {
                return Array.Empty<byte>();
            }

            // GetDIBits with a positive height is bottom-up, and the desktop alpha byte is 0.
            BgraBuffer.FlipVertical(bottomUp, stride, height);
            BgraBuffer.ForceOpaque(bottomUp);
            return bottomUp;
        }
        finally
        {
            NativeMethods.SelectObject(memory, previous);
            NativeMethods.DeleteObject(bitmap);
            NativeMethods.DeleteDC(memory);
            NativeMethods.ReleaseDC(IntPtr.Zero, screen);
        }
    }
}
