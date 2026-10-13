namespace MetaballSpectrum.Harness;

internal static class HarnessCases
{
    public static IEnumerable<(string Name, MetaballSpectrumParameter Parameter, IReadOnlyList<int> Frames)> All()
    {
        yield break;
    }

    public static IEnumerable<(string Name, Func<MetaballSpectrumParameter> Create, Action<MetaballSpectrumParameter> Change, int Frame)> Transitions()
    {
        yield break;
    }

    public static IEnumerable<(string Name, MetaballSpectrumParameter Parameter)> Benchmarks()
    {
        yield break;
    }
}
