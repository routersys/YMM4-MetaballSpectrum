using System.Windows;
using Telemetry;

namespace MetaballSpectrum.Tests;

public sealed class HostIntegrationTests
{
    [Fact]
    public void OutsideAWpfApplicationNoTelemetryIsStartedOrSent()
    {
        Assert.Null(Application.Current);

        MetaballSpectrumTelemetry.EnsureStartedOnce();
        MetaballSpectrumTelemetry.Report(new InvalidOperationException());

        Assert.Null(ProcessState.Read("DrainClaimed"));
        Assert.Null(ProcessState.Read("SentCount"));
    }

    [Fact]
    public void OutsideAWpfApplicationTheParameterCanStillBeCreated()
    {
        Assert.Null(Application.Current);

        var parameter = new MetaballSpectrumPlugin().CreateAudioSpectrumParameter(null);

        Assert.IsType<MetaballSpectrumParameter>(parameter);
        Assert.Null(ProcessState.Read("DrainClaimed"));
    }
}
