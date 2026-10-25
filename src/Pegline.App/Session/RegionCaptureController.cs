using System.Windows;
using Pegline.App.Views;
using Pegline.Core.Abstractions;
using Pegline.Core.Domain;

namespace Pegline.App.Session;

public readonly record struct CaptureSelection(FrozenMonitor Frame, PixelRect Region, Rect ScreenDip);

/// <summary>
/// One dim overlay per frozen monitor. The first commit or cancel closes every overlay.
/// </summary>
public sealed class RegionCaptureController
{
    private readonly IDisplayEnvironment _displays;

    public RegionCaptureController(IDisplayEnvironment displays)
    {
        _displays = displays;
    }

    public Task<CaptureSelection?> RunAsync(IReadOnlyList<FrozenMonitor> frames)
    {
        if (frames.Count == 0)
        {
            return Task.FromResult<CaptureSelection?>(null);
        }

        var windows = new List<CaptureOverlayWindow>(frames.Count);
        var done = new TaskCompletionSource<CaptureSelection?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var finished = 0;

        void Finish(CaptureSelection? selection)
        {
            if (Interlocked.Exchange(ref finished, 1) != 0)
            {
                return;
            }

            foreach (var open in windows)
            {
                open.Dismiss();
            }

            done.TrySetResult(selection);
        }

        try
        {
            var cursor = _displays.QueryCursor();
            CaptureOverlayWindow? focus = null;
            foreach (var frame in frames)
            {
                var window = new CaptureOverlayWindow(frame);
                var captured = frame;
                window.Committed += (_, choice) => Finish(new CaptureSelection(captured, choice.Region, choice.ScreenDip));
                window.Cancelled += (_, _) => Finish(null);
                windows.Add(window);
                if (focus is null && frame.Monitor.Contains(cursor.X, cursor.Y))
                {
                    focus = window;
                }
            }

            foreach (var window in windows)
            {
                window.Show();
            }

            var target = focus ?? windows[0];
            target.Activate();
            target.Focus();
        }
        catch
        {
            Finish(null);
            throw;
        }

        return done.Task;
    }
}
