using System.Runtime.InteropServices;
using System.Security.Cryptography;
using YukkuriMovieMaker.Plugin.Shape;

namespace MetaballSpectrum.Harness;

internal static class SyntheticSpectrum
{
    public const int Bands = 32;
    const double BandStep = 0.11;
    const double FrameStep = 0.07;
    const int KeyLength = 16;

    public static float[] For(IAudioSpectrumParameter parameter, int frame)
        => parameter is MetaballSpectrumParameter { IsBipolar: true } ? Waveform(frame) : At(frame);

    public static float[] At(int frame)
    {
        var bands = new float[Bands];
        for (var band = 0; band < Bands; band++)
        {
            var phase = 2.0 * Math.PI * (band * BandStep + frame * FrameStep);
            bands[band] = (float)((0.5 + 0.5 * Math.Sin(phase)) * (1.0 - band / (double)Bands));
        }

        return bands;
    }

    public static float[] Waveform(int frame)
    {
        var samples = new float[Bands];
        for (var sample = 0; sample < Bands; sample++)
        {
            var phase = 2.0 * Math.PI * (sample * BandStep + frame * FrameStep);
            samples[sample] = (float)(Math.Sin(phase) * (1.0 - sample / (double)Bands));
        }

        return samples;
    }

    public static string Identity(int frames)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        for (var frame = 0; frame < frames; frame++)
        {
            hash.AppendData(MemoryMarshal.AsBytes(At(frame).AsSpan()));
            hash.AppendData(MemoryMarshal.AsBytes(Waveform(frame).AsSpan()));
        }

        return $"synthetic {Bands} bands {frames} frames {Convert.ToHexString(hash.GetHashAndReset())[..KeyLength]}";
    }
}
