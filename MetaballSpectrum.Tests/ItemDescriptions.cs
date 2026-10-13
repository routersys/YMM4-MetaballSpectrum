using System.Drawing;
using YukkuriMovieMaker.Player.Video;

namespace MetaballSpectrum.Tests;

internal static class ItemDescriptions
{
    public const int Fps = 30;

    static readonly Size ScreenSize = new(1920, 1080);

    public static TimelineItemSourceDescription At(int frame, int length)
    {
        var timeline = new TimelineSourceDescription(
            ScreenSize,
            new FrameTime(frame, Fps),
            new FrameTime(length, Fps),
            Fps,
            TimelineSourceUsage.Playing,
            Guid.Empty,
            []);
        return new TimelineItemSourceDescription(timeline, frame, length, 0);
    }
}
