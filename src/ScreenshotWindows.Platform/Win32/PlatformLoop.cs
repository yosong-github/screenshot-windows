using System.Runtime.InteropServices;

namespace ScreenshotWindows.Platform.Win32;

/// <summary>
/// A message-only window on its own thread. RegisterHotKey delivers WM_HOTKEY here,
/// and the callback is posted back to the UI synchronization context.
/// </summary>
public sealed class PlatformLoop : IDisposable
{
    private readonly object _gate = new();
    private readonly Queue<Action> _inbox = new();
    private Thread? _thread;
    private SynchronizationContext? _ui;
    private IntPtr _hwnd;
    private bool _started;
    private bool _disposed;

    public bool IsRunning => _hwnd != IntPtr.Zero;

    public IntPtr Handle => _hwnd;

    public event Action<int>? HotkeyReceived;

    public void EnsureStarted()
    {
        if (_started)
        {
            return;
        }

        _ui = SynchronizationContext.Current
            ?? throw new InvalidOperationException("screenshot-windows starts the hotkey loop on the UI thread.");
        var ready = new ManualResetEventSlim(false);
        _thread = new Thread(() => Run(ready))
        {
            IsBackground = true,
            Name = "ScreenshotWindows.Platform",
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        if (!ready.Wait(TimeSpan.FromSeconds(5)) || _hwnd == IntPtr.Zero)
        {
            throw new InvalidOperationException("screenshot-windows could not create its hotkey window.");
        }

        _started = true;
    }

    public void Invoke(Action action) => Invoke<object?>(() =>
    {
        action();
        return null;
    });

    public T Invoke<T>(Func<T> action)
    {
        EnsureStarted();
        if (Thread.CurrentThread == _thread)
        {
            return action();
        }

        var done = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        Enqueue(() =>
        {
            try
            {
                done.SetResult(action());
            }
            catch (Exception ex)
            {
                done.SetException(ex);
            }
        });
        return done.Task.GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_hwnd != IntPtr.Zero)
        {
            NativeMethods.PostMessage(_hwnd, NativeMethods.WmClose, IntPtr.Zero, IntPtr.Zero);
        }

        _thread?.Join(TimeSpan.FromSeconds(2));
        GC.KeepAlive(_proc);
    }

    private void Run(ManualResetEventSlim ready)
    {
        var proc = new NativeMethods.WndProc(Procedure);
        var windowClass = new NativeMethods.WindowClass
        {
            WindowProc = Marshal.GetFunctionPointerForDelegate(proc),
            Instance = NativeMethods.GetModuleHandle(null),
            ClassName = "ScreenshotWindowsPlatform",
        };
        // The delegate must stay rooted for the life of the window.
        _proc = proc;
        NativeMethods.RegisterClass(ref windowClass);
        _hwnd = NativeMethods.CreateWindowEx(
            0,
            windowClass.ClassName,
            string.Empty,
            0,
            0,
            0,
            0,
            0,
            NativeMethods.HwndMessage,
            IntPtr.Zero,
            windowClass.Instance,
            IntPtr.Zero);
        ready.Set();
        if (_hwnd == IntPtr.Zero)
        {
            return;
        }

        while (NativeMethods.GetMessage(out var message, IntPtr.Zero, 0, 0) > 0)
        {
            NativeMethods.TranslateMessage(ref message);
            NativeMethods.DispatchMessage(ref message);
        }
    }

    private NativeMethods.WndProc? _proc;

    private IntPtr Procedure(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam)
    {
        if (message == NativeMethods.WmHotkey)
        {
            var id = wParam.ToInt32();
            _ui?.Post(_ => HotkeyReceived?.Invoke(id), null);
            return IntPtr.Zero;
        }

        if (message == NativeMethods.WmAppInvoke)
        {
            Drain();
            return IntPtr.Zero;
        }

        if (message == NativeMethods.WmClose)
        {
            NativeMethods.DestroyWindow(hwnd);
            return IntPtr.Zero;
        }

        if (message == NativeMethods.WmDestroy)
        {
            NativeMethods.PostQuitMessage(0);
            return IntPtr.Zero;
        }

        return NativeMethods.DefWindowProc(hwnd, message, wParam, lParam);
    }

    private void Enqueue(Action action)
    {
        lock (_gate)
        {
            _inbox.Enqueue(action);
        }

        NativeMethods.PostMessage(_hwnd, NativeMethods.WmAppInvoke, IntPtr.Zero, IntPtr.Zero);
    }

    private void Drain()
    {
        while (true)
        {
            Action action;
            lock (_gate)
            {
                if (_inbox.Count == 0)
                {
                    return;
                }

                action = _inbox.Dequeue();
            }

            action();
        }
    }
}
