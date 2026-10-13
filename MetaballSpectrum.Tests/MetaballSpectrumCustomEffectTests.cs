using Vortice.Direct2D1;
using YukkuriMovieMaker.Commons;
using static MetaballSpectrum.MetaballSpectrumCustomEffect;

namespace MetaballSpectrum.Tests;

[Collection("Direct2D")]
public sealed class MetaballSpectrumCustomEffectTests
{
    const string ValuesName = "Values";

    static float Read(MetaballSpectrumCustomEffect effect, PropertyIndex index) => effect.GetFloatValue((int)index);

    static byte[] ReadValues(MetaballSpectrumCustomEffect effect)
    {
        var values = new byte[effect.GetValueSize((int)PropertyIndex.Values)];
        effect.GetValueByName(ValuesName, PropertyType.Blob, values, values.Length);
        return values;
    }

    static void Write(MetaballSpectrumCustomEffect effect, int channel, float value)
    {
        switch ((PropertyIndex)channel)
        {
            case PropertyIndex.ColorR:
                effect.ColorR = value;
                break;
            case PropertyIndex.ColorG:
                effect.ColorG = value;
                break;
            case PropertyIndex.ColorB:
                effect.ColorB = value;
                break;
            default:
                effect.ColorA = value;
                break;
        }
    }

    [Fact]
    public void TheEffectIsEnabledOnceCreated()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var effect = new MetaballSpectrumCustomEffect(context);

