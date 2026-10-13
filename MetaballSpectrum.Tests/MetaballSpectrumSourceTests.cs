using System.Globalization;
using System.Windows.Media;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Json;
using YukkuriMovieMaker.Plugin.Shape;

namespace MetaballSpectrum.Tests;

[Collection("Direct2D")]
public sealed class MetaballSpectrumSourceTests
{
    const int Length = 60;
    const int Tolerance = 3;

    static readonly Color Steel = Color.FromRgb(32, 102, 255);
    static readonly float[] Silent = new float[32];
    static readonly float[] Loud = Enumerable.Repeat(1f, 32).ToArray();
    static readonly float[] Uneven = Enumerable.Range(0, 32).Select(index => (float)(0.1 + 0.9 * Math.Abs(Math.Sin(index * 0.7)))).ToArray();
    static readonly float[] Waveform = Enumerable.Range(0, 32).Select(index => (float)Math.Sin(index * 0.9) * (1f - index / 32f)).ToArray();

    static MetaballSpectrumParameter Parameter(double width = 200d, double height = 100d, double radius = 20d, double threshold = 45d, double rim = 4d, double brightness = 60d, bool bipolar = false, Color? color = null)
    {
        var parameter = new MetaballSpectrumParameter { IsBipolar = bipolar, MetaballColor = color ?? Steel };
        parameter.FieldWidth.Values[0].Value = width;
        parameter.FieldHeight.Values[0].Value = height;
        parameter.BlobRadius.Values[0].Value = radius;
        parameter.Threshold.Values[0].Value = threshold;
        parameter.RimThickness.Values[0].Value = rim;
        parameter.RimBrightness.Values[0].Value = brightness;
        return parameter;
    }

    static Rendering RenderFrame(IGraphicsDevicesAndContext devices, IAudioSpectrumSource source, int frame, float[] spectrum)
    {
        source.Update(ItemDescriptions.At(frame, Length), spectrum);
        return Rendering.Capture(devices, source.Output);
    }

    static MetaballReference ReferenceOf(MetaballSpectrumParameter parameter, int frame, float[] spectrum)
    {
        var values = new float[MetaballSpectrumCustomEffect.MaxValues];
        var count = SpectrumResampler.Resample(spectrum, values);
        var color = parameter.MetaballColor;
        return new MetaballReference(
            parameter.FieldWidth.GetValue(frame, Length, ItemDescriptions.Fps),
            parameter.FieldHeight.GetValue(frame, Length, ItemDescriptions.Fps),
            parameter.BlobRadius.GetValue(frame, Length, ItemDescriptions.Fps),
            parameter.Threshold.GetValue(frame, Length, ItemDescriptions.Fps) / 100d,
            parameter.RimThickness.GetValue(frame, Length, ItemDescriptions.Fps),
            parameter.RimBrightness.GetValue(frame, Length, ItemDescriptions.Fps) / 100d,
            parameter.IsBipolar,
            values[..count],
            (color.R / 255d, color.G / 255d, color.B / 255d, color.A / 255d));
    }

    static int Mismatches(Rendering rendering, MetaballReference reference)
        => rendering.Coordinates().Count(point =>
        {
            var actual = rendering[point.X, point.Y];
            var expected = reference.At(point.X, point.Y);
            return new[] { actual.Blue - expected.Blue, actual.Green - expected.Green, actual.Red - expected.Red, actual.Alpha - expected.Alpha }.Max(Math.Abs) > Tolerance;
        });

    static bool Transparent(Rendering rendering) => rendering.Coordinates().All(point => rendering[point.X, point.Y] == Bgra.Transparent);

    static Animation Linear(double from, double to)
        => Json.LoadFromText<Animation>(string.Create(CultureInfo.InvariantCulture, $$"""{"AnimationType":"直線移動","Values":[{"Value":{{from}}},{"Value":{{to}}}]}"""))!;

    [Theory]
    [InlineData(200d, 100d, 20d, 4d, 124, 74)]
    [InlineData(600d, 300d, 36d, 4d, 340, 190)]
    [InlineData(100d, 50d, 10d, 0d, 62, 37)]
    [InlineData(100d, 50d, 10d, 30d, 77, 52)]
    public void TheOutputIsCroppedToTheFieldPlusTheReachOfABlob(double width, double height, double radius, double rim, int halfWidth, int halfHeight)
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = Parameter(width, height, radius, rim: rim).CreateShapeSource(context);

