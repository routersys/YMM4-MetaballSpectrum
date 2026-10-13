using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Windows.Media;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Json;
using YukkuriMovieMaker.Plugin.Shape;
using YukkuriMovieMaker.Project;

namespace MetaballSpectrum.Tests;

public sealed class MetaballSpectrumParameterTests
{
    const double VeryLargeValue = YMM4Constants.VeryLargeValue;

    static PropertyInfo Property(string name) => typeof(MetaballSpectrumParameter).GetProperty(name)!;

    static T Attribute<T>(string property) where T : Attribute => Property(property).GetCustomAttribute<T>()!;

    static ExoOutputDescription ExoDescription() => new(new VideoInfo(), string.Empty, new AviUtlDirectories(string.Empty, string.Empty));

    static AudioSpectrumExoOutputDescription SpectrumDescription() => new(string.Empty, new Animation(100, 0, 100), TimeSpan.Zero, new Animation(32, 1, 1000));

    static ShapeMaskExoOutputDescription MaskDescription() => new(true, new Animation(0, -VeryLargeValue, VeryLargeValue), new Animation(0, -VeryLargeValue, VeryLargeValue), new Animation(0, -VeryLargeValue, VeryLargeValue), new Animation(0, 0, VeryLargeValue), false);

    static Animation Linear(double from, double to)
        => Json.LoadFromText<Animation>(string.Create(CultureInfo.InvariantCulture, $$"""{"AnimationType":"直線移動","Values":[{"Value":{{from}}},{"Value":{{to}}}]}"""))!;

    static MetaballSpectrumParameter Configured()
    {
        var parameter = new MetaballSpectrumParameter { IsBipolar = true, MetaballColor = Color.FromArgb(200, 10, 20, 30) };
        parameter.FieldWidth.Values[0].Value = 300d;
        parameter.FieldHeight.Values[0].Value = 150d;
        parameter.BlobRadius.Values[0].Value = 21d;
        parameter.Threshold.Values[0].Value = 62.5d;
        parameter.RimThickness.Values[0].Value = 7d;
        parameter.RimBrightness.Values[0].Value = 33d;
        return parameter;
    }

    static void AssertConfigured(MetaballSpectrumParameter parameter)
    {
        Assert.True(parameter.IsBipolar);
        Assert.Equal(Color.FromArgb(200, 10, 20, 30), parameter.MetaballColor);
        Assert.Equal(300d, parameter.FieldWidth.GetValue(0, 1, ItemDescriptions.Fps));
        Assert.Equal(150d, parameter.FieldHeight.GetValue(0, 1, ItemDescriptions.Fps));
        Assert.Equal(21d, parameter.BlobRadius.GetValue(0, 1, ItemDescriptions.Fps));
        Assert.Equal(62.5d, parameter.Threshold.GetValue(0, 1, ItemDescriptions.Fps));
        Assert.Equal(7d, parameter.RimThickness.GetValue(0, 1, ItemDescriptions.Fps));
        Assert.Equal(33d, parameter.RimBrightness.GetValue(0, 1, ItemDescriptions.Fps));
    }

    [Theory]
    [InlineData(nameof(MetaballSpectrumParameter.FieldWidth), 600d, 0.01d, VeryLargeValue)]
    [InlineData(nameof(MetaballSpectrumParameter.FieldHeight), 300d, 0.01d, VeryLargeValue)]
    [InlineData(nameof(MetaballSpectrumParameter.BlobRadius), 36d, 0.01d, VeryLargeValue)]
    [InlineData(nameof(MetaballSpectrumParameter.Threshold), 45d, 0.1d, 99.9d)]
    [InlineData(nameof(MetaballSpectrumParameter.RimThickness), 4d, 0d, VeryLargeValue)]
    [InlineData(nameof(MetaballSpectrumParameter.RimBrightness), 60d, 0d, 100d)]
    public void AnimatedParametersStartFromTheirDefaultsWithinTheirRange(string name, double defaultValue, double minimum, double maximum)
    {
        var parameter = new MetaballSpectrumParameter();

        var animation = (Animation)Property(name).GetValue(parameter)!;

        Assert.Equal(defaultValue, animation.DefaultValue);
        Assert.Equal(minimum, animation.MinValue);
        Assert.Equal(maximum, animation.MaxValue);
        Assert.Equal(defaultValue, animation.GetValue(0, 1, ItemDescriptions.Fps));
    }

