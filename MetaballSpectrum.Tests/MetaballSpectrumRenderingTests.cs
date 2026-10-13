using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Direct2D1.Effects;
using YukkuriMovieMaker.Commons;

namespace MetaballSpectrum.Tests;

[Collection("Direct2D")]
public sealed class MetaballSpectrumRenderingTests
{
    const int Tolerance = 3;
    const int Margin = 24;

    static readonly (double R, double G, double B, double A) White = (1d, 1d, 1d, 1d);
    static readonly (double R, double G, double B, double A) Blue = (0.125, 0.4, 1d, 1d);

    static MetaballReference Single(float value, bool bipolar = false, double rim = 4d, double brightness = 0.6, (double R, double G, double B, double A)? tint = null)
        => new(100d, 60d, 20d, 0.45, rim, brightness, bipolar, [value], tint ?? White);

    static byte[] Packed(float[] values)
    {
        var block = new float[MetaballSpectrumCustomEffect.MaxValues];
        values.CopyTo(block, 0);
        return MemoryMarshal.AsBytes(block.AsSpan()).ToArray();
    }

    static Rendering Render(IGraphicsDevicesAndContext devices, MetaballReference spec, int window = 3, int? count = null)
    {
        var context = devices.DeviceContext;
        var halfWidth = (float)(spec.Width * 0.5 + spec.Radius + Margin);
        var halfHeight = (float)(spec.Height * 0.5 + spec.Radius + Margin);
        using var flood = new Flood(context) { Color = Vector4.Zero };
        using var effect = new MetaballSpectrumCustomEffect(devices);
        using var clip = new Crop(context) { Rectangle = new Vector4(-halfWidth, -halfHeight, halfWidth, halfHeight) };
        using var floodOutput = flood.Output;
        effect.SetInput(0, floodOutput, true);
        effect.FieldWidth = (float)spec.Width;
        effect.FieldHeight = (float)spec.Height;
        effect.ValueCount = count ?? spec.Values.Length;
        effect.BlobRadius = (float)spec.Radius;
        effect.Threshold = (float)spec.Threshold;
        effect.RimThickness = (float)spec.RimThickness;
        effect.RimBrightness = (float)spec.RimBrightness;
        effect.Window = window;
        effect.Bipolar = spec.Bipolar ? 1f : 0f;
        effect.ColorR = (float)spec.Tint.R;
        effect.ColorG = (float)spec.Tint.G;
        effect.ColorB = (float)spec.Tint.B;
        effect.ColorA = (float)spec.Tint.A;
        effect.Values = Packed(spec.Values);
        using var effectOutput = effect.Output;
        clip.SetInput(0, effectOutput, true);
        using var output = clip.Output;
        return Rendering.Capture(devices, output);
    }

    static float[] Ramp(int count) => Enumerable.Range(0, count).Select(index => (float)(0.15 + 0.85 * Math.Abs(Math.Sin(index * 0.7)))).ToArray();

    static float[] Wave(int count) => Enumerable.Range(0, count).Select(index => (float)Math.Sin(index * 0.9) * (1f - index / (float)count)).ToArray();

    [Fact]
    public void WithoutValuesNothingIsDrawn()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();

        var rendering = Render(context, Single(1f), count: 0);

