using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ScreenshotWindows.Core.Abstractions;
using ScreenshotWindows.Core.Domain;
using ScreenshotWindows.Platform.Win32;

namespace ScreenshotWindows.Platform.Clipboard;

public sealed class ClipboardImageWriter : IClipboardImageWriter
{
    private const uint CfDib = 8;
    private const uint GmemMoveable = 0x0002;

    public void SetPngFile(string path)
    {
        var png = File.ReadAllBytes(path);
        var dib = ToBottomUpDib(path);
        if (!TryOpen())
        {
            throw new InvalidOperationException("The clipboard is busy.");
        }

        IntPtr pngHandle = IntPtr.Zero;
        IntPtr dibHandle = IntPtr.Zero;
        try
        {
            NativeMethods.EmptyClipboard();
            pngHandle = CopyToGlobal(png);
            dibHandle = CopyToGlobal(dib);
            var pngFormat = NativeMethods.RegisterClipboardFormat("PNG");
            if (NativeMethods.SetClipboardData(pngFormat, pngHandle) != IntPtr.Zero)
            {
                pngHandle = IntPtr.Zero;
            }

            if (NativeMethods.SetClipboardData(CfDib, dibHandle) != IntPtr.Zero)
            {
                dibHandle = IntPtr.Zero;
            }
        }
        finally
        {
            if (pngHandle != IntPtr.Zero)
            {
                NativeMethods.GlobalFree(pngHandle);
            }

            if (dibHandle != IntPtr.Zero)
            {
                NativeMethods.GlobalFree(dibHandle);
            }

            NativeMethods.CloseClipboard();
        }
    }

    private static bool TryOpen()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            if (NativeMethods.OpenClipboard(IntPtr.Zero))
            {
                return true;
            }

            Thread.Sleep(15);
        }

        return false;
    }

    private static IntPtr CopyToGlobal(byte[] data)
    {
        var handle = NativeMethods.GlobalAlloc(GmemMoveable, (nuint)data.Length);
        if (handle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Could not allocate clipboard memory.");
        }

        var target = NativeMethods.GlobalLock(handle);
        try
        {
            Marshal.Copy(data, 0, target, data.Length);
        }
        finally
        {
            NativeMethods.GlobalUnlock(handle);
        }

        return handle;
    }

    private static byte[] ToBottomUpDib(string path)
    {
        using var bitmap = new Bitmap(path);
        var width = bitmap.Width;
        var height = bitmap.Height;
        var source = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var stride = width * 4;
            var pixels = new byte[stride * height];
            for (var y = 0; y < height; y++)
            {
                Marshal.Copy(IntPtr.Add(source.Scan0, y * source.Stride), pixels, y * stride, stride);
            }

            BgraBuffer.ForceOpaque(pixels);
            var header = 40;
            var dib = new byte[header + pixels.Length];
            WriteInt(dib, 0, header);
            WriteInt(dib, 4, width);
            WriteInt(dib, 8, height);
            WriteShort(dib, 12, 1);
            WriteShort(dib, 14, 32);
            for (var y = 0; y < height; y++)
            {
                Buffer.BlockCopy(pixels, (height - 1 - y) * stride, dib, header + y * stride, stride);
            }

            return dib;
        }
        finally
        {
            bitmap.UnlockBits(source);
        }
    }

    private static void WriteInt(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)value;
        buffer[offset + 1] = (byte)(value >> 8);
        buffer[offset + 2] = (byte)(value >> 16);
        buffer[offset + 3] = (byte)(value >> 24);
    }

    private static void WriteShort(byte[] buffer, int offset, short value)
    {
        buffer[offset] = (byte)value;
        buffer[offset + 1] = (byte)(value >> 8);
    }
}
