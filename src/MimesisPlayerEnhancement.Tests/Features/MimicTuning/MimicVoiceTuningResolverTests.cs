using MimesisPlayerEnhancement.Features.MimicTuning.MimicVoiceTuning;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.MimicTuning
{
    public sealed class MimicVoiceTuningResolverTests
    {
        [Theory]
        [InlineData("Vanilla", "Vanilla")]
        [InlineData("vanilla", "Vanilla")]
        [InlineData("Custom", "Custom")]
        [InlineData("custom", "Custom")]
        [InlineData(null, "Vanilla")]
        [InlineData("invalid", "Vanilla")]
        public void ParseMode_maps_values(string? value, string expectedName)
        {
            Assert.Equal(expectedName, MimicVoiceTuningResolver.ParseMode(value).ToString());
        }

        [Theory]
        [InlineData(10f, false, 0.5f, 10f)]
        [InlineData(10f, true, 2f, 20f)]
        [InlineData(0f, true, 2f, 0f)]
        [InlineData(-1f, true, 2f, -1f)]
        public void ScaleIntervalSeconds_applies_multiplier_only_when_custom(
            float vanilla,
            bool shouldApplyCustom,
            float multiplier,
            float expected)
        {
            float result = MimicVoiceTuningResolver.ScaleIntervalSeconds(
                vanilla,
                shouldApplyCustom,
                multiplier);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(10f, false, 10f)]
        [InlineData(10f, true, 20f)]
        public void ScalePeriodicIntervalSeconds_matches_scale_helper(
            float vanilla,
            bool shouldApplyCustom,
            float expected)
        {
            float result = MimicVoiceTuningResolver.ScaleIntervalSeconds(
                vanilla,
                shouldApplyCustom,
                2f);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetClipReuseCooldownSeconds_returns_vanilla_when_not_custom()
        {
            Assert.Equal(60f, MimicVoiceTuningResolver.GetClipReuseCooldownSeconds());
        }

        [Theory]
        [InlineData(false, 50f, 20f)]
        [InlineData(true, 50f, 50f)]
        public void GetResponseMaxDistance_returns_configured_or_vanilla(
            bool shouldApplyCustom,
            float configuredDistance,
            float expected)
        {
            float result = MimicVoiceTuningResolver.GetResponseMaxDistance(
                shouldApplyCustom,
                configuredDistance);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(false, 1f, 5f, 0.2f)]
        [InlineData(true, 2f, 2f, 2f)]
        [InlineData(true, 3f, 1f, 3f)]
        public void RollResponseDelaySeconds_is_deterministic_when_not_custom_or_min_equals_max(
            bool shouldApplyCustom,
            float minSeconds,
            float maxSeconds,
            float expected)
        {
            float result = MimicVoiceTuningResolver.RollResponseDelaySeconds(
                shouldApplyCustom,
                minSeconds,
                maxSeconds);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(false, 0, true)]
        [InlineData(false, 50, true)]
        [InlineData(true, 0, false)]
        [InlineData(true, 100, true)]
        public void RollResponseChance_is_deterministic_at_extremes(
            bool shouldApplyCustom,
            int chancePercent,
            bool expected)
        {
            bool result = MimicVoiceTuningResolver.RollResponseChance(shouldApplyCustom, chancePercent);

            Assert.Equal(expected, result);
        }
    }
}
