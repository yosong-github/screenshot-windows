using System.Windows;
using System.Windows.Interop;
using ScreenshotWindows.Core.Domain;
using ScreenshotWindows.Platform.Win32;

namespace ScreenshotWindows.App.Session;

/// <summary>
/// Pins a window to a monitor. Layout DIPs come from the monitor DPI
/// (<see cref="DisplayScale"/>); the HWND is always the physical rectangle.
/// TransformFromDevice is not used: it is still the previous scale while the
/// window is moving, and sizing the layout in physical pixels makes WPF draw
/// the tree at 96/dpi into the top-left of the monitor.
/// </summary>
internal static class MonitorFit
{
    public static void Apply(Window window, MonitorBounds monitor, double? heightDip, bool activate)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero || monitor.Width <= 0 || monitor.Height <= 0)
        {
            return;
        }

        var pixelWidth = monitor.Width;
        var pixelHeight = heightDip is { } panel
            ? Math.Max(1, DisplayScale.ToPhysical(panel, monitor.DpiY))
            : monitor.Height;
        var dipWidth = Math.Max(1, DisplayScale.ToDip(pixelWidth, monitor.DpiX));
        var dipHeight = Math.Max(1, DisplayScale.ToDip(pixelHeight, monitor.DpiY));

        window.Width = dipWidth;
        window.Height = dipHeight;
        WindowPlacement.Move(hwnd, monitor.Left, monitor.Top, pixelWidth, pixelHeight, activate);
    }
}