        Assert.True(effect.IsEnabled);
    }

    [Fact]
    public void ThePropertiesStartFromZero()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var effect = new MetaballSpectrumCustomEffect(context);

        Assert.All(Enum.GetValues<PropertyIndex>().Where(index => index != PropertyIndex.Values), index => Assert.Equal(0f, Read(effect, index)));
        Assert.Equal(new byte[ValueByteSize], ReadValues(effect));
    }

    [Theory]
    [InlineData((int)PropertyIndex.FieldWidth, -1f, 0.001f)]
    [InlineData((int)PropertyIndex.FieldWidth, 0f, 0.001f)]
    [InlineData((int)PropertyIndex.FieldWidth, 0.0005f, 0.001f)]
    [InlineData((int)PropertyIndex.FieldWidth, 0.001f, 0.001f)]
    [InlineData((int)PropertyIndex.FieldWidth, 600f, 600f)]
    [InlineData((int)PropertyIndex.FieldWidth, float.MaxValue, float.MaxValue)]
    [InlineData((int)PropertyIndex.FieldHeight, -1f, 0.001f)]
    [InlineData((int)PropertyIndex.FieldHeight, 0f, 0.001f)]
    [InlineData((int)PropertyIndex.FieldHeight, 0.0005f, 0.001f)]
    [InlineData((int)PropertyIndex.FieldHeight, 300f, 300f)]
    [InlineData((int)PropertyIndex.FieldHeight, float.MaxValue, float.MaxValue)]
    [InlineData((int)PropertyIndex.BlobRadius, -1f, 0.001f)]
    [InlineData((int)PropertyIndex.BlobRadius, 0f, 0.001f)]
    [InlineData((int)PropertyIndex.BlobRadius, 0.0005f, 0.001f)]
    [InlineData((int)PropertyIndex.BlobRadius, 36f, 36f)]
    [InlineData((int)PropertyIndex.BlobRadius, float.MaxValue, float.MaxValue)]
    public void TheSizesNeverDropBelowAThousandthOfAPixel(int index, float value, float expected)
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var effect = new MetaballSpectrumCustomEffect(context);
        effect.FieldWidth = 5f;
        effect.FieldHeight = 5f;
        effect.BlobRadius = 5f;

        switch ((PropertyIndex)index)
        {
            case PropertyIndex.FieldWidth:
                effect.FieldWidth = value;
                break;
            case PropertyIndex.FieldHeight:
                effect.FieldHeight = value;
                break;
            default:
                effect.BlobRadius = value;
                break;
        }

        Assert.Equal(expected, effect.GetFloatValue(index));
    }

    [Theory]
    [InlineData(-1f, 0f)]
    [InlineData(0f, 0f)]
    [InlineData(2.5f, 2.5f)]
    [InlineData(32f, 32f)]
    [InlineData((float)MaxValues, (float)MaxValues)]
    [InlineData(MaxValues + 1f, (float)MaxValues)]
    [InlineData(float.MaxValue, (float)MaxValues)]
    public void TheValueCountIsClampedToTheShaderCapacity(float value, float expected)
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var effect = new MetaballSpectrumCustomEffect(context);

        effect.ValueCount = value;

        Assert.Equal(expected, Read(effect, PropertyIndex.ValueCount));
    }

    [Theory]
    [InlineData(-1f, 0.001f)]
    [InlineData(0f, 0.001f)]
    [InlineData(0.0005f, 0.001f)]
    [InlineData(0.45f, 0.45f)]
    [InlineData(0.999f, 0.999f)]
    [InlineData(1f, 0.999f)]
    [InlineData(float.MaxValue, 0.999f)]
    public void TheThresholdStaysInsideTheOpenUnitRange(float value, float expected)
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var effect = new MetaballSpectrumCustomEffect(context);
        effect.Threshold = 0.5f;

        effect.Threshold = value;

        Assert.Equal(expected, Read(effect, PropertyIndex.Threshold));
    }

    [Theory]
    [InlineData(-5f, 0f)]
    [InlineData(0f, 0f)]
    [InlineData(4f, 4f)]
    [InlineData(float.MaxValue, float.MaxValue)]
    public void TheRimThicknessNeverGoesNegative(float value, float expected)
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var effect = new MetaballSpectrumCustomEffect(context);

        effect.RimThickness = value;

        Assert.Equal(expected, Read(effect, PropertyIndex.RimThickness));
    }

    [Theory]
    [InlineData(-0.5f, 0f)]
    [InlineData(0.25f, 0.25f)]
    [InlineData(1.5f, 1f)]
    public void TheRimBrightnessIsClampedToTheUnitRange(float value, float expected)
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var effect = new MetaballSpectrumCustomEffect(context);

        effect.RimBrightness = value;

        Assert.Equal(expected, Read(effect, PropertyIndex.RimBrightness));
    }

    [Theory]
    [InlineData(-1f, 0f)]
    [InlineData(0.5f, 0.5f)]
    [InlineData(2f, 1f)]
    public void TheBipolarSwitchIsClampedToTheUnitRange(float value, float expected)
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var effect = new MetaballSpectrumCustomEffect(context);

        effect.Bipolar = value;

        Assert.Equal(expected, Read(effect, PropertyIndex.Bipolar));
    }

    [Theory]
    [InlineData((int)PropertyIndex.ColorR, -0.5f, 0f)]
    [InlineData((int)PropertyIndex.ColorR, 0.25f, 0.25f)]
    [InlineData((int)PropertyIndex.ColorR, 1.5f, 1f)]
    [InlineData((int)PropertyIndex.ColorG, -0.5f, 0f)]
    [InlineData((int)PropertyIndex.ColorG, 0.25f, 0.25f)]
    [InlineData((int)PropertyIndex.ColorG, 1.5f, 1f)]
    [InlineData((int)PropertyIndex.ColorB, -0.5f, 0f)]
    [InlineData((int)PropertyIndex.ColorB, 0.25f, 0.25f)]
    [InlineData((int)PropertyIndex.ColorB, 1.5f, 1f)]
    [InlineData((int)PropertyIndex.ColorA, -0.5f, 0f)]
    [InlineData((int)PropertyIndex.ColorA, 0.25f, 0.25f)]
    [InlineData((int)PropertyIndex.ColorA, 1.5f, 1f)]
    public void EveryColorChannelIsClampedToTheUnitRange(int channel, float value, float expected)
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var effect = new MetaballSpectrumCustomEffect(context);

        Write(effect, channel, value);

        Assert.Equal(expected, effect.GetFloatValue(channel));
    }

    [Fact]
    public void AShorterValueBlockFillsTheFrontAndClearsTheRest()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var effect = new MetaballSpectrumCustomEffect(context);
        effect.Values = Enumerable.Repeat((byte)7, ValueByteSize).ToArray();

        effect.Values = [1, 2, 3];

        var expected = new byte[ValueByteSize];
        expected[0] = 1;
        expected[1] = 2;
        expected[2] = 3;
        Assert.Equal(expected, ReadValues(effect));
    }

    [Fact]
    public void ALongerValueBlockIsTruncatedToTheShaderCapacity()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var effect = new MetaballSpectrumCustomEffect(context);

        effect.Values = Enumerable.Range(1, ValueByteSize + 40).Select(index => (byte)index).ToArray();

        Assert.Equal(Enumerable.Range(1, ValueByteSize).Select(index => (byte)index), ReadValues(effect));
    }
}
