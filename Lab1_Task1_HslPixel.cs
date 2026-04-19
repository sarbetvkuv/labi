using System;

namespace Lab1.Task1;

public sealed class HslPixel : IEquatable<HslPixel>
{
    public double Hue { get; }
    public double Saturation { get; }
    public double Lightness { get; }

    public HslPixel(double hue, double saturation, double lightness)
    {
        Hue = NormalizeHue(hue);
        Saturation = ClampPercent(saturation);
        Lightness = ClampPercent(lightness);
    }

    public override string ToString()
    {
        return $"rgba({Math.Round(Hue)},{Math.Round(Saturation)}%,{Math.Round(Lightness)}%)";
    }

    public (byte R, byte G, byte B) ToRgb()
    {
        double h = Hue / 360.0;
        double s = Saturation / 100.0;
        double l = Lightness / 100.0;

        if (s == 0)
        {
            byte gray = ToByte(l * 255.0);
            return (gray, gray, gray);
        }

        double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
        double p = 2 * l - q;

        double r = HueToRgb(p, q, h + 1.0 / 3.0);
        double g = HueToRgb(p, q, h);
        double b = HueToRgb(p, q, h - 1.0 / 3.0);

        return (ToByte(r * 255.0), ToByte(g * 255.0), ToByte(b * 255.0));
    }

    public string ToHex()
    {
        var (r, g, b) = ToRgb();
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    public bool Equals(HslPixel? other)
    {
        if (other is null)
        {
            return false;
        }

        return Math.Abs(Hue - other.Hue) < 1e-9
            && Math.Abs(Saturation - other.Saturation) < 1e-9
            && Math.Abs(Lightness - other.Lightness) < 1e-9;
    }

    public override bool Equals(object? obj) => Equals(obj as HslPixel);

    public override int GetHashCode() => HashCode.Combine(Hue, Saturation, Lightness);

    public static bool operator ==(HslPixel? left, HslPixel? right)
    {
        return left is null ? right is null : left.Equals(right);
    }

    public static bool operator !=(HslPixel? left, HslPixel? right) => !(left == right);

    public static HslPixel operator +(HslPixel left, HslPixel right)
    {
        return new HslPixel(
            left.Hue + right.Hue,
            left.Saturation + right.Saturation,
            left.Lightness + right.Lightness);
    }

    public static HslPixel operator -(HslPixel left, HslPixel right)
    {
        return new HslPixel(
            left.Hue - right.Hue,
            left.Saturation - right.Saturation,
            left.Lightness - right.Lightness);
    }

    public static HslPixel operator *(HslPixel pixel, double scalar)
    {
        return new HslPixel(pixel.Hue * scalar, pixel.Saturation * scalar, pixel.Lightness * scalar);
    }

    public static HslPixel operator *(double scalar, HslPixel pixel) => pixel * scalar;

    public static HslPixel operator *(HslPixel left, HslPixel right)
    {
        return new HslPixel(
            left.Hue * (right.Hue / 360.0),
            left.Saturation * (right.Saturation / 100.0),
            left.Lightness * (right.Lightness / 100.0));
    }

    public static HslPixel operator /(HslPixel pixel, double scalar)
    {
        if (Math.Abs(scalar) < 1e-12)
        {
            throw new DivideByZeroException("Scalar must not be zero.");
        }

        return new HslPixel(pixel.Hue / scalar, pixel.Saturation / scalar, pixel.Lightness / scalar);
    }

    private static double ClampPercent(double value)
    {
        if (value < 0)
        {
            return 0;
        }

        if (value > 100)
        {
            return 100;
        }

        return value;
    }

    private static double NormalizeHue(double hue)
    {
        double normalized = hue % 360.0;
        if (normalized < 0)
        {
            normalized += 360.0;
        }

        return normalized;
    }

    private static byte ToByte(double value)
    {
        if (value < 0)
        {
            return 0;
        }

        if (value > 255)
        {
            return 255;
        }

        return (byte)Math.Round(value);
    }

    private static double HueToRgb(double p, double q, double t)
    {
        if (t < 0)
        {
            t += 1;
        }

        if (t > 1)
        {
            t -= 1;
        }

        if (t < 1.0 / 6.0)
        {
            return p + (q - p) * 6.0 * t;
        }

        if (t < 1.0 / 2.0)
        {
            return q;
        }

        if (t < 2.0 / 3.0)
        {
            return p + (q - p) * (2.0 / 3.0 - t) * 6.0;
        }

        return p;
    }
}

public static class Lab1Task1Program
{
    public static void Main()
    {
        Console.WriteLine("=== Lab 1 / Task 1 (HSL pixel) ===");

        var p1 = new HslPixel(210, 50, 60);
        var p2 = new HslPixel(100, 20, 30);

        Console.WriteLine($"p1 = {p1}");
        Console.WriteLine($"p2 = {p2}");

        Console.WriteLine($"p1 + p2 = {p1 + p2}");
        Console.WriteLine($"p1 - p2 = {p1 - p2}");
        Console.WriteLine($"p1 * 1.5 = {p1 * 1.5}");
        Console.WriteLine($"p1 * p2 = {p1 * p2}");
        Console.WriteLine($"p1 / 2 = {p1 / 2}");

        var (r, g, b) = p1.ToRgb();
        Console.WriteLine($"p1 RGB = ({r}, {g}, {b})");
        Console.WriteLine($"p1 HEX = {p1.ToHex()}");

        var overflowPixel = new HslPixel(760, 130, -20);
        Console.WriteLine($"overflow normalized = {overflowPixel}");

        Assert(overflowPixel.Hue == 40, "Hue normalization failed");
        Assert(overflowPixel.Saturation == 100, "Saturation clamping failed");
        Assert(overflowPixel.Lightness == 0, "Lightness clamping failed");

        var red = new HslPixel(0, 100, 50);
        Assert(red.ToHex() == "#FF0000", "HSL->RGB->HEX conversion failed for red");

        Console.WriteLine("Self-checks passed.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
