namespace MetaballSpectrum.Tests;

internal sealed record MetaballReference(
    double Width,
    double Height,
    double Radius,
    double Threshold,
    double RimThickness,
    double RimBrightness,
    bool Bipolar,
    float[] Values,
    (double R, double G, double B, double A) Tint)
{
    public const double PixelCentre = 0.5;
    public const double Antialias = 1d;

    public Bgra At(int x, int y)
    {
        var localX = x + PixelCentre;
        var localY = y + PixelCentre;
        var count = Values.Length;
        var halfWidth = Width * 0.5;
        var halfHeight = Height * 0.5;
        var columnWidth = Width / count;
        var baseline = Bipolar ? 0d : halfHeight;
        var span = Bipolar ? halfHeight : Height;

        var density = 0d;
        var gradientX = 0d;
        var gradientY = 0d;
        for (var index = 0; index < count; index++)
        {
            var value = Values[index];
            var reach = (Bipolar ? value : Math.Abs(value)) * span;
            var centreX = -halfWidth + (index + 0.5) * columnWidth;
            var slide = Math.Abs(reach) > 0.0001 ? Math.Clamp((localY - baseline) / -reach, 0d, 1d) : 0d;
            var deltaX = localX - centreX;
            if (Math.Abs(deltaX) >= Radius)
                continue;

            var deltaY = localY - (baseline - slide * reach);
            var ratio = Math.Sqrt(deltaX * deltaX + deltaY * deltaY) / Radius;
            if (ratio >= 1d)
                continue;

            var shell = 1d - ratio * ratio;
            density += shell * shell * shell;
            var gain = -6d * shell * shell / (Radius * Radius);
            gradientX += deltaX * gain;
            gradientY += deltaY * gain;
        }

        var slope = Math.Max(Math.Sqrt(gradientX * gradientX + gradientY * gradientY), 0.00001);
        var signedDistance = (density - Threshold) / slope;
        var body = Math.Clamp(signedDistance / Antialias + 0.5, 0d, 1d);
        var halfRim = RimThickness * 0.5;
        var rim = LineCoverage(Math.Abs(signedDistance), halfRim) * Math.Clamp(RimThickness / Antialias, 0d, 1d);

        var coverage = Math.Max(body, rim) * Tint.A;
        var mix = rim * RimBrightness;
        return new Bgra(
            Quantize((Tint.B + (1d - Tint.B) * mix) * coverage),
            Quantize((Tint.G + (1d - Tint.G) * mix) * coverage),
            Quantize((Tint.R + (1d - Tint.R) * mix) * coverage),
            Quantize(coverage));
    }

    public int Mismatches(Rendering rendering, int tolerance)
    {
        var mismatches = 0;
        for (var y = rendering.Top; y < rendering.Bottom; y++)
        {
            for (var x = rendering.Left; x < rendering.Right; x++)
            {
                var actual = rendering[x, y];
                var expected = At(x, y);
                var difference = Math.Max(
                    Math.Max(Math.Abs(actual.Blue - expected.Blue), Math.Abs(actual.Green - expected.Green)),
                    Math.Max(Math.Abs(actual.Red - expected.Red), Math.Abs(actual.Alpha - expected.Alpha)));
                if (difference > tolerance)
                    mismatches++;
            }
        }

        return mismatches;
    }

    static double LineCoverage(double distance, double halfWidth)
    {
        var edge0 = Math.Max(halfWidth - Antialias, 0d);
        var edge1 = halfWidth + Antialias;
        var t = Math.Clamp((distance - edge0) / (edge1 - edge0), 0d, 1d);
        return 1d - t * t * (3d - 2d * t);
    }

    static byte Quantize(double value) => (byte)Math.Round(Math.Clamp(value, 0d, 1d) * byte.MaxValue, MidpointRounding.AwayFromZero);
}
