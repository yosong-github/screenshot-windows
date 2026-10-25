namespace Pegline.Core.Domain;

/// <summary>
/// When the line is down. Times are supplied by the caller.
/// Top-edge dwell and retract match the interaction timings of the original clothesline:
/// 250 ms to come down, 500 ms to tuck away, 2.5 s peek after a new shot.
/// </summary>
public static class RevealTiming
{
    public static readonly TimeSpan TopEdgeDelay = TimeSpan.FromMilliseconds(250);
    public static readonly TimeSpan RetractDelay = TimeSpan.FromMilliseconds(500);
    public static readonly TimeSpan PeekDuration = TimeSpan.FromSeconds(2.5);
}

public enum LineMotion
{
    None = 0,
    Show = 1,
    Hide = 2,
}

public readonly record struct RevealSample(
    DateTimeOffset Now,
    int LiveCount,
    bool PointerInTopBand,
    bool PointerInLineZone);

public sealed class RevealEngine
{
    private DateTimeOffset? _dwellSince;
    private DateTimeOffset? _awaySince;
    private DateTimeOffset _peekUntil = DateTimeOffset.MinValue;

    public bool IsRevealed { get; private set; }

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
            _dwellSince = null;
            return LineMotion.None;
        }

        if (!IsRevealed)
        {
            if (sample.PointerInTopBand && sample.LiveCount > 0)
            {
                _dwellSince ??= sample.Now;
                if (sample.Now - _dwellSince.Value >= RevealTiming.TopEdgeDelay)
                {
                    _dwellSince = null;
                    Reveal(pinned: false);
                    return LineMotion.Show;
                }
            }
            else
            {
                _dwellSince = null;
            }

            return LineMotion.None;
        }

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

    private void Reveal(bool pinned)
    {
        if (pinned)
        {
            IsPinned = true;
        }

        IsRevealed = true;
        _awaySince = null;
        _dwellSince = null;
    }

    private LineMotion Retract(int liveCount)
    {
        IsRevealed = false;
        IsPinned = false;
        KeepOpen = false;
        Wanted = liveCount > 0;
        _peekUntil = DateTimeOffset.MinValue;
        _awaySince = null;
        _dwellSince = null;
        return LineMotion.Hide;
    }

    private LineMotion Collapse()
    {
        var was = IsRevealed;
        IsRevealed = false;
        IsPinned = false;
        KeepOpen = false;
        Wanted = false;
        _peekUntil = DateTimeOffset.MinValue;
        _awaySince = null;
        _dwellSince = null;
        return was ? LineMotion.Hide : LineMotion.None;
    }
}
