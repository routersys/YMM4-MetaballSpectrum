using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Plugin.Shape;
using YukkuriMovieMaker.Project;

namespace MetaballSpectrum;

public class MetaballSpectrumParameter : AudioSpectrumParameterBase
{
    public MetaballSpectrumParameter(SharedDataStore? sharedData = null) : base(sharedData)
    {
        MetaballSpectrumTelemetry.EnsureStartedOnce();
        MetaballSpectrumUpdateNotifier.EnsureCheckedOnce();
    }

    public MetaballSpectrumParameter() : this(null)
    {
    }

    [Display(Name = nameof(Texts.FieldWidth), Description = nameof(Texts.FieldWidthDescription), Order = 10, ResourceType = typeof(Texts))]
    [AnimationSlider("F1", "px", 0, 1920)]
    public Animation FieldWidth { get; } = new Animation(600, 0.01, YMM4Constants.VeryLargeValue);

    [Display(Name = nameof(Texts.FieldHeight), Description = nameof(Texts.FieldHeightDescription), Order = 11, ResourceType = typeof(Texts))]
    [AnimationSlider("F1", "px", 0, 1080)]
    public Animation FieldHeight { get; } = new Animation(300, 0.01, YMM4Constants.VeryLargeValue);

    [Display(Name = nameof(Texts.BlobRadius), Description = nameof(Texts.BlobRadiusDescription), Order = 12, ResourceType = typeof(Texts))]
    [AnimationSlider("F1", "px", 1, 120)]
    public Animation BlobRadius { get; } = new Animation(36, 0.01, YMM4Constants.VeryLargeValue);

    [Display(Name = nameof(Texts.Threshold), Description = nameof(Texts.ThresholdDescription), Order = 13, ResourceType = typeof(Texts))]
    [AnimationSlider("F1", "%", 5, 95)]
    public Animation Threshold { get; } = new Animation(45, 0.1, 99.9);

    [Display(Name = nameof(Texts.RimThickness), Description = nameof(Texts.RimThicknessDescription), Order = 14, ResourceType = typeof(Texts))]
    [AnimationSlider("F1", "px", 0, 30)]
    public Animation RimThickness { get; } = new Animation(4, 0, YMM4Constants.VeryLargeValue);

    [Display(Name = nameof(Texts.RimBrightness), Description = nameof(Texts.RimBrightnessDescription), Order = 15, ResourceType = typeof(Texts))]
    [AnimationSlider("F1", "%", 0, 100)]
    public Animation RimBrightness { get; } = new Animation(60, 0, 100);

    [Display(Name = nameof(Texts.IsBipolar), Description = nameof(Texts.IsBipolarDescription), Order = 16, ResourceType = typeof(Texts))]
    [ToggleSlider]
    public bool IsBipolar { get => isBipolar; set => Set(ref isBipolar, value); }
    private bool isBipolar;

    [Display(Name = nameof(Texts.MetaballColor), Description = nameof(Texts.MetaballColorDescription), Order = 17, ResourceType = typeof(Texts))]
    [ColorPicker]
    public Color MetaballColor { get => metaballColor; set => Set(ref metaballColor, value); }
    private Color metaballColor = Colors.White;

    public override IEnumerable<string> CreateShapeItemExoFilter(int keyFrameIndex, ExoOutputDescription desc, AudioSpectrumExoOutputDescription spectrumParameters) => [];

    public override IEnumerable<string> CreateMaskExoFilter(int keyFrameIndex, ExoOutputDescription desc, ShapeMaskExoOutputDescription shapeMaskParameters, AudioSpectrumExoOutputDescription spectrumParameters) => [];

    public override IAudioSpectrumSource CreateShapeSource(IGraphicsDevicesAndContext devices)
    {
        try
        {
            return new MetaballSpectrumSource(devices, this);
        }
        catch (Exception exception)
        {
            MetaballSpectrumTelemetry.Report(exception);
            throw;
        }
    }

    protected override IEnumerable<IAnimatable> GetAnimatables() =>
        [FieldWidth, FieldHeight, BlobRadius, Threshold, RimThickness, RimBrightness];

    protected override void LoadSharedData(SharedDataStore store)
    {
        if (store.Load<MetaballSpectrumParameter>() is { } saved)
            CopyFrom(saved);
    }

    protected override void SaveSharedData(SharedDataStore store)
    {
        var saved = new MetaballSpectrumParameter();
        saved.CopyFrom(this);
        store.Save(saved);
    }

    private void CopyFrom(MetaballSpectrumParameter source)
    {
        FieldWidth.CopyFrom(source.FieldWidth);
        FieldHeight.CopyFrom(source.FieldHeight);
        BlobRadius.CopyFrom(source.BlobRadius);
        Threshold.CopyFrom(source.Threshold);
        RimThickness.CopyFrom(source.RimThickness);
        RimBrightness.CopyFrom(source.RimBrightness);
        IsBipolar = source.IsBipolar;
        MetaballColor = source.MetaballColor;
    }
}
