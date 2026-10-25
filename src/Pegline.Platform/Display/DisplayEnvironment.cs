using Pegline.Core.Abstractions;
using Pegline.Core.Domain;
using Pegline.Platform.Win32;

namespace Pegline.Platform.Display;

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
}
