using ScreenshotWindows.Core.Abstractions;
using ScreenshotWindows.Core.Domain;
using ScreenshotWindows.Platform.Win32;

namespace ScreenshotWindows.Platform.Hotkeys;

public sealed class GlobalHotkeyService : IGlobalHotkey
{
    private readonly PlatformLoop _loop;

    public GlobalHotkeyService(PlatformLoop loop)
    {
        _loop = loop;
        _loop.HotkeyReceived += id => Pressed?.Invoke(this, id);
    }

    public event EventHandler<int>? Pressed;

    public bool TryBind(int id, HotkeyChord chord)
    {
        if (!chord.IsUsable)
        {
            return false;
        }

        _loop.EnsureStarted();
        return _loop.Invoke(() =>
        {
            NativeMethods.UnregisterHotKey(_loop.Handle, id);
            return NativeMethods.RegisterHotKey(
                _loop.Handle,
                id,
                (uint)chord.Modifiers | NativeMethods.ModNoRepeat,
                (uint)chord.VirtualKey);
        });
    }

    public void Unbind(int id)
    {
        if (!_loop.IsRunning)
        {
            return;
        }

        _loop.Invoke(() => NativeMethods.UnregisterHotKey(_loop.Handle, id));
    }
}
