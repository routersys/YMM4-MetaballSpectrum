using System.Windows.Media;

namespace MetaballSpectrum.Harness;

internal static class HarnessCases
{
    static readonly Color Blue = Color.FromRgb(40, 120, 255);

    public static IEnumerable<(string Name, MetaballSpectrumParameter Parameter, IReadOnlyList<int> Frames)> All()
    {
        yield return ("default", Create(), [0]);
        yield return ("default-frame-4", Create(), [4]);
        yield return ("default-frames-0-8", Create(), Enumerable.Range(0, 9).ToArray());
        yield return ("width-200", Create(parameter => parameter.FieldWidth.Values[0].Value = 200), [0]);
        yield return ("width-1200", Create(parameter => parameter.FieldWidth.Values[0].Value = 1200), [0]);
        yield return ("height-100", Create(parameter => parameter.FieldHeight.Values[0].Value = 100), [0]);
        yield return ("height-600", Create(parameter => parameter.FieldHeight.Values[0].Value = 600), [0]);
        yield return ("radius-8", Create(parameter => parameter.BlobRadius.Values[0].Value = 8), [0]);
        yield return ("radius-120", Create(parameter => parameter.BlobRadius.Values[0].Value = 120), [0]);
        yield return ("width-200-radius-120", Create(parameter =>
        {
            parameter.FieldWidth.Values[0].Value = 200;
            parameter.BlobRadius.Values[0].Value = 120;
        }), [0]);
        yield return ("threshold-10", Create(parameter => parameter.Threshold.Values[0].Value = 10), [0]);
        yield return ("threshold-90", Create(parameter => parameter.Threshold.Values[0].Value = 90), [0]);
        yield return ("rim-thickness-0", Create(parameter =>
        {
            parameter.MetaballColor = Blue;
            parameter.RimThickness.Values[0].Value = 0;
        }), [0]);
        yield return ("rim-thickness-30", Create(parameter =>
        {
            parameter.MetaballColor = Blue;
            parameter.RimThickness.Values[0].Value = 30;
        }), [0]);
        yield return ("rim-brightness-0", Create(parameter =>
        {
            parameter.MetaballColor = Blue;
            parameter.RimBrightness.Values[0].Value = 0;
        }), [0]);
        yield return ("rim-brightness-100", Create(parameter =>
        {
            parameter.MetaballColor = Blue;
            parameter.RimBrightness.Values[0].Value = 100;
        }), [0]);
        yield return ("bipolar", Create(parameter => parameter.IsBipolar = true), [0]);
        yield return ("bipolar-frame-4", Create(parameter => parameter.IsBipolar = true), [4]);
        yield return ("color-half-red", Create(parameter => parameter.MetaballColor = Color.FromArgb(128, 255, 0, 0)), [0]);
    }

    public static IEnumerable<(string Name, Func<MetaballSpectrumParameter> Create, Action<MetaballSpectrumParameter> Change, int Frame)> Transitions()
    {
        yield return ("width-600-to-200", () => Create(), parameter => parameter.FieldWidth.Values[0].Value = 200, 0);
        yield return ("height-300-to-100", () => Create(), parameter => parameter.FieldHeight.Values[0].Value = 100, 0);
        yield return ("radius-36-to-120", () => Create(), parameter => parameter.BlobRadius.Values[0].Value = 120, 0);
        yield return ("threshold-45-to-10", () => Create(), parameter => parameter.Threshold.Values[0].Value = 10, 0);
        yield return ("rim-thickness-4-to-30", () => Create(), parameter => parameter.RimThickness.Values[0].Value = 30, 0);
        yield return ("rim-brightness-60-to-0", () => Create(), parameter => parameter.RimBrightness.Values[0].Value = 0, 0);
        yield return ("unipolar-to-bipolar", () => Create(), parameter => parameter.IsBipolar = true, 4);
        yield return ("color-white-to-half-red", () => Create(), parameter => parameter.MetaballColor = Color.FromArgb(128, 255, 0, 0), 0);
    }

    public static IEnumerable<(string Name, MetaballSpectrumParameter Parameter)> Benchmarks()
    {
        yield return ("default", Create());
        yield return ("width-1200", Create(parameter => parameter.FieldWidth.Values[0].Value = 1200));
        yield return ("radius-120", Create(parameter => parameter.BlobRadius.Values[0].Value = 120));
        yield return ("bipolar", Create(parameter => parameter.IsBipolar = true));
    }

    static MetaballSpectrumParameter Create(Action<MetaballSpectrumParameter>? configure = null)
    {
        var parameter = new MetaballSpectrumParameter();
        configure?.Invoke(parameter);
        return parameter;
    }
}
