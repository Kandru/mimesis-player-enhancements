using MimesisPlayerEnhancement.Features.MimicTuning;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.MimicTuning
{
    public sealed class MimicTuningModeHelpersTests
    {
        [Theory]
        [InlineData("Fixed", MimicTuningValueMode.Fixed)]
        [InlineData("Random", MimicTuningValueMode.Random)]
        [InlineData("Vanilla", MimicTuningValueMode.Vanilla)]
        [InlineData(null, MimicTuningValueMode.Vanilla)]
        public void ParseValueMode_maps_values(string? value, MimicTuningValueMode expected)
        {
            Assert.Equal(expected, MimicTuningModeHelpers.ParseValueMode(value));
        }

        [Theory]
        [InlineData("Random", MimicTuningIntervalMode.Random)]
        [InlineData("Vanilla", MimicTuningIntervalMode.Vanilla)]
        public void ParseIntervalMode_maps_values(string? value, MimicTuningIntervalMode expected)
        {
            Assert.Equal(expected, MimicTuningModeHelpers.ParseIntervalMode(value));
        }

        [Theory]
        [InlineData(MimicTuningValueMode.Vanilla, 50f, 80f, 10f, 20f, 50f)]
        [InlineData(MimicTuningValueMode.Fixed, 50f, 80f, 10f, 20f, 80f)]
        [InlineData(MimicTuningValueMode.Random, 50f, 80f, 70f, 70f, 70f)]
        public void ResolveTrustScore_returns_expected_points(
            MimicTuningValueMode mode,
            float vanilla,
            float fixedValue,
            float randomMin,
            float randomMax,
            float expected)
        {
            float result = MimicTuningModeHelpers.ResolveTrustScore(
                mode,
                vanilla,
                fixedValue,
                randomMin,
                randomMax);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(80, 0.8f)]
        [InlineData(0, 0f)]
        [InlineData(100, 1f)]
        public void PercentToProbability_converts_percent(int percent, float expected)
        {
            Assert.Equal(expected, MimicTuningModeHelpers.PercentToProbability(percent));
        }
    }
}
