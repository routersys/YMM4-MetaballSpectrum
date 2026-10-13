namespace MetaballSpectrum;

internal static class SpectrumResampler
{
    public static int Resample(float[]? source, Span<float> destination)
    {
        if (source is null || source.Length == 0 || destination.Length == 0)
        {
            destination.Clear();
            return 0;
        }

        var count = Math.Min(source.Length, destination.Length);
        for (var i = 0; i < count; i++)
        {
            var begin = (int)((long)i * source.Length / count);
            var end = (int)((long)(i + 1) * source.Length / count);

            var extremum = 0f;
            var magnitude = 0f;
            for (var j = begin; j < end; j++)
            {
                var value = source[j];
                if (!float.IsFinite(value))
                    continue;

                var scale = Math.Abs(value);
                if (scale > magnitude)
                {
                    magnitude = scale;
                    extremum = value;
                }
            }

            destination[i] = Math.Clamp(extremum, -1f, 1f);
        }

        destination[count..].Clear();
        return count;
    }
}