        var rendering = RenderFrame(context, source, 0, Silent);

        Assert.Equal((-halfWidth, -halfHeight, 2 * halfWidth, 2 * halfHeight), (rendering.Left, rendering.Top, rendering.Width, rendering.Height));
    }

    public static readonly TheoryData<string, double, double, double, double, double, bool, float[]> Reaches = new()
    {
        { "loud", 200d, 100d, 20d, 4d, 45d, false, Loud },
        { "loud wide radius", 200d, 100d, 120d, 4d, 45d, false, Loud },
        { "thin surface", 200d, 100d, 20d, 4d, 99.9d, false, Loud },
        { "fat surface", 200d, 100d, 20d, 4d, 0.1d, false, Loud },
        { "thick rim", 200d, 100d, 20d, 30d, 0.1d, false, Loud },
        { "silent", 200d, 100d, 20d, 4d, 0.1d, false, Silent },
        { "bipolar", 200d, 100d, 20d, 30d, 0.1d, true, Waveform },
        { "narrow", 20d, 100d, 120d, 30d, 0.1d, false, Loud },
        { "short", 200d, 4d, 120d, 30d, 0.1d, true, Waveform },
    };

    [Theory]
    [MemberData(nameof(Reaches))]
    public void NoDrawnPixelTouchesTheCrop(string name, double width, double height, double radius, double rim, double threshold, bool bipolar, float[] spectrum)
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = Parameter(width, height, radius, threshold, rim, bipolar: bipolar).CreateShapeSource(context);

        var rendering = RenderFrame(context, source, 0, spectrum);

        Assert.True(rendering.Coordinates().Any(point => rendering[point.X, point.Y].Alpha > 0), name);
        foreach (var x in rendering.Columns())
        {
            Assert.True(rendering[x, rendering.Top] == Bgra.Transparent, $"{name} ({x}, {rendering.Top})");
            Assert.True(rendering[x, rendering.Bottom - 1] == Bgra.Transparent, $"{name} ({x}, {rendering.Bottom - 1})");
        }

        foreach (var y in rendering.Rows())
        {
            Assert.True(rendering[rendering.Left, y] == Bgra.Transparent, $"{name} ({rendering.Left}, {y})");
            Assert.True(rendering[rendering.Right - 1, y] == Bgra.Transparent, $"{name} ({rendering.Right - 1}, {y})");
        }
    }

    [Fact]
    public void TheCropIsSymmetricAboutTheOrigin()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = Parameter().CreateShapeSource(context);

        var rendering = RenderFrame(context, source, 0, Loud);

        Assert.Equal(-rendering.Left, rendering.Right);
        Assert.Equal(-rendering.Top, rendering.Bottom);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WithoutSpectrumBandsNothingIsDrawn(bool bipolar)
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = Parameter(bipolar: bipolar).CreateShapeSource(context);

        Assert.True(Transparent(RenderFrame(context, source, 0, [])));
    }

    [Fact]
    public void WithoutASpectrumNothingIsDrawn()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = Parameter().CreateShapeSource(context);

        Assert.True(Transparent(RenderFrame(context, source, 0, null!)));
    }

    [Theory]
    [InlineData(false, 32)]
    [InlineData(true, 32)]
    [InlineData(false, 3)]
    [InlineData(false, 100)]
    [InlineData(false, 256)]
    [InlineData(true, 300)]
    public void EveryFrameMatchesTheReference(bool bipolar, int bands)
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        var parameter = Parameter(bipolar: bipolar);
        var spectrum = Enumerable.Range(0, bands).Select(index => (float)(Math.Sin(index * 0.9) * (0.2 + 0.8 * Math.Abs(Math.Sin(index * 0.37))))).ToArray();
        using var source = parameter.CreateShapeSource(context);

        var rendering = RenderFrame(context, source, 5, spectrum);

        Assert.Equal(0, Mismatches(rendering, ReferenceOf(parameter, 5, spectrum)));
    }

    [Theory]
    [InlineData(200d, 100d, 120d, 64)]
    [InlineData(200d, 100d, 120d, 256)]
    [InlineData(20d, 100d, 120d, 32)]
    [InlineData(2d, 60d, 30d, 128)]
    [InlineData(600d, 300d, 120d, 256)]
    public void BlobsMergeWithEveryNeighbourInReach(double width, double height, double radius, int bands)
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        var parameter = Parameter(width, height, radius);
        var spectrum = Enumerable.Range(0, bands).Select(index => (float)(0.1 + 0.9 * Math.Abs(Math.Sin(index * 0.7)))).ToArray();
        using var source = parameter.CreateShapeSource(context);

        var rendering = RenderFrame(context, source, 0, spectrum);

        Assert.Equal(0, Mismatches(rendering, ReferenceOf(parameter, 0, spectrum)));
    }

    [Fact]
    public void ASilentSpectrumDrawsARibbonAlongTheBaseline()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = Parameter(rim: 0d, color: Colors.White).CreateShapeSource(context);

        var rendering = RenderFrame(context, source, 0, Silent);

        Assert.Equal(Bgra.Opaque(255, 255, 255), rendering[0, 49]);
        Assert.Equal(Bgra.Opaque(255, 255, 255), rendering[-90, 48]);
        Assert.Equal(Bgra.Transparent, rendering[0, 0]);
        Assert.Equal(Bgra.Transparent, rendering[0, -50]);
    }

    [Fact]
    public void ASilentSpectrumInBothDirectionsDrawsARibbonAlongTheCentreLine()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = Parameter(rim: 0d, color: Colors.White, bipolar: true).CreateShapeSource(context);

        var rendering = RenderFrame(context, source, 0, Silent);

        Assert.Equal(Bgra.Opaque(255, 255, 255), rendering[0, 0]);
        Assert.Equal(Bgra.Opaque(255, 255, 255), rendering[0, -1]);
        Assert.Equal(Bgra.Transparent, rendering[0, 49]);
        Assert.Equal(Bgra.Transparent, rendering[0, -50]);
    }

    [Fact]
    public void InOneDirectionTheSignOfAValueDoesNotMatter()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = Parameter().CreateShapeSource(context);

        var positive = RenderFrame(context, source, 0, [0.5f, 0.25f, 1f]);
        var negative = RenderFrame(context, source, 0, [-0.5f, -0.25f, -1f]);

        Assert.True(positive.SamePixelsAs(negative));
    }

    [Fact]
    public void InBothDirectionsAPositiveValueRisesAndANegativeValueFalls()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = Parameter(rim: 0d, color: Colors.White, bipolar: true).CreateShapeSource(context);

        var up = RenderFrame(context, source, 0, [1f]);
        var down = RenderFrame(context, source, 0, [-1f]);

        Assert.Equal(Bgra.Opaque(255, 255, 255), up[0, -49]);
        Assert.Equal(Bgra.Transparent, up[0, 49]);
        Assert.Equal(Bgra.Opaque(255, 255, 255), down[0, 48]);
        Assert.Equal(Bgra.Transparent, down[0, -50]);
    }

    [Fact]
    public void BandsBeyondTheCapacityAreFolded()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        var folded = Enumerable.Range(0, MetaballSpectrumCustomEffect.MaxValues).Select(index => (index % 7) / 7f).ToArray();
        var doubled = folded.SelectMany(value => new[] { value, value }).ToArray();
        using var source = Parameter().CreateShapeSource(context);

        var direct = RenderFrame(context, source, 3, folded);
        var paired = RenderFrame(context, source, 3, doubled);

        Assert.True(direct.SamePixelsAs(paired));
    }

    [Fact]
    public void BandsThatAreNotFiniteCountAsSilence()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = Parameter().CreateShapeSource(context);

        var silent = RenderFrame(context, source, 0, [0f, 1f]);
        var broken = RenderFrame(context, source, 0, [float.NaN, 1f]);

        Assert.True(silent.SamePixelsAs(broken));
    }

    [Fact]
    public void BandsBeyondTheUnitRangeAreClamped()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = Parameter(bipolar: true).CreateShapeSource(context);

        var unit = RenderFrame(context, source, 0, [1f, -1f]);
        var beyond = RenderFrame(context, source, 0, [5f, -3f]);

        Assert.True(unit.SamePixelsAs(beyond));
    }

    [Fact]
    public void TheColorReachesTheBlobs()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = Parameter(rim: 0d, color: Color.FromArgb(153, 51, 102, 204)).CreateShapeSource(context);

        var rendering = RenderFrame(context, source, 0, Loud);

        Assert.Equal(new Bgra(204, 102, 51, 153).Premultiplied(), rendering[0, 0]);
    }

    public static readonly TheoryData<string, Action<MetaballSpectrumParameter>> LaterChanges = new()
    {
        { nameof(MetaballSpectrumParameter.FieldWidth), parameter => parameter.FieldWidth.Values[0].Value = 140d },
        { nameof(MetaballSpectrumParameter.FieldHeight), parameter => parameter.FieldHeight.Values[0].Value = 60d },
        { nameof(MetaballSpectrumParameter.BlobRadius), parameter => parameter.BlobRadius.Values[0].Value = 12d },
        { nameof(MetaballSpectrumParameter.Threshold), parameter => parameter.Threshold.Values[0].Value = 80d },
        { nameof(MetaballSpectrumParameter.RimThickness), parameter => parameter.RimThickness.Values[0].Value = 12d },
        { nameof(MetaballSpectrumParameter.RimBrightness), parameter => parameter.RimBrightness.Values[0].Value = 0d },
        { nameof(MetaballSpectrumParameter.IsBipolar), parameter => parameter.IsBipolar = true },
        { nameof(Color.R), parameter => parameter.MetaballColor = Color.FromArgb(255, 200, 102, 255) },
        { nameof(Color.G), parameter => parameter.MetaballColor = Color.FromArgb(255, 32, 200, 255) },
        { nameof(Color.B), parameter => parameter.MetaballColor = Color.FromArgb(255, 32, 102, 10) },
        { nameof(Color.A), parameter => parameter.MetaballColor = Color.FromArgb(128, 32, 102, 255) },
    };

    [Theory]
    [MemberData(nameof(LaterChanges))]
    public void EverySettingChangedAfterTheFirstFrameReachesTheBlobs(string setting, Action<MetaballSpectrumParameter> change)
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        var parameter = Parameter();
        var changed = Parameter();
        change(changed);
        using var source = parameter.CreateShapeSource(context);
        using var rebuilt = changed.CreateShapeSource(context);

        var before = RenderFrame(context, source, 5, Waveform);
        change(parameter);
        var after = RenderFrame(context, source, 5, Waveform);
        var expected = RenderFrame(context, rebuilt, 5, Waveform);

        Assert.False(before.SamePixelsAs(after), setting);
        Assert.True(after.SamePixelsAs(expected), setting);
    }

    [Fact]
    public void AChangedSpectrumReachesTheNextFrame()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = Parameter().CreateShapeSource(context);

        var quiet = RenderFrame(context, source, 0, [0.1f, 0.1f]);
        var loud = RenderFrame(context, source, 0, [1f, 1f]);
        var again = RenderFrame(context, source, 0, [0.1f, 0.1f]);

        Assert.False(quiet.SamePixelsAs(loud));
        Assert.True(quiet.SamePixelsAs(again));
    }

    [Fact]
    public void AnimatedHeightIsReadAtEachFrame()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        var parameter = Parameter(rim: 0d, color: Colors.White);
        parameter.FieldHeight.CopyFrom(Linear(20d, 100d));
        using var source = parameter.CreateShapeSource(context);

        var start = RenderFrame(context, source, 0, [1f]);
        var end = RenderFrame(context, source, Length - 1, [1f]);

        Assert.True(end.Height > start.Height);
        Assert.Equal(Bgra.Transparent, start[0, -25]);
        Assert.Equal(Bgra.Opaque(255, 255, 255), end[0, -45]);
        Assert.Equal(0, Mismatches(end, ReferenceOf(parameter, Length - 1, [1f])));
    }

    [Fact]
    public void TheSameInputsAlwaysProduceTheSamePixels()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var first = Parameter().CreateShapeSource(context);
        using var second = Parameter().CreateShapeSource(context);

        var one = RenderFrame(context, first, 3, Uneven);
        var again = RenderFrame(context, first, 3, Uneven);
        var other = RenderFrame(context, second, 3, Uneven);

        Assert.True(one.SamePixelsAs(again));
        Assert.True(one.SamePixelsAs(other));
    }

    [Fact]
    public void AFailureWhileUpdatingIsNotSwallowed()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = Parameter().CreateShapeSource(context);

        Assert.ThrowsAny<Exception>(() => source.Update(null!, Silent));
    }
}
