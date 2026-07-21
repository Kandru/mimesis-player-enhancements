using System.Globalization;
using MimesisPlayerEnhancement;
using MimesisPlayerEnhancement.Features.MimicTuning.MimicPossession;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.MimicTuning
{
    public sealed class MimicTuningConfigBoundsTests
    {
        private const string SectionId = "MimesisPlayerEnhancement_MimicTuning";

        [Theory]
        [InlineData("MimicPossessionMinTimeSeconds")]
        [InlineData("MimicPossessionMaxTimeSeconds")]
        public void Possession_duration_seconds_use_resolver_bounds(string key)
        {
            Assert.True(ModConfigEntryBounds.TryGet(SectionId, key, out ModConfigEntryBound bound));
            Assert.Equal(
                MimicPossessionResolver.MinDurationSeconds,
                float.Parse(bound.MinValue!, CultureInfo.InvariantCulture));
            Assert.Equal(
                MimicPossessionResolver.MaxDurationSeconds,
                float.Parse(bound.MaxValue!, CultureInfo.InvariantCulture));
        }

        [Fact]
        public void MimicPossessionCooltimeMultiplier_uses_resolver_bounds()
        {
            Assert.True(ModConfigEntryBounds.TryGet(
                SectionId,
                "MimicPossessionCooltimeMultiplier",
                out ModConfigEntryBound bound));
            Assert.Equal(
                MimicPossessionResolver.MinCooltimeMultiplier,
                float.Parse(bound.MinValue!, CultureInfo.InvariantCulture));
            Assert.Equal(
                MimicPossessionResolver.MaxCooltimeMultiplier,
                float.Parse(bound.MaxValue!, CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData("PeriodicVoiceIntervalMultiplier")]
        [InlineData("PlayerVoiceResponseCooldownSeconds")]
        [InlineData("PlayerVoiceResponseDelayMinSeconds")]
        [InlineData("PlayerVoiceResponseDelayMaxSeconds")]
        public void Voice_tuning_floats_have_minimum_zero(string key)
        {
            Assert.True(ModConfigEntryBounds.TryGet(SectionId, key, out ModConfigEntryBound bound));
            Assert.Equal(0f, float.Parse(bound.MinValue!, CultureInfo.InvariantCulture));
            Assert.Null(bound.MaxValue);
        }

        [Fact]
        public void PlayerVoiceResponseMaxDistance_has_minimum_one()
        {
            Assert.True(ModConfigEntryBounds.TryGet(
                SectionId,
                "PlayerVoiceResponseMaxDistance",
                out ModConfigEntryBound bound));
            Assert.Equal(1f, float.Parse(bound.MinValue!, CultureInfo.InvariantCulture));
            Assert.Null(bound.MaxValue);
        }

        [Fact]
        public void PlayerVoiceResponseChancePercent_is_clamped_to_0_through_100()
        {
            Assert.True(ModConfigEntryBounds.TryGet(
                SectionId,
                "PlayerVoiceResponseChancePercent",
                out ModConfigEntryBound bound));
            Assert.Equal("0", bound.MinValue);
            Assert.Equal("100", bound.MaxValue);
        }
    }
}
