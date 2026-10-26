using ScreenshotWindows.Core.Domain;

namespace ScreenshotWindows.Core.Tests;

public class RevealEngineTests
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;

    [Fact]
    public void Timings_MatchTheClotheslineInteraction()
    {
        Assert.Equal(TimeSpan.FromMilliseconds(120), Motion.HandleDwell);
        Assert.InRange(Motion.HandleDwell.TotalMilliseconds, 100, 150);
        Assert.Equal(TimeSpan.FromMilliseconds(500), Motion.HandleRetractDelay);
        Assert.InRange(Motion.HandleRetractDelay.TotalMilliseconds, 400, 600);
        Assert.Equal(TimeSpan.FromMilliseconds(500), RevealTiming.RetractDelay);
        Assert.Equal(TimeSpan.FromSeconds(2.5), RevealTiming.PeekDuration);
    }

    [Fact]
    public void TopEdge_DropsTheHandle_AndDoesNotOpenTheLine()
    {
        var engine = Armed();

        Assert.Equal(LineMotion.ShowHandle, engine.Advance(Sample(T0, count: 2, edge: true, handle: false, zone: false)));
        Assert.True(engine.IsHandleShown);
        Assert.False(engine.IsRevealed);

        Assert.Equal(LineMotion.None, engine.Advance(Sample(T0.AddSeconds(2), count: 2, edge: true, handle: false, zone: false)));
        Assert.False(engine.IsRevealed);
    }

    [Fact]
    public void Handle_WaitsThenShows_OnlyWhenSomethingIsHanging()
    {
        var engine = Armed();
        engine.Advance(Sample(T0, count: 2, edge: true, handle: false, zone: false));

        var on = T0.AddSeconds(1);
        Assert.Equal(LineMotion.None, engine.Advance(Sample(on, count: 2, edge: false, handle: true, zone: false)));
        Assert.False(engine.IsRevealed);
        Assert.Equal(
            LineMotion.Show,
            engine.Advance(Sample(on.Add(Motion.HandleDwell), count: 2, edge: false, handle: true, zone: false)));
        Assert.True(engine.IsRevealed);
        Assert.False(engine.IsHandleShown);

        var empty = new RevealEngine();
        Assert.Equal(LineMotion.None, empty.Advance(Sample(T0.AddSeconds(2), count: 0, edge: true, handle: true, zone: true)));
        Assert.False(empty.IsHandleShown);
        Assert.False(empty.IsRevealed);
    }

    [Fact]
    public void HandleFlyby_DoesNotOpenTheLine_AndTheHandleTucks()
    {
        var engine = Armed();
        var shown = T0.AddSeconds(1);
        Assert.Equal(LineMotion.ShowHandle, engine.Advance(Sample(shown, count: 1, edge: true, handle: true, zone: false)));

        var left = shown.AddMilliseconds(40);
        Assert.Equal(LineMotion.None, engine.Advance(Sample(left, count: 1, edge: false, handle: false, zone: false)));
        Assert.False(engine.IsRevealed);
        Assert.Equal(
            LineMotion.HideHandle,
            engine.Advance(Sample(left.Add(Motion.HandleRetractDelay), count: 1, edge: false, handle: false, zone: false)));
        Assert.False(engine.IsHandleShown);
    }

    [Fact]
    public void ReturningToTheEdge_CancelsTheHandleRetract()
    {
        var engine = Armed();
        engine.Advance(Sample(T0, count: 1, edge: true, handle: false, zone: false));
        var left = T0.AddSeconds(1);
        engine.Advance(Sample(left, count: 1, edge: false, handle: false, zone: false));
        var back = left.AddMilliseconds(200);
        Assert.Equal(LineMotion.None, engine.Advance(Sample(back, count: 1, edge: true, handle: false, zone: false)));
        Assert.True(engine.IsHandleShown);
        Assert.Equal(LineMotion.None, engine.Advance(Sample(left.Add(Motion.HandleRetractDelay), count: 1, edge: true, handle: false, zone: false)));
        Assert.True(engine.IsHandleShown);
    }

    [Fact]
    public void LeavingTheZone_TucksAfterTheRetractDelay()
    {
        var engine = Armed();
        var shown = T0.AddSeconds(10);
        Rest(engine, shown, count: 2);

        var left = shown.AddSeconds(1);
        Assert.Equal(LineMotion.None, engine.Advance(Sample(left, count: 2, edge: false, handle: false, zone: false)));
        Assert.Equal(
            LineMotion.Hide,
            engine.Advance(Sample(left.Add(RevealTiming.RetractDelay), count: 2, edge: false, handle: false, zone: false)));
        Assert.False(engine.IsRevealed);
        Assert.True(engine.Wanted);
    }

    [Fact]
    public void StayingInTheZone_HoldsTheLine()
    {
        var engine = Armed();
        Rest(engine, T0.AddSeconds(10), count: 1);

        var later = T0.AddSeconds(20);
        Assert.Equal(LineMotion.None, engine.Advance(Sample(later, count: 1, edge: true, handle: true, zone: true)));
        Assert.True(engine.IsRevealed);
    }

    [Fact]
    public void Toggle_PinsUntilThePointerVisits_ThenLeaves()
    {
        var engine = new RevealEngine();
        Assert.Equal(LineMotion.Show, engine.Toggle(T0, liveCount: 2));
        Assert.True(engine.IsPinned);
        Assert.False(engine.IsHandleShown);

        Assert.Equal(LineMotion.None, engine.Advance(Sample(T0.AddSeconds(2), count: 2, edge: false, handle: false, zone: false)));
        Assert.True(engine.IsRevealed);

        engine.Advance(Sample(T0.AddSeconds(3), count: 2, edge: false, handle: false, zone: true));
        Assert.False(engine.IsPinned);

        var left = T0.AddSeconds(4);
        engine.Advance(Sample(left, count: 2, edge: false, handle: false, zone: false));
        Assert.Equal(LineMotion.Hide, engine.Advance(Sample(left.Add(RevealTiming.RetractDelay), count: 2, edge: false, handle: false, zone: false)));
    }

    [Fact]
    public void Toggle_HidesWhenTheLineIsAlreadyDown()
    {
        var engine = new RevealEngine();
        engine.Toggle(T0, liveCount: 1);
        Assert.Equal(LineMotion.Hide, engine.Toggle(T0.AddSeconds(1), liveCount: 1));
        Assert.False(engine.IsRevealed);
        Assert.True(engine.Wanted);
    }

    [Fact]
    public void Toggle_WhileTheHandleIsOut_OpensTheLine()
    {
        var engine = Armed();
        engine.Advance(Sample(T0, count: 2, edge: true, handle: false, zone: false));
        Assert.Equal(LineMotion.Show, engine.Toggle(T0.AddSeconds(1), liveCount: 2));
        Assert.True(engine.IsRevealed);
        Assert.False(engine.IsHandleShown);
        Assert.True(engine.IsPinned);
    }

    [Fact]
    public void Peek_HoldsWithoutThePointer_ThenTucks()
    {
        var engine = new RevealEngine();
        Assert.Equal(LineMotion.Show, engine.Peek(T0));

        Assert.Equal(LineMotion.None, engine.Advance(Sample(T0.AddSeconds(2.4), count: 1, edge: false, handle: false, zone: false)));
        Assert.True(engine.IsRevealed);

        var released = T0.Add(RevealTiming.PeekDuration);
        Assert.Equal(LineMotion.None, engine.Advance(Sample(released, count: 1, edge: false, handle: false, zone: false)));
        Assert.Equal(
            LineMotion.Hide,
            engine.Advance(Sample(released.Add(RevealTiming.RetractDelay), count: 1, edge: false, handle: false, zone: false)));
    }

    [Fact]
    public void LastShotRemoved_CollapsesUnlessTheLineWasOpenedOnPurpose()
    {
        var peeked = new RevealEngine();
        peeked.Peek(T0);
        Assert.Equal(LineMotion.Hide, peeked.Advance(Sample(T0.AddSeconds(1), count: 0, edge: false, handle: false, zone: true)));
        Assert.False(peeked.Wanted);

        var opened = new RevealEngine();
        opened.Toggle(T0, liveCount: 0);
        Assert.Equal(LineMotion.None, opened.Advance(Sample(T0.AddSeconds(1), count: 0, edge: false, handle: false, zone: true)));
        Assert.True(opened.IsRevealed);
        Assert.True(opened.KeepOpen);

        var handle = Armed();
        handle.Advance(Sample(T0, count: 1, edge: true, handle: false, zone: false));
        Assert.Equal(LineMotion.HideHandle, handle.Advance(Sample(T0.AddSeconds(1), count: 0, edge: true, handle: true, zone: false)));
        Assert.False(handle.IsHandleShown);
    }

    [Fact]
    public void AfterTuck_TheHandleCanBringItDownAgain()
    {
        var engine = Armed();
        var shown = T0.AddSeconds(10);
        Rest(engine, shown, count: 1);
        var left = shown.AddSeconds(1);
        engine.Advance(Sample(left, count: 1, edge: false, handle: false, zone: false));
        engine.Advance(Sample(left.Add(RevealTiming.RetractDelay), count: 1, edge: false, handle: false, zone: false));

        var again = left.AddSeconds(2);
        Assert.Equal(LineMotion.ShowHandle, engine.Advance(Sample(again, count: 1, edge: true, handle: false, zone: false)));
        var onHandle = again.AddMilliseconds(20);
        engine.Advance(Sample(onHandle, count: 1, edge: false, handle: true, zone: false));
        Assert.Equal(
            LineMotion.Show,
            engine.Advance(Sample(onHandle.Add(Motion.HandleDwell), count: 1, edge: false, handle: true, zone: false)));
    }

    private static void Rest(RevealEngine engine, DateTimeOffset at, int count)
    {
        engine.Advance(Sample(at, count, edge: true, handle: false, zone: false));
        var onHandle = at.AddMilliseconds(20);
        engine.Advance(Sample(onHandle, count, edge: false, handle: true, zone: false));
        Assert.Equal(
            LineMotion.Show,
            engine.Advance(Sample(onHandle.Add(Motion.HandleDwell), count, edge: false, handle: true, zone: false)));
    }

    private static RevealEngine Armed()
    {
        var engine = new RevealEngine();
        engine.Peek(T0);
        engine.Advance(Sample(T0.Add(RevealTiming.PeekDuration), count: 1, edge: false, handle: false, zone: false));
        engine.Advance(Sample(T0.Add(RevealTiming.PeekDuration).Add(RevealTiming.RetractDelay), count: 1, edge: false, handle: false, zone: false));
        Assert.False(engine.IsRevealed);
        Assert.True(engine.Wanted);
        return engine;
    }

    private static RevealSample Sample(DateTimeOffset now, int count, bool edge, bool handle, bool zone) =>
        new(now, count, edge, handle, zone);
}
