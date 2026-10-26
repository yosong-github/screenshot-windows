namespace ScreenshotWindows.Core.Domain;

public readonly record struct Rgb(byte R, byte G, byte B)
{
    public int Packed => (R << 16) | (G << 8) | B;

    public static Rgb Unpack(int packed) =>
        new((byte)((packed >> 16) & 0xFF), (byte)((packed >> 8) & 0xFF), (byte)(packed & 0xFF));

    public double Luminance => (0.2126 * R + 0.7152 * G + 0.0722 * B) / 255.0;

    public static Rgb Lerp(Rgb from, Rgb to, double t)
    {
        t = t < 0 ? 0 : t > 1 ? 1 : t;
        return new Rgb(
            (byte)Math.Round(from.R + (to.R - from.R) * t),
            (byte)Math.Round(from.G + (to.G - from.G) * t),
            (byte)Math.Round(from.B + (to.B - from.B) * t));
    }
}

public readonly record struct Hsl(double H, double S, double L)
{
    public static Hsl FromRgb(Rgb rgb)
    {
        var r = rgb.R / 255.0;
        var g = rgb.G / 255.0;
        var b = rgb.B / 255.0;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var l = (max + min) / 2;
        var d = max - min;
        if (d < 1e-8)
        {
            return new Hsl(0, 0, l);
        }

        var s = d / (1 - Math.Abs(2 * l - 1));
        double h;
        if (max == r)
        {
            h = ((g - b) / d) % 6;
        }
        else if (max == g)
        {
            h = (b - r) / d + 2;
        }
        else
        {
            h = (r - g) / d + 4;
        }

        h *= 60;
        if (h < 0)
        {
            h += 360;
        }

        return new Hsl(h, s, l);
    }

    public Rgb ToRgb()
    {
        var c = (1 - Math.Abs(2 * L - 1)) * S;
        var x = c * (1 - Math.Abs(H / 60 % 2 - 1));
        var m = L - c / 2;
        var (r, g, b) = H switch
        {
            < 60 => (c, x, 0.0),
            < 120 => (x, c, 0.0),
            < 180 => (0.0, c, x),
            < 240 => (0.0, x, c),
            < 300 => (x, 0.0, c),
            _ => (c, 0.0, x),
        };
        return new Rgb(Byte(r + m), Byte(g + m), Byte(b + m));
    }

    private static byte Byte(double channel) => (byte)Math.Clamp(Math.Round(channel * 255), 0, 255);
}

/// <summary>Three shades of one peg so the highlight and shadow still read as metal.</summary>
public readonly record struct PegPalette(Rgb Shadow, Rgb Body, Rgb Highlight)
{
    public static PegPalette Metal { get; } = new(new Rgb(92, 92, 92), new Rgb(212, 212, 212), new Rgb(247, 247, 247));

    public static PegPalette FromAccent(Rgb accent)
    {
        var hsl = Hsl.FromRgb(accent);
        var body = new Hsl(hsl.H, Clamp(hsl.S, 0.38, 0.72), Clamp(hsl.L, 0.42, 0.56)).ToRgb();
        var mid = Hsl.FromRgb(body);
        var highlight = new Hsl(mid.H, mid.S * 0.55, Math.Min(0.90, mid.L + 0.28)).ToRgb();
        var shadow = new Hsl(mid.H, Math.Min(1, mid.S * 1.05), Math.Max(0.16, mid.L - 0.24)).ToRgb();
        return new PegPalette(shadow, body, highlight);
    }

    public static PegPalette Lerp(PegPalette from, PegPalette to, double t) =>
        new(Rgb.Lerp(from.Shadow, to.Shadow, t), Rgb.Lerp(from.Body, to.Body, t), Rgb.Lerp(from.Highlight, to.Highlight, t));

    private static double Clamp(double value, double min, double max) => Math.Clamp(value, min, max);
}

/// <summary>
/// Dominant chromatic color of a BGRA image. Near-white, near-black, grey, and
/// transparent pixels are ignored so a pale screenshot does not become a grey peg.
/// </summary>
public static class DominantColor
{
    public static Rgb? Extract(ReadOnlySpan<byte> bgra, int width, int height, int stride)
    {
        if (width <= 0 || height <= 0 || stride < width * 4 || bgra.Length < stride * (height - 1) + width * 4)
        {
            return null;
        }

        const int bins = 12;
        Span<double> weight = stackalloc double[bins];
        Span<double> sumR = stackalloc double[bins];
        Span<double> sumG = stackalloc double[bins];
        Span<double> sumB = stackalloc double[bins];
        Span<int> count = stackalloc int[bins];

        var step = Math.Max(1, Math.Max(width, height) / 48);
        for (var y = 0; y < height; y += step)
        {
            var row = y * stride;
            for (var x = 0; x < width; x += step)
            {
                var i = row + x * 4;
                var alpha = bgra[i + 3];
                if (alpha < 128)
                {
                    continue;
                }

                var rgb = new Rgb(bgra[i + 2], bgra[i + 1], bgra[i]);
                var hsl = Hsl.FromRgb(rgb);
                if (hsl.L < 0.10 || hsl.L > 0.92 || hsl.S < 0.18)
                {
                    continue;
                }

                var bin = (int)((hsl.H + 15) % 360 / 360.0 * bins) % bins;
                weight[bin] += hsl.S;
                sumR[bin] += rgb.R * hsl.S;
                sumG[bin] += rgb.G * hsl.S;
                sumB[bin] += rgb.B * hsl.S;
                count[bin]++;
            }
        }

        var best = 0;
        for (var bin = 1; bin < bins; bin++)
        {
            if (weight[bin] > weight[best])
            {
                best = bin;
            }
        }

        if (count[best] < 2 || weight[best] < 0.4)
        {
            return null;
        }

        var scale = 1 / weight[best];
        return new Rgb(
            (byte)Math.Clamp(Math.Round(sumR[best] * scale), 0, 255),
            (byte)Math.Clamp(Math.Round(sumG[best] * scale), 0, 255),
            (byte)Math.Clamp(Math.Round(sumB[best] * scale), 0, 255));
    }
}
