namespace MetaballSpectrum.Tests;

public sealed class SpectrumResamplerTests
{
    static float[] Filled(int length) => Enumerable.Repeat(9f, length).ToArray();

    [Fact]
    public void WithoutASourceTheDestinationIsClearedAndNothingIsAvailable()
    {
        var destination = Filled(3);

        var available = SpectrumResampler.Resample(null, destination);

        Assert.Equal(0, available);
        Assert.Equal([0f, 0f, 0f], destination);
    }

    [Fact]
    public void AnEmptySourceClearsTheDestination()
    {
        var destination = Filled(3);

        var available = SpectrumResampler.Resample([], destination);

        Assert.Equal(0, available);
        Assert.Equal([0f, 0f, 0f], destination);
    }

    [Fact]
    public void AnEmptyDestinationReceivesNothing()
    {
        var available = SpectrumResampler.Resample([0.5f, 0.25f], Span<float>.Empty);

        Assert.Equal(0, available);
    }

    [Fact]
    public void ASourceOfTheSameLengthIsCopied()
    {
        var destination = new float[3];

        var available = SpectrumResampler.Resample([0.25f, -0.5f, 1f], destination);

        Assert.Equal(3, available);
        Assert.Equal([0.25f, -0.5f, 1f], destination);
    }

    [Fact]
    public void AShorterSourceFillsTheFrontAndClearsTheRest()
    {
        var destination = Filled(4);

        var available = SpectrumResampler.Resample([0.3f, -0.7f], destination);

        Assert.Equal(2, available);
        Assert.Equal([0.3f, -0.7f, 0f, 0f], destination);
    }

    [Fact]
    public void ALongerSourceKeepsTheStrongestValueOfEachBlockWithItsSign()
    {
        var destination = new float[2];

        var available = SpectrumResampler.Resample([0.1f, -0.9f, 0.5f, 0.2f], destination);

        Assert.Equal(2, available);
        Assert.Equal([-0.9f, 0.5f], destination);
    }

    [Fact]
    public void UnevenBlocksGiveTheRemainderToTheLastBlocks()
    {
        var destination = new float[2];

        SpectrumResampler.Resample([0.1f, 0.2f, 0.3f, 0.4f, 0.5f], destination);

        Assert.Equal([0.2f, 0.5f], destination);
    }

    [Fact]
    public void TheEarlierOfTwoEqualMagnitudesWins()
    {
        var destination = new float[1];

        SpectrumResampler.Resample([0.5f, -0.5f], destination);

        Assert.Equal([0.5f], destination);
    }

    [Theory]
    [InlineData(float.NaN, 0.3f, 0.3f)]
    [InlineData(float.PositiveInfinity, -0.2f, -0.2f)]
    [InlineData(float.NegativeInfinity, 0f, 0f)]
    [InlineData(float.NaN, float.NaN, 0f)]
    public void ValuesThatAreNotFiniteAreSkipped(float first, float second, float expected)
    {
        var destination = Filled(1);

        SpectrumResampler.Resample([first, second], destination);

        Assert.Equal([expected], destination);
    }

    [Fact]
    public void EveryValueIsClampedToTheUnitRange()
    {
        var destination = new float[2];

        SpectrumResampler.Resample([2f, -3f], destination);

        Assert.Equal([1f, -1f], destination);
    }

    [Fact]
    public void TheClampAppliesAfterTheStrongestValueIsChosen()
    {
        var destination = new float[1];

        SpectrumResampler.Resample([2f, -3f], destination);

        Assert.Equal([-1f], destination);
    }
}
