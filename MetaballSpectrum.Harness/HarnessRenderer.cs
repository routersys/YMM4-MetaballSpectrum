using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.DCommon;
using Vortice.Direct2D1;
using Vortice.DXGI;
using Vortice.Mathematics;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Shape;

namespace MetaballSpectrum.Harness;

internal sealed class HarnessRenderer : IDisposable
{
    public const int Fps = 30;
    public const int Length = 300;
    const int WarmupUpdates = 2000;
    const int UpdateRepeats = 20;
    const float Dpi = 96f;

    static readonly PixelFormat PixelFormat = new(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied);

    readonly GraphicsDevices devices;
    readonly IGraphicsDevicesAndContext context;
    readonly ID2D1Bitmap1 target;
    readonly ID2D1Bitmap1 readback;
    readonly ID2D1Bitmap1 sync;
    readonly GpuTimer timer;

    public HarnessRenderer(int canvasWidth, int canvasHeight)
    {
        CanvasWidth = canvasWidth;
        CanvasHeight = canvasHeight;

        devices = new GraphicsDevices();
        context = devices.CreateContext();
        var deviceContext = context.DeviceContext;
        var adapter = devices.DXGI.Adapter;
        Adapter = adapter.Description.Description;
        Driver = adapter.CheckInterfaceSupport<IDXGIDevice>(out var version) ? FormatDriverVersion(version) : "不明";

        target = CreateBitmap(deviceContext, canvasWidth, canvasHeight, BitmapOptions.Target);
        readback = CreateBitmap(deviceContext, canvasWidth, canvasHeight, BitmapOptions.CpuRead | BitmapOptions.CannotDraw);
        sync = CreateBitmap(deviceContext, 1, 1, BitmapOptions.CpuRead | BitmapOptions.CannotDraw);
        timer = new GpuTimer(devices.D3D.Device, devices.D3D.DeviceContext);
    }

    public int CanvasWidth { get; }

    public int CanvasHeight { get; }

    public string Adapter { get; }

    public string Driver { get; }

    public byte[][] Render(IAudioSpectrumParameter parameter, IReadOnlyList<int> frames)
    {
        if (frames.Count == 0)
            throw new ArgumentException("フレームを 1 つ以上指定してください。", nameof(frames));

        using var source = parameter.CreateShapeSource(context);
        var rendered = new byte[frames.Count][];
        for (var index = 0; index < frames.Count; index++)
        {
            source.Update(Describe(frames[index]), SyntheticSpectrum.For(parameter, frames[index]));
            rendered[index] = Capture(source.Output);
        }

        return rendered;
    }

    public (byte[] Direct, byte[] Transitioned) RenderTransition<TParameter>(Func<TParameter> create, Action<TParameter> change, int frame)
        where TParameter : IAudioSpectrumParameter
    {
        var settled = create();
        change(settled);
        var direct = Render(settled, [frame])[0];

        var live = create();
        using var source = live.CreateShapeSource(context);
        source.Update(Describe(frame), SyntheticSpectrum.For(live, frame));
        _ = Capture(source.Output);
        change(live);
        source.Update(Describe(frame), SyntheticSpectrum.For(live, frame));
        return (direct, Capture(source.Output));
    }

