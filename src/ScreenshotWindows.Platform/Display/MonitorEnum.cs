using ScreenshotWindows.Core.Domain;
using ScreenshotWindows.Platform.Win32;

namespace ScreenshotWindows.Platform.Display;

internal static class MonitorEnum
{
    public static IReadOnlyList<MonitorBounds> All()
    {
        var found = new List<MonitorBounds>();
        NativeMethods.MonitorEnumProc callback = (IntPtr monitor, IntPtr _, ref NativeMethods.NativeRect rect, IntPtr _) =>
        {
            var info = new NativeMethods.MonitorInfoEx { Size = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MonitorInfoEx>() };
            if (!NativeMethods.GetMonitorInfo(monitor, ref info))
            {
                return true;
            }

            uint dpiX = 96;
            uint dpiY = 96;
            try
            {
                if (NativeMethods.GetDpiForMonitor(monitor, 0, out var x, out var y) == 0)
                {
                    dpiX = x == 0 ? 96 : x;
                    dpiY = y == 0 ? 96 : y;
                }
            }
            catch (DllNotFoundException)
            {
                dpiX = 96;
                dpiY = 96;
            }

            var width = info.Monitor.Right - info.Monitor.Left;
            var height = info.Monitor.Bottom - info.Monitor.Top;
            if (width <= 0 || height <= 0)
            {
                return true;
            }

            found.Add(new MonitorBounds(
                info.DeviceName ?? "DISPLAY",
                info.Monitor.Left,
                info.Monitor.Top,
                width,
                height,
                dpiX,
                dpiY));
            return true;
        };

        NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);
        if (found.Count == 0)
        {
            found.Add(new MonitorBounds(
                "DISPLAY",
                0,
                0,
                Math.Max(1, NativeMethods.GetSystemMetrics(NativeMethods.SmCxScreen)),
                Math.Max(1, NativeMethods.GetSystemMetrics(NativeMethods.SmCyScreen)),
                96,
                96));
        }

        return found;
    }
}
