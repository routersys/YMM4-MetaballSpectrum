using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MetaballSpectrum.Harness;

internal static class HarnessImage
{
    public const int BytesPerPixel = 4;

    public static (int Width, int Height, byte[] Pixels) LoadPremultiplied(string path)
    {
        using var stream = new MemoryStream(File.ReadAllBytes(path));
        BitmapDecoder decoder;
        try
        {
            decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        }
        catch (Exception exception) when (exception is NotSupportedException or FileFormatException)
        {
            throw new HarnessException($"画像として読めません。{path}");
        }

        var converted = new FormatConvertedBitmap(decoder.Frames[0], PixelFormats.Pbgra32, null, 0d);
        var pixels = new byte[converted.PixelWidth * converted.PixelHeight * BytesPerPixel];
        converted.CopyPixels(pixels, converted.PixelWidth * BytesPerPixel, 0);
        return (converted.PixelWidth, converted.PixelHeight, pixels);
    }

    public static void Save(string path, byte[] pixels, int width, int height)
    {
        var source = BitmapSource.Create(width, height, 96, 96, PixelFormats.Pbgra32, null, pixels, width * BytesPerPixel);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }
}