    public Measurement Measure(IAudioSpectrumParameter parameter, int frames, bool moving)
    {
        var descriptions = new TimelineItemSourceDescription[frames];
        var spectra = new float[frames][];
        for (var frame = 0; frame < frames; frame++)
        {
            descriptions[frame] = Describe(moving ? frame : 0);
            spectra[frame] = SyntheticSpectrum.For(parameter, moving ? frame : 0);
        }

        using var source = parameter.CreateShapeSource(context);
        var output = source.Output;
        for (var index = 0; index < WarmupUpdates; index++)
            source.Update(descriptions[index % frames], spectra[index % frames]);
        for (var frame = 0; frame < frames; frame++)
        {
            source.Update(descriptions[frame], spectra[frame]);
            Submit(output);
        }
        Synchronize();

        var cpu = TimeSpan.MaxValue;
        for (var frame = 0; frame < frames; frame++)
        {
            var elapsed = Stopwatch.StartNew();
            source.Update(descriptions[frame], spectra[frame]);
            Submit(output);
            elapsed.Stop();
            cpu = elapsed.Elapsed < cpu ? elapsed.Elapsed : cpu;
        }
        Synchronize();

        TimeSpan? gpu = null;
        for (var frame = 0; frame < frames; frame++)
        {
            source.Update(descriptions[frame], spectra[frame]);
            timer.Begin();
            Submit(output);
            timer.End();
            if (timer.Resolve() is { } resolved && (gpu is null || resolved < gpu))
                gpu = resolved;
        }
        Synchronize();

        var repeats = frames * UpdateRepeats;
        var updates = Stopwatch.StartNew();
        for (var index = 0; index < repeats; index++)
            source.Update(descriptions[index % frames], spectra[index % frames]);
        updates.Stop();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        var collections = GC.CollectionCount(0);
        for (var frame = 0; frame < frames; frame++)
            source.Update(descriptions[frame], spectra[frame]);
        var allocated = (GC.GetAllocatedBytesForCurrentThread() - before) / frames;
        var gen0 = GC.CollectionCount(0) - collections;

        return new Measurement(gpu, cpu, updates.Elapsed / repeats, allocated, gen0);
    }

    byte[] Capture(ID2D1Image output)
    {
        Submit(output);
        readback.CopyFromBitmap(target);

        var stride = CanvasWidth * HarnessImage.BytesPerPixel;
        var pixels = new byte[stride * CanvasHeight];
        var mapped = readback.Map(MapOptions.Read);
        try
        {
            for (var y = 0; y < CanvasHeight; y++)
                Marshal.Copy(mapped.Bits + y * (int)mapped.Pitch, pixels, y * stride, stride);
        }
        finally
        {
            readback.Unmap();
        }

        return pixels;
    }

    void Submit(ID2D1Image output)
    {
        var deviceContext = context.DeviceContext;
        deviceContext.Target = target;
        deviceContext.BeginDraw();
        deviceContext.Clear(new Color4(0f, 0f, 0f, 0f));
        deviceContext.DrawImage(output, new Vector2(CanvasWidth * 0.5f, CanvasHeight * 0.5f), null, InterpolationMode.NearestNeighbor, CompositeMode.SourceOver);
        deviceContext.EndDraw();
        deviceContext.Target = null;
    }

    void Synchronize()
    {
        sync.CopyFromBitmap(Int2.Zero, target, new RectI(0, 0, 1, 1));
        _ = sync.Map(MapOptions.Read);
        sync.Unmap();
    }

    TimelineItemSourceDescription Describe(int frame)
    {
        var timeline = new TimelineSourceDescription(
            new System.Drawing.Size(CanvasWidth, CanvasHeight),
            new YukkuriMovieMaker.Player.Video.FrameTime(frame, Fps),
            new YukkuriMovieMaker.Player.Video.FrameTime(Length, Fps),
            Fps,
            TimelineSourceUsage.Playing,
            Guid.Empty,
            []);
        return new TimelineItemSourceDescription(timeline, frame, Length, 0);
    }

    static ID2D1Bitmap1 CreateBitmap(ID2D1DeviceContext6 deviceContext, int width, int height, BitmapOptions options)
        => deviceContext.CreateBitmap(
            new SizeI(width, height), nint.Zero, width * HarnessImage.BytesPerPixel,
            new BitmapProperties1(PixelFormat, Dpi, Dpi, options));

    static string FormatDriverVersion(long version)
        => $"{(version >> 48) & 0xFFFF}.{(version >> 32) & 0xFFFF}.{(version >> 16) & 0xFFFF}.{version & 0xFFFF}";

    public void Dispose()
    {
        timer.Dispose();
        sync.Dispose();
        readback.Dispose();
        target.Dispose();
        context.Dispose();
        devices.Dispose();
    }

    public readonly record struct Measurement(TimeSpan? Gpu, TimeSpan Cpu, TimeSpan Update, long Allocated, int Gen0);
}
