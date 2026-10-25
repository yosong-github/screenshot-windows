using System.Windows;
using System.Windows.Interop;
using Pegline.Core.Domain;
using Pegline.Platform.Win32;

namespace Pegline.App.Session;

/// <summary>
/// Pins a window to a monitor in physical pixels, then tells WPF the DIP size that matches.
/// Cropping does not use this placement; the overlay maps the drag through the frozen bitmap.
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

        var scaleX = Math.Max(1, monitor.DpiX) / 96.0;
        var scaleY = Math.Max(1, monitor.DpiY) / 96.0;
        var pixelWidth = monitor.Width;
        var pixelHeight = heightDip is { } panelDip
            ? Math.Max(1, (int)Math.Round(panelDip * scaleY))
            : monitor.Height;

        WindowPlacement.Move(hwnd, monitor.Left, monitor.Top, pixelWidth, pixelHeight, activate);
        var origin = ToDip(hwnd, monitor, pixelWidth, pixelHeight, scaleX, scaleY);
        window.Left = origin.X;
        window.Top = origin.Y;
        window.Width = Math.Max(1, origin.Width);
        window.Height = Math.Max(1, origin.Height);
        // Assigning Left/Width can ask WPF to move the HWND again. Put the physical rect back.
        WindowPlacement.Move(hwnd, monitor.Left, monitor.Top, pixelWidth, pixelHeight, activate);
    }

    private static Rect ToDip(IntPtr hwnd, MonitorBounds monitor, int pixelWidth, int pixelHeight, double scaleX, double scaleY)
    {
        var source = HwndSource.FromHwnd(hwnd);
        var target = source?.CompositionTarget;
        if (target is not null)
        {
            var matrix = target.TransformFromDevice;
            var near = matrix.Transform(new Point(monitor.Left, monitor.Top));
            var far = matrix.Transform(new Point(monitor.Left + pixelWidth, monitor.Top + pixelHeight));
            if (far.X > near.X && far.Y > near.Y)
            {
                return new Rect(near, far);
            }
        }

        return new Rect(monitor.Left / scaleX, monitor.Top / scaleY, pixelWidth / scaleX, pixelHeight / scaleY);
    }
}