        Assert.All(rendering.Coordinates(), point => Assert.Equal(Bgra.Transparent, rendering[point.X, point.Y]));
    }

    [Fact]
    public void ASingleFullBandDrawsAStadiumAlongTheWholeHeight()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();

        var rendering = Render(context, Single(1f, rim: 0d));

        Assert.Equal(Bgra.Opaque(255, 255, 255), rendering[0, 0]);
        Assert.Equal(Bgra.Opaque(255, 255, 255), rendering[-8, 0]);
        Assert.Equal(Bgra.Opaque(255, 255, 255), rendering[7, 0]);
        Assert.Equal(Bgra.Transparent, rendering[-12, 0]);
        Assert.Equal(Bgra.Transparent, rendering[11, 0]);
        Assert.Equal(Bgra.Opaque(255, 255, 255), rendering[0, -30]);
        Assert.Equal(Bgra.Opaque(255, 255, 255), rendering[0, 29]);
    }

    [Fact]
    public void TheSurfaceStandsWhereTheDensityEqualsTheThreshold()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        var rendering = Render(context, Single(1f, rim: 0d));
        var surface = 20d * Math.Sqrt(1d - Math.Cbrt(0.45));

        var edge = rendering.Columns().Where(x => x > 0).Select(x => (X: x, Alpha: rendering[x, 0].Alpha)).First(pixel => pixel.Alpha < byte.MaxValue / 2);

        Assert.InRange(edge.X + MetaballReference.PixelCentre, surface - 1d, surface + 1d);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.01)]
    [InlineData(0.25)]
    [InlineData(1.0)]
    public void ASingleBandMatchesTheReference(float value)
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        var spec = Single(value);

        var rendering = Render(context, spec);

        Assert.Equal(0, spec.Mismatches(rendering, Tolerance));
    }

    [Fact]
    public void AnUnevenSpectrumMatchesTheReference()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        var spec = new MetaballReference(400d, 200d, 30d, 0.45, 4d, 0.6, false, Ramp(24), Blue);

        var rendering = Render(context, spec, window: 4);

        Assert.Equal(0, spec.Mismatches(rendering, Tolerance));
    }

    [Fact]
    public void AWaveformWithBothSignsMatchesTheReference()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        var spec = new MetaballReference(400d, 200d, 30d, 0.45, 4d, 0.6, true, Wave(24), Blue);

        var rendering = Render(context, spec, window: 4);

        Assert.Equal(0, spec.Mismatches(rendering, Tolerance));
    }

    [Theory]
    [InlineData(0.05, 0d, 0.6)]
    [InlineData(0.9, 12d, 1d)]
    [InlineData(0.45, 30d, 0d)]
    public void TheThresholdAndTheRimMatchTheReference(double threshold, double rim, double brightness)
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        var spec = new MetaballReference(400d, 200d, 30d, threshold, rim, brightness, false, Ramp(16), Blue);

        var rendering = Render(context, spec, window: 4);

        Assert.Equal(0, spec.Mismatches(rendering, Tolerance));
    }

    [Fact]
    public void ATranslucentTintIsAppliedPremultiplied()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        var spec = Single(1f, rim: 0d, tint: (0.2, 0.4, 0.8, 0.6));

        var rendering = Render(context, spec);

        Assert.Equal(new Bgra(122, 61, 31, 153), rendering[0, 0]);
        Assert.Equal(0, spec.Mismatches(rendering, Tolerance));
    }

    [Fact]
    public void AZeroRimThicknessDrawsNoRim()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        var spec = new MetaballReference(400d, 200d, 30d, 0.45, 0d, 1d, false, Ramp(16), Blue);

        var rendering = Render(context, spec, window: 4);

        Assert.All(rendering.Coordinates().Where(point => rendering[point.X, point.Y].Alpha == byte.MaxValue), point => Assert.Equal(Bgra.Opaque(255, 102, 32), rendering[point.X, point.Y]));
    }

    [Fact]
    public void ABrightRimPullsTheEdgeTowardWhite()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        var plain = Render(context, new MetaballReference(400d, 200d, 30d, 0.45, 6d, 0d, false, Ramp(16), Blue), window: 4);
        var bright = Render(context, new MetaballReference(400d, 200d, 30d, 0.45, 6d, 1d, false, Ramp(16), Blue), window: 4);

        var whitened = bright.Coordinates().Count(point => bright[point.X, point.Y].Red > plain[point.X, point.Y].Red + 40);

        Assert.True(whitened > 100, whitened.ToString());
        Assert.All(plain.Coordinates().Where(point => plain[point.X, point.Y].Alpha == byte.MaxValue), point => Assert.Equal(32, plain[point.X, point.Y].Red));
    }

    [Fact]
    public void ABlobReachesTheNeighboursBeyondEightColumns()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        var spec = new MetaballReference(200d, 100d, 60d, 0.45, 4d, 0.6, false, Ramp(64), Blue);
        var needed = (int)Math.Ceiling(spec.Radius / (spec.Width / 64)) + 1;

        var rendering = Render(context, spec, window: needed);

        Assert.True(needed > 8);
        Assert.Equal(0, spec.Mismatches(rendering, Tolerance));
    }

    [Fact]
    public void TheSameSettingsAlwaysProduceTheSamePixels()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        var spec = new MetaballReference(400d, 200d, 30d, 0.45, 4d, 0.6, true, Wave(24), Blue);

        var first = Render(context, spec, window: 4);
        var second = Render(context, spec, window: 4);

        Assert.True(first.SamePixelsAs(second));
    }
}
