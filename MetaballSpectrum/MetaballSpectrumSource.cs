using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Direct2D1;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Shape;
using D2DEffects = Vortice.Direct2D1.Effects;

namespace MetaballSpectrum;

internal sealed class MetaballSpectrumSource : IAudioSpectrumSource
{
    private readonly DisposeCollector _disposer = new();
    private readonly MetaballSpectrumParameter _parameter;
    private readonly byte[] _valueBytes = new byte[MetaballSpectrumCustomEffect.ValueByteSize];

    private readonly D2DEffects.Flood _flood;
    private readonly D2DEffects.Crop _crop;
    private readonly MetaballSpectrumCustomEffect? _effect;

    private bool _isFirst = true;
    private Vector4 _cropRect;
    private Parameters _parameters;

    public ID2D1Image Output { get; }

    public MetaballSpectrumSource(IGraphicsDevicesAndContext devices, MetaballSpectrumParameter parameter)
    {
        _parameter = parameter;

        _flood = new D2DEffects.Flood(devices.DeviceContext);
        _disposer.Collect(_flood);
        _crop = new D2DEffects.Crop(devices.DeviceContext);
        _disposer.Collect(_crop);
        _flood.Color = new Vector4(0f, 0f, 0f, 0f);

        _effect = new MetaballSpectrumCustomEffect(devices);
        _disposer.Collect(_effect);

        if (!_effect.IsEnabled)
        {
            _disposer.RemoveAndDispose(ref _effect);
            _crop.Rectangle = new Vector4(0f, 0f, 0f, 0f);
            using (var output = _flood.Output)
                _crop.SetInput(0, output, true);
        }
        else
        {
            using (var output = _flood.Output)
                _effect.SetInput(0, output, true);
            using (var output = _effect.Output)
                _crop.SetInput(0, output, true);
        }

        var result = _crop.Output;
        _disposer.Collect(result);
        Output = result;
    }

    public void Update(TimelineItemSourceDescription desc, float[] spectrum)
    {
        try
        {
            UpdateCore(desc, spectrum);
        }
        catch (Exception exception)
        {
            MetaballSpectrumTelemetry.Report(exception);
            throw;
        }
    }

    private void UpdateCore(TimelineItemSourceDescription desc, float[] spectrum)
    {
        if (_effect is null)
            return;

        var count = SpectrumResampler.Resample(spectrum, MemoryMarshal.Cast<byte, float>(_valueBytes.AsSpan()));
        var parameters = ReadParameters(desc);

        ApplyChanges(_effect, parameters);
        _effect.ValueCount = count;
        _effect.Values = _valueBytes;

        var crop = parameters.Crop;
        if (_isFirst || _cropRect != crop)
        {
            _crop.Rectangle = crop;
            _cropRect = crop;
        }

        _parameters = parameters;
        _isFirst = false;
    }

    private Parameters ReadParameters(TimelineItemSourceDescription desc)
    {
        var frame = desc.ItemPosition.Frame;
        var length = desc.ItemDuration.Frame;
        var fps = desc.FPS;
        var color = _parameter.MetaballColor;

        return new Parameters(
            (float)_parameter.FieldWidth.GetValue(frame, length, fps),
            (float)_parameter.FieldHeight.GetValue(frame, length, fps),
            (float)_parameter.BlobRadius.GetValue(frame, length, fps),
            (float)(_parameter.Threshold.GetValue(frame, length, fps) / 100.0),
            (float)_parameter.RimThickness.GetValue(frame, length, fps),
            (float)(_parameter.RimBrightness.GetValue(frame, length, fps) / 100.0),
            _parameter.IsBipolar ? 1f : 0f,
            color.R / 255f,
            color.G / 255f,
            color.B / 255f,
            color.A / 255f);
    }

    private void ApplyChanges(MetaballSpectrumCustomEffect effect, in Parameters parameters)
    {
        if (_isFirst || _parameters.FieldWidth != parameters.FieldWidth)
            effect.FieldWidth = parameters.FieldWidth;
        if (_isFirst || _parameters.FieldHeight != parameters.FieldHeight)
            effect.FieldHeight = parameters.FieldHeight;
        if (_isFirst || _parameters.BlobRadius != parameters.BlobRadius)
            effect.BlobRadius = parameters.BlobRadius;
        if (_isFirst || _parameters.Threshold != parameters.Threshold)
            effect.Threshold = parameters.Threshold;
        if (_isFirst || _parameters.RimThickness != parameters.RimThickness)
            effect.RimThickness = parameters.RimThickness;
        if (_isFirst || _parameters.RimBrightness != parameters.RimBrightness)
            effect.RimBrightness = parameters.RimBrightness;
        if (_isFirst || _parameters.Bipolar != parameters.Bipolar)
            effect.Bipolar = parameters.Bipolar;
        if (_isFirst || _parameters.ColorR != parameters.ColorR)
            effect.ColorR = parameters.ColorR;
        if (_isFirst || _parameters.ColorG != parameters.ColorG)
            effect.ColorG = parameters.ColorG;
        if (_isFirst || _parameters.ColorB != parameters.ColorB)
            effect.ColorB = parameters.ColorB;
        if (_isFirst || _parameters.ColorA != parameters.ColorA)
            effect.ColorA = parameters.ColorA;
    }

    public void Dispose()
    {
        try
        {
            DisposeCore();
        }
        catch (Exception exception)
        {
            MetaballSpectrumTelemetry.Report(exception);
            throw;
        }
    }

    private void DisposeCore()
    {
        _crop.SetInput(0, null, true);
        _effect?.SetInput(0, null, true);
        _disposer.DisposeAndClear();
    }

    private readonly record struct Parameters(
        float FieldWidth,
        float FieldHeight,
        float BlobRadius,
        float Threshold,
        float RimThickness,
        float RimBrightness,
        float Bipolar,
        float ColorR,
        float ColorG,
        float ColorB,
        float ColorA)
    {
        private const float AntialiasMargin = 2f;

        public Vector4 Crop
        {
            get
            {
                var margin = BlobRadius + RimThickness * 0.5f + AntialiasMargin;
                var halfWidth = FieldWidth * 0.5f + margin;
                var halfHeight = FieldHeight * 0.5f + margin;
                return new Vector4(-halfWidth, -halfHeight, halfWidth, halfHeight);
            }
        }
    }
}
