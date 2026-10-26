using ScreenshotWindows.Core.Abstractions;
using ScreenshotWindows.Core.Domain;
using ScreenshotWindows.Platform.Win32;

namespace ScreenshotWindows.Platform.Display;

public sealed class DisplayEnvironment : IDisplayEnvironment
{
    public IReadOnlyList<MonitorBounds> QueryMonitors() => MonitorEnum.All();

    public MousePoint QueryCursor()
    {
        if (!NativeMethods.GetCursorPos(out var point))
        {
            return new MousePoint(0, 0);
        }

        return new MousePoint(point.X, point.Y);
    }

    public double CaptionHeightDip(uint dpi)
    {
        if (dpi == 0)
        {
            dpi = 96;
        }

        try
        {
            var caption = NativeMethods.GetSystemMetricsForDpi(NativeMethods.SmCyCaption, dpi);
            var frame = NativeMethods.GetSystemMetricsForDpi(NativeMethods.SmCyFrame, dpi);
            var pad = NativeMethods.GetSystemMetricsForDpi(NativeMethods.SmCxPaddedBorder, dpi);
            return CaptionBand.FromPhysical(caption, frame, pad, dpi);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            return CaptionBand.FallbackDip;
        }
    }
}
