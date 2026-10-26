namespace ScreenshotWindows.Core.Domain;

/// <summary>
/// When the line is down. Times are supplied by the caller.
/// The top edge drops a handle; the line comes down only after a short rest on that handle.
/// Leaving the line tucks it away after <see cref="RetractDelay"/>. A new shot peeks for <see cref="PeekDuration"/>.
/// </summary>
public static class RevealTiming
{
    public static readonly TimeSpan RetractDelay = TimeSpan.FromMilliseconds(500);
    public static readonly TimeSpan PeekDuration = TimeSpan.FromSeconds(2.5);
}

public enum LineMotion
{
    None = 0,
    Show = 1,
    Hide = 2,
    ShowHandle = 3,
    HideHandle = 4,
}

public readonly record struct RevealSample(
    DateTimeOffset Now,
    int LiveCount,
    bool PointerInTopBand,
    bool PointerOnHandle,
    bool PointerInLineZone);

public sealed class RevealEngine
{
    private DateTimeOffset? _handleDwellSince;
    private DateTimeOffset? _handleAwaySince;
    private DateTimeOffset? _awaySince;
    private DateTimeOffset _peekUntil = DateTimeOffset.MinValue;

    public bool IsRevealed { get; private set; }

    public bool IsHandleShown { get; private set; }

    public bool IsPinned { get; private set; }

    public bool KeepOpen { get; private set; }

    public bool Wanted { get; private set; }

    public LineMotion Advance(in RevealSample sample)
    {
        if (sample.LiveCount <= 0 && !KeepOpen)
        {
            return Collapse();
        }

        if (!Wanted)
        {
            return HideHandleIfShown();
        }

        if (!IsRevealed)
        {
            return AdvanceHidden(sample);
        }

        return AdvanceOpen(sample);
    }

    public LineMotion Toggle(DateTimeOffset now, int liveCount)
    {
        if (IsRevealed)
        {
            return Retract(liveCount);
        }

        KeepOpen = true;
        Wanted = true;
        Reveal(pinned: true);
        _peekUntil = DateTimeOffset.MinValue;
        return LineMotion.Show;
    }

    public LineMotion Peek(DateTimeOffset now)
    {
        Wanted = true;
        var was = IsRevealed;
        Reveal(pinned: false);
        _peekUntil = now + RevealTiming.PeekDuration;
        return was ? LineMotion.None : LineMotion.Show;
    }

    private LineMotion AdvanceHidden(in RevealSample sample)
    {
        var onHandle = sample.PointerOnHandle && sample.LiveCount > 0;
        if (onHandle)
        {
            _handleDwellSince ??= sample.Now;
            if (sample.Now - _handleDwellSince.Value >= Motion.HandleDwell)
            {
                Reveal(pinned: false);
                return LineMotion.Show;
            }
        }
        else
        {
            _handleDwellSince = null;
        }

        var inTopZone = (sample.PointerInTopBand || onHandle) && sample.LiveCount > 0;
        if (inTopZone)
        {
            _handleAwaySince = null;
            if (!IsHandleShown)
            {
                IsHandleShown = true;
                return LineMotion.ShowHandle;
            }

            return LineMotion.None;
        }

        return RetractHandle(sample.Now);
    }

    private LineMotion AdvanceOpen(in RevealSample sample)
    {
        if (sample.PointerInLineZone && IsPinned)
        {
            IsPinned = false;
        }

        var held = IsPinned || sample.Now < _peekUntil;
        if (sample.PointerInLineZone || held)
        {
            _awaySince = null;
            return LineMotion.None;
        }

        _awaySince ??= sample.Now;
        if (sample.Now - _awaySince.Value < RevealTiming.RetractDelay)
        {
            return LineMotion.None;
        }

        return Retract(sample.LiveCount);
    }

    private LineMotion RetractHandle(DateTimeOffset now)
    {
        _handleDwellSince = null;
        if (!IsHandleShown)
        {
            _handleAwaySince = null;
            return LineMotion.None;
        }

        _handleAwaySince ??= now;
        if (now - _handleAwaySince.Value < Motion.HandleRetractDelay)
        {
            return LineMotion.None;
        }

        IsHandleShown = false;
        _handleAwaySince = null;
        return LineMotion.HideHandle;
    }

    private LineMotion HideHandleIfShown()
    {
        _handleDwellSince = null;
        _handleAwaySince = null;
        if (!IsHandleShown)
        {
            return LineMotion.None;
        }

        IsHandleShown = false;
        return LineMotion.HideHandle;
    }

    private void Reveal(bool pinned)
    {
        if (pinned)
        {
            IsPinned = true;
        }

        IsRevealed = true;
        IsHandleShown = false;
        _awaySince = null;
        _handleDwellSince = null;
        _handleAwaySince = null;
    }

    private LineMotion Retract(int liveCount)
    {
        IsRevealed = false;
        IsHandleShown = false;
        IsPinned = false;
        KeepOpen = false;
        Wanted = liveCount > 0;
        _peekUntil = DateTimeOffset.MinValue;
        _awaySince = null;
        _handleDwellSince = null;
        _handleAwaySince = null;
        return LineMotion.Hide;
    }

    private LineMotion Collapse()
    {
        var wasLine = IsRevealed;
        var wasHandle = IsHandleShown;
        IsRevealed = false;
        IsHandleShown = false;
        IsPinned = false;
        KeepOpen = false;
        Wanted = false;
        _peekUntil = DateTimeOffset.MinValue;
        _awaySince = null;
        _handleDwellSince = null;
        _handleAwaySince = null;
        if (wasLine)
        {
            return LineMotion.Hide;
        }

        return wasHandle ? LineMotion.HideHandle : LineMotion.None;
    }
}
