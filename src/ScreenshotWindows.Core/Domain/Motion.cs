namespace ScreenshotWindows.Core.Domain;

/// <summary>
/// Shared timing for the line, the flight, and the hover. Views sample these;
/// they do not invent their own durations.
/// </summary>
public static class Motion
{
    public static readonly TimeSpan FlightDuration = TimeSpan.FromMilliseconds(650);

    public static readonly TimeSpan OverlayFade = TimeSpan.FromMilliseconds(140);

    public static readonly TimeSpan TintFade = TimeSpan.FromMilliseconds(180);

    /// <summary>How long the pointer must rest on the handle before the line comes down.</summary>
    public static readonly TimeSpan HandleDwell = TimeSpan.FromMilliseconds(120);

    /// <summary>How long the handle stays after the pointer leaves the top edge without resting on it.</summary>
    public static readonly TimeSpan HandleRetractDelay = TimeSpan.FromMilliseconds(500);

    public const double FlightArc = 18;

    public const double HoverScale = 1.06;

    public const double HoverSwing = 2.4;

    /// <summary>Angular speed (degrees per second) added when a photo lands. The angle itself does not jump.</summary>
    public const double LandingSwingVelocity = 100;

    /// <summary>
    /// Downward speed (DIP per second) when the line starts to tuck away.
    /// The slide spring is stiff enough to settle a full travel in under half a second,
    /// so this only reads as a few DIPs of anticipatory dip.
    /// </summary>
    public const double RetractKick = 2300;

    /// <summary>Degrees of swing per DIP/second of line travel. The photos lag the rope.</summary>
    public const double SwingLag = 0.018;

    public const double MaxLagSwing = 4.5;

    public static SpringConfig Slide { get; } = new(290, 22.5, 0.35, 6);

    /// <summary>Short drop for the handle. The travel is only the tab height.</summary>
    public static SpringConfig Handle { get; } = new(420, 28, 0.2, 4);

    public static SpringConfig Swing { get; } = new(78, 11.5, 0.08, 1.2);

    public static SpringConfig Hover { get; } = new(380, 28, 0.002, 0.04);

    public static SpringConfig Shift { get; } = new(280, 30, 0.25, 6);

    public static SpringConfig Fade { get; } = new(420, 34, 0.01, 0.15);
}

public readonly record struct SpringConfig(double Stiffness, double Damping, double SettleDistance, double SettleVelocity);

/// <summary>One-dimensional damped spring. <paramref name="dt"/> is seconds.</summary>
public readonly struct Spring1D
{
    public Spring1D(double value, double velocity)
    {
        Value = value;
        Velocity = velocity;
    }

    public double Value { get; }

    public double Velocity { get; }

    public bool IsResting(double target, SpringConfig config) =>
        Math.Abs(Value - target) <= config.SettleDistance && Math.Abs(Velocity) <= config.SettleVelocity;

    public Spring1D Kick(double velocity) => new(Value, velocity);

    public Spring1D Step(double target, double dt, SpringConfig config)
    {
        if (dt <= 0)
        {
            return this;
        }

        if (IsResting(target, config))
        {
            return new Spring1D(target, 0);
        }

        var force = -config.Stiffness * (Value - target) - config.Damping * Velocity;
        var velocity = Velocity + force * dt;
        var value = Value + velocity * dt;
        if (Math.Abs(value - target) <= config.SettleDistance && Math.Abs(velocity) <= config.SettleVelocity)
        {
            return new Spring1D(target, 0);
        }

        return new Spring1D(value, velocity);
    }
}

public static class Easing
{
    public static double Clamp01(double t) => t < 0 ? 0 : t > 1 ? 1 : t;

    public static double Lerp(double from, double to, double t) => from + (to - from) * t;

    /// <summary>Fast start, gentle arrival. 0 and 1 stay put.</summary>
    public static double OutCubic(double t)
    {
        var u = 1 - Clamp01(t);
        return 1 - u * u * u;
    }

    public static double SmoothStep(double t)
    {
        t = Clamp01(t);
        return t * t * (3 - 2 * t);
    }
}

public readonly record struct DipRect(double X, double Y, double Width, double Height)
{
    public double MidX => X + Width / 2;

    public double MidY => Y + Height / 2;
}

public readonly record struct FlightSample(
    double X,
    double Y,
    double Width,
    double Height,
    double Angle,
    double Chrome);

/// <summary>
/// The capture shrinking toward its slot. Progress is 0 at the selection and 1 in the slot.
/// The arc lifts the photo in the middle and is gone at both ends.
/// </summary>
public static class FlightPath
{
    public static FlightSample Sample(DipRect from, DipRect to, double tiltDegrees, double progress)
    {
        var t = Easing.Clamp01(progress);
        var k = Easing.OutCubic(t);
        var arc = Math.Sin(Math.PI * t) * Motion.FlightArc;
        var chrome = Easing.SmoothStep(Math.Clamp((k - 0.28) / 0.72, 0, 1));
        return new FlightSample(
            Easing.Lerp(from.X, to.X, k),
            Easing.Lerp(from.Y, to.Y, k) - arc,
            Easing.Lerp(from.Width, to.Width, k),
            Easing.Lerp(from.Height, to.Height, k),
            tiltDegrees * k,
            chrome);
    }
}