    [Fact]
    public void TheBipolarSwitchAndTheColorStartFromTheirDefaults()
    {
        var parameter = new MetaballSpectrumParameter();

        Assert.False(parameter.IsBipolar);
        Assert.Equal(Colors.White, parameter.MetaballColor);
    }

    [Fact]
    public void ChangingTheBipolarSwitchOrTheColorNotifiesTheEditor()
    {
        var parameter = new MetaballSpectrumParameter();
        var changed = new List<string?>();
        parameter.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        parameter.IsBipolar = true;
        parameter.MetaballColor = Colors.Red;

        Assert.Equal([nameof(MetaballSpectrumParameter.IsBipolar), nameof(MetaballSpectrumParameter.MetaballColor)], changed);
    }

    [Fact]
    public void AssigningAnUnchangedValueDoesNotNotify()
    {
        var parameter = new MetaballSpectrumParameter();
        var changed = new List<string?>();
        parameter.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        parameter.IsBipolar = false;
        parameter.MetaballColor = Colors.White;

        Assert.Empty(changed);
    }

    [Fact]
    public void TheSixAnimatedParametersReceiveTheAnimationParameters()
    {
        var parameter = new MetaballSpectrumParameter();

        parameter.SetAnimationParameters(120, ItemDescriptions.Fps);

        Assert.All([parameter.FieldWidth, parameter.FieldHeight, parameter.BlobRadius, parameter.Threshold, parameter.RimThickness, parameter.RimBrightness], animation => Assert.Equal(120, animation.Length));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void NoExoFilterIsWrittenForAviUtl(int keyFrameIndex)
    {
        var parameter = new MetaballSpectrumParameter();

        Assert.Empty(parameter.CreateShapeItemExoFilter(keyFrameIndex, ExoDescription(), SpectrumDescription()));
        Assert.Empty(parameter.CreateMaskExoFilter(keyFrameIndex, ExoDescription(), MaskDescription(), SpectrumDescription()));
    }

    [Theory]
    [InlineData(nameof(MetaballSpectrumParameter.FieldWidth), nameof(Texts.FieldWidth), nameof(Texts.FieldWidthDescription), 10)]
    [InlineData(nameof(MetaballSpectrumParameter.FieldHeight), nameof(Texts.FieldHeight), nameof(Texts.FieldHeightDescription), 11)]
    [InlineData(nameof(MetaballSpectrumParameter.BlobRadius), nameof(Texts.BlobRadius), nameof(Texts.BlobRadiusDescription), 12)]
    [InlineData(nameof(MetaballSpectrumParameter.Threshold), nameof(Texts.Threshold), nameof(Texts.ThresholdDescription), 13)]
    [InlineData(nameof(MetaballSpectrumParameter.RimThickness), nameof(Texts.RimThickness), nameof(Texts.RimThicknessDescription), 14)]
    [InlineData(nameof(MetaballSpectrumParameter.RimBrightness), nameof(Texts.RimBrightness), nameof(Texts.RimBrightnessDescription), 15)]
    [InlineData(nameof(MetaballSpectrumParameter.IsBipolar), nameof(Texts.IsBipolar), nameof(Texts.IsBipolarDescription), 16)]
    [InlineData(nameof(MetaballSpectrumParameter.MetaballColor), nameof(Texts.MetaballColor), nameof(Texts.MetaballColorDescription), 17)]
    public void EveryParameterIsDisplayedInOrder(string property, string name, string description, int order)
    {
        var display = Attribute<DisplayAttribute>(property);

        Assert.Null(display.GroupName);
        Assert.Equal(name, display.Name);
        Assert.Equal(description, display.Description);
        Assert.Equal(order, display.Order);
        Assert.Equal(typeof(Texts), display.ResourceType);
    }

    [Theory]
    [InlineData(nameof(MetaballSpectrumParameter.FieldWidth), "F1", "px", 0d, 1920d)]
    [InlineData(nameof(MetaballSpectrumParameter.FieldHeight), "F1", "px", 0d, 1080d)]
    [InlineData(nameof(MetaballSpectrumParameter.BlobRadius), "F1", "px", 1d, 120d)]
    [InlineData(nameof(MetaballSpectrumParameter.Threshold), "F1", "%", 5d, 95d)]
    [InlineData(nameof(MetaballSpectrumParameter.RimThickness), "F1", "px", 0d, 30d)]
    [InlineData(nameof(MetaballSpectrumParameter.RimBrightness), "F1", "%", 0d, 100d)]
    public void AnimatedParametersAreEditedWithAnimationSliders(string property, string format, string unit, double minimum, double maximum)
    {
        var slider = Attribute<AnimationSliderAttribute>(property);

        Assert.Equal(format, slider.StringFormat);
        Assert.Equal(unit, slider.UnitText);
        Assert.Equal(minimum, slider.DefaultMin);
        Assert.Equal(maximum, slider.DefaultMax);
    }

    [Fact]
    public void TheBipolarSwitchIsEditedWithAToggle()
    {
        Assert.NotNull(Attribute<ToggleSliderAttribute>(nameof(MetaballSpectrumParameter.IsBipolar)));
    }

    [Fact]
    public void TheColorIsEditedWithAColorPicker()
    {
        Assert.NotNull(Attribute<ColorPickerAttribute>(nameof(MetaballSpectrumParameter.MetaballColor)));
    }

    [Fact]
    public void EverySettingSurvivesAProjectRoundTrip()
    {
        var parameter = Configured();

        var clone = Json.GetClone(parameter)!;

        Assert.NotSame(parameter, clone);
        AssertConfigured(clone);
    }

    [Fact]
    public void KeyframedAnimationsSurviveAProjectRoundTrip()
    {
        const int length = 60;
        var parameter = new MetaballSpectrumParameter();
        parameter.BlobRadius.CopyFrom(Linear(10d, 90d));

        var clone = Json.GetClone(parameter)!;

        Assert.NotEqual(parameter.BlobRadius.GetValue(0, length, ItemDescriptions.Fps), parameter.BlobRadius.GetValue(length - 1, length, ItemDescriptions.Fps));
        Assert.All([0, length / 2, length - 1], frame => Assert.Equal(parameter.BlobRadius.GetValue(frame, length, ItemDescriptions.Fps), clone.BlobRadius.GetValue(frame, length, ItemDescriptions.Fps)));
    }

    [Fact]
    public void EverySettingIsCarriedThroughTheSharedData()
    {
        var parameter = Configured();

        var restored = new MetaballSpectrumParameter(parameter.GetSharedData());

        Assert.NotSame(parameter, restored);
        AssertConfigured(restored);
    }

    [Fact]
    public void KeyframedAnimationsAreCarriedThroughTheSharedData()
    {
        const int length = 60;
        var parameter = new MetaballSpectrumParameter();
        parameter.FieldWidth.CopyFrom(Linear(100d, 300d));

        var restored = new MetaballSpectrumParameter(parameter.GetSharedData());

        Assert.All([0, length / 2, length - 1], frame => Assert.Equal(parameter.FieldWidth.GetValue(frame, length, ItemDescriptions.Fps), restored.FieldWidth.GetValue(frame, length, ItemDescriptions.Fps)));
        Assert.NotEqual(restored.FieldWidth.GetValue(0, length, ItemDescriptions.Fps), restored.FieldWidth.GetValue(length - 1, length, ItemDescriptions.Fps));
    }

    [Fact]
    public void AnEmptySharedDataLeavesTheDefaults()
    {
        var parameter = new MetaballSpectrumParameter(new SharedDataStore());

        Assert.False(parameter.IsBipolar);
        Assert.Equal(Colors.White, parameter.MetaballColor);
        Assert.Equal(600d, parameter.FieldWidth.GetValue(0, 1, ItemDescriptions.Fps));
    }

    [Fact]
    public void TheSharedDataOfAnotherKindIsIgnored()
    {
        var store = new SharedDataStore();
        store.Save(new object());

        var parameter = new MetaballSpectrumParameter(store);

        Assert.False(parameter.IsBipolar);
        Assert.Equal(36d, parameter.BlobRadius.GetValue(0, 1, ItemDescriptions.Fps));
    }

    [Fact]
    public void AFailureWhileCreatingTheSourceIsNotSwallowed()
    {
        var parameter = new MetaballSpectrumParameter();

        Assert.ThrowsAny<Exception>(() => parameter.CreateShapeSource(null!));
    }
}
