using MimesisPlayerEnhancement.Features.MimicTuning;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.MimicTuning
{
    public sealed class MimicTuningModeHelpersTests
    {
        [Theory]
        [InlineData("Fixed", "Fixed")]
        [InlineData("Random", "Random")]
        [InlineData("Vanilla", "Vanilla")]
        [InlineData(null, "Vanilla")]
        public void ParseValueMode_maps_values(string? value, string expected)
        {
            Assert.Equal(expected, MimicTuningModeHelpers.ParseValueMode(value).ToString());
        }

        [Theory]
        [InlineData("Random", "Random")]
        [InlineData("Vanilla", "Vanilla")]
        public void ParseIntervalMode_maps_values(string? value, string expected)
        {
            Assert.Equal(expected, MimicTuningModeHelpers.ParseIntervalMode(value).ToString());
        }

        [Theory]
        [InlineData("Vanilla", 50f, 80f, 10f, 20f, 50f)]
        [InlineData("Fixed", 50f, 80f, 10f, 20f, 80f)]
        [InlineData("Random", 50f, 80f, 70f, 70f, 70f)]
        public void ResolveTrustScore_returns_expected_points(
            string modeName,
            float vanilla,
            float fixedValue,
            float randomMin,
            float randomMax,
            float expected)
        {
            MimicTuningValueMode mode = MimicTuningModeHelpers.ParseValueMode(modeName);
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
