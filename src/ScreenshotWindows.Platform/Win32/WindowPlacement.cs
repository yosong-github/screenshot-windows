using ScreenshotWindows.Core.Domain;

namespace ScreenshotWindows.Platform.Win32;

public static class WindowPlacement
{
    public static void Move(IntPtr hwnd, int x, int y, int width, int height, bool activate)
    {
        var flags = NativeMethods.SwpShowWindow | (activate ? 0 : NativeMethods.SwpNoActivate);
        NativeMethods.SetWindowPos(hwnd, NativeMethods.HwndTopmost, x, y, width, height, flags);
    }

    public static void MakeToolWindow(IntPtr hwnd)
    {
        var style = NativeMethods.GetExStyle(hwnd).ToInt64();
        style |= NativeMethods.WsExToolWindow | NativeMethods.WsExNoActivate | NativeMethods.WsExTransparent;
        NativeMethods.SetExStyle(hwnd, new IntPtr(style));
    }

    public static void SetClickThrough(IntPtr hwnd, bool enabled)
    {
        var style = NativeMethods.GetExStyle(hwnd).ToInt64();
        var updated = enabled
            ? style | NativeMethods.WsExTransparent
            : style & ~NativeMethods.WsExTransparent;
        if (updated != style)
        {
            NativeMethods.SetExStyle(hwnd, new IntPtr(updated));
        }
    }

    public static bool TryClientPoint(IntPtr hwnd, MousePoint screen, out int x, out int y)
    {
        var point = new NativeMethods.NativePoint { X = screen.X, Y = screen.Y };
        if (!NativeMethods.ScreenToClient(hwnd, ref point))
        {
            x = 0;
            y = 0;
            return false;
        }

        x = point.X;
        y = point.Y;
        return true;
    }
}
