using System.Reflection;
using YukkuriMovieMaker.Plugin;
using YukkuriMovieMaker.Plugin.Shape;

namespace MetaballSpectrum.Tests;

public sealed class MetaballSpectrumPluginTests
{
    [Fact]
    public void TheNameIsTheLocalizedSpectrumName()
    {
        var plugin = new MetaballSpectrumPlugin();

        Assert.Equal(Texts.MetaballSpectrum, plugin.Name);
    }

    [Fact]
    public void AviUtlOutputIsNotSupported()
    {
        var plugin = new MetaballSpectrumPlugin();

        Assert.False(plugin.IsExoShapeSupported);
        Assert.False(plugin.IsExoMaskSupported);
    }

    [Fact]
    public void ThePluginIsDiscoveredAsAnAudioSpectrum()
    {
        Assert.IsAssignableFrom<IAudioSpectrumPlugin>(new MetaballSpectrumPlugin());
    }

    [Fact]
    public void TheAuthorIsDeclared()
    {
        var details = typeof(MetaballSpectrumPlugin).GetCustomAttribute<PluginDetailsAttribute>();

        Assert.NotNull(details);
        Assert.Equal("routersys", details.AuthorName);
    }

    [Fact]
    public void WithoutSharedDataTheParameterStartsFromItsDefaults()
    {
        var plugin = new MetaballSpectrumPlugin();

        var parameter = Assert.IsType<MetaballSpectrumParameter>(plugin.CreateAudioSpectrumParameter(null));

        Assert.False(parameter.IsBipolar);
    }

    [Fact]
    public void TheSharedDataOfThePreviousParameterIsCarriedOver()
    {
        var plugin = new MetaballSpectrumPlugin();
        var previous = new MetaballSpectrumParameter { IsBipolar = true };
        previous.BlobRadius.Values[0].Value = 7d;

        var parameter = Assert.IsType<MetaballSpectrumParameter>(plugin.CreateAudioSpectrumParameter(previous.GetSharedData()));

        Assert.NotSame(previous, parameter);
        Assert.True(parameter.IsBipolar);
        Assert.Equal(7d, parameter.BlobRadius.GetValue(0, 1, ItemDescriptions.Fps));
    }

    [Fact]
    public void EveryCallCreatesANewParameter()
    {
        var plugin = new MetaballSpectrumPlugin();

        Assert.NotSame(plugin.CreateAudioSpectrumParameter(null), plugin.CreateAudioSpectrumParameter(null));
    }
}
