using ScreenshotWindows.Core.Domain;

namespace ScreenshotWindows.Core.Tests;

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
        Assert.Equal(0, start.Angle, 5);
        Assert.Equal(0, start.Chrome, 5);
        Assert.Equal(1, end.Chrome, 5);
    }

    [Fact]
    public void Flight_IsContinuous_ThroughHandoff()
    {
        var from = new DipRect(100, 480, 900, 560);
        var to = new DipRect(420, 80, 158, 116);
        const double tilt = 2.2;
        var previous = FlightPath.Sample(from, to, tilt, 0);
        Assert.Equal(from.X, previous.X, 6);
        Assert.Equal(from.Y, previous.Y, 6);
        Assert.Equal(from.Width, previous.Width, 6);
        Assert.Equal(from.Height, previous.Height, 6);

        var span = Math.Abs(from.X - to.X) + Math.Abs(from.Y - to.Y)
            + Math.Abs(from.Width - to.Width) + Math.Abs(from.Height - to.Height);
        for (var i = 1; i <= 60; i++)
        {
            var sample = FlightPath.Sample(from, to, tilt, i / 60.0);
            var step = Math.Abs(sample.X - previous.X)
                + Math.Abs(sample.Y - previous.Y)
                + Math.Abs(sample.Width - previous.Width)
                + Math.Abs(sample.Height - previous.Height)
                + Math.Abs(sample.Angle - previous.Angle)
                + Math.Abs(sample.Chrome - previous.Chrome) * 80;
            Assert.True(step < span * 0.2 + 5, $"step {i} jumped by {step}");
            previous = sample;
        }

        Assert.Equal(to.X, previous.X, 6);
        Assert.Equal(to.Y, previous.Y, 6);
        Assert.Equal(to.Width, previous.Width, 6);
        Assert.Equal(to.Height, previous.Height, 6);
        Assert.Equal(tilt, previous.Angle, 6);
        Assert.Equal(1, previous.Chrome, 6);
    }

    [Fact]
    public void LandingKick_DoesNotTeleport_ThenSettles()
    {
        var spring = new Spring1D(0, Motion.LandingSwingVelocity);
        spring = spring.Step(0, 1.0 / 60, Motion.Swing);
        Assert.InRange(Math.Abs(spring.Value), 0, 2.5);

        var peak = Math.Abs(spring.Value);
        for (var i = 0; i < 70; i++)
        {
            spring = spring.Step(0, 1.0 / 60, Motion.Swing);
            peak = Math.Max(peak, Math.Abs(spring.Value));
        }

        Assert.InRange(peak, 3, 6);
        Assert.True(spring.IsResting(0, Motion.Swing));
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
