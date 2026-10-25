using Pegline.Core.Domain;

namespace Pegline.Core.Tests;

public class MotionTests
{
    [Fact]
    public void OutCubic_StartsFastAndLandsSoft()
    {
        Assert.Equal(0, Easing.OutCubic(0), 5);
        Assert.Equal(1, Easing.OutCubic(1), 5);
        Assert.True(Easing.OutCubic(0.5) > 0.8);
    }

    [Fact]
    public void Flight_LeavesTheSelection_AndLandsInTheSlot()
    {
        var from = new DipRect(80, 600, 800, 500);
        var to = new DipRect(420, 48, 150, 120);
        var start = FlightPath.Sample(from, to, tiltDegrees: 2, progress: 0);
        var end = FlightPath.Sample(from, to, tiltDegrees: 2, progress: 1);
        var mid = FlightPath.Sample(from, to, tiltDegrees: 2, progress: 0.5);

        Assert.Equal(from.X, start.X, 3);
        Assert.Equal(from.Y, start.Y, 3);
        Assert.Equal(from.Width, start.Width, 3);
        Assert.Equal(to.X, end.X, 3);
        Assert.Equal(to.Y, end.Y, 3);
        Assert.Equal(to.Width, end.Width, 3);
        Assert.Equal(2, end.Angle, 3);
        Assert.True(mid.Y < Easing.Lerp(from.Y, to.Y, Easing.OutCubic(0.5)));
        Assert.True(end.Width < start.Width);
    }

    [Fact]
    public void Slide_OvershootsALittle_ThenSettles()
    {
        var spring = new Spring1D(-240, 0);
        var peak = -240.0;
        for (var i = 0; i < 27; i++)
        {
            spring = spring.Step(0, 1.0 / 60, Motion.Slide);
            peak = Math.Max(peak, spring.Value);
        }

        Assert.InRange(peak, 4, 22);
        Assert.InRange(spring.Value, -1.5, 1.5);
        Assert.True(spring.IsResting(0, Motion.Slide));
    }

    [Fact]
    public void Retract_DipsBeforeRising()
    {
        var spring = new Spring1D(0, Motion.RetractKick);
        var peak = 0.0;
        for (var i = 0; i < 8; i++)
        {
            spring = spring.Step(-240, 1.0 / 60, Motion.Slide);
            peak = Math.Max(peak, spring.Value);
        }

        Assert.InRange(peak, 1, 18);
        for (var i = 0; i < 22; i++)
        {
            spring = spring.Step(-240, 1.0 / 60, Motion.Slide);
        }

        Assert.InRange(spring.Value, -250, -220);
    }

    [Fact]
    public void Reverse_ContinuesFromTheCurrentValue()
    {
        var spring = new Spring1D(-240, 0);
        spring = spring.Step(0, 1.0 / 60, Motion.Slide);
        var at = spring.Value;
        spring = spring.Step(-240, 1.0 / 60, Motion.Slide);
        Assert.InRange(Math.Abs(spring.Value - at), 0, 40);
        Assert.NotEqual(-240, spring.Value);
    }

    [Fact]
    public void Shift_GlidesHome_WithoutABigBounce()
    {
        var spring = new Spring1D(120, 0);
        var away = 0.0;
        for (var i = 0; i < 40; i++)
        {
            spring = spring.Step(0, 1.0 / 60, Motion.Shift);
            if (spring.Value < 0)
            {
                away = Math.Max(away, -spring.Value);
            }
        }

        Assert.InRange(away, 0, 8);
        Assert.InRange(spring.Value, -1, 1);
    }
}
