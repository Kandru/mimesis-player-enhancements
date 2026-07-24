using MimesisPlayerEnhancement.Features.MoreVoices;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.MoreVoices
{
    public sealed class SpeechEventArchiveLimitsTests
    {
        private static SpeechEventArchiveLimits.MoreVoicesLimitsConfig Config(
            bool enabled = true,
            bool unify = false,
            int indoor = 100,
            int outdoor = 50,
            int deathMatch = 25) =>
            new(enabled, unify, indoor, outdoor, deathMatch);

        [Fact]
        public void Resolve_returns_null_when_feature_disabled()
        {
            SpeechEventArchiveLimits.PoolLimits? limits = SpeechEventArchiveLimits.Resolve(
                Config(enabled: false));

            Assert.Null(limits);
        }

        [Fact]
        public void Resolve_split_mode_maps_indoor_outdoor_and_deathmatch_caps()
        {
            SpeechEventArchiveLimits.PoolLimits limits = SpeechEventArchiveLimits.Resolve(
                Config(unify: false, indoor: 100, outdoor: 50, deathMatch: 25))!.Value;

            Assert.Equal(175, limits.MaxEvents);
            Assert.Equal(25, limits.MaxDeathMatch);
            Assert.Equal(50, limits.MaxOutdoor);
            Assert.Equal(100, limits.IndoorCap);
        }

        [Fact]
        public void Resolve_unify_mode_merges_indoor_and_outdoor_into_shared_bucket()
        {
            SpeechEventArchiveLimits.PoolLimits limits = SpeechEventArchiveLimits.Resolve(
                Config(unify: true, indoor: 100, outdoor: 50, deathMatch: 25))!.Value;

            Assert.Equal(175, limits.MaxEvents);
            Assert.Equal(25, limits.MaxDeathMatch);
            Assert.Equal(150, limits.MaxOutdoor);
            Assert.Equal(0, limits.IndoorCap);
        }

        [Fact]
        public void ToEffectiveCaps_maps_pool_limits_to_per_bucket_caps()
        {
            var limits = new SpeechEventArchiveLimits.PoolLimits(maxEvents: 175, maxDeathMatch: 25, maxOutdoor: 50);
            SpeechEventArchiveLimits.EffectiveCaps caps = SpeechEventArchiveLimits.ToEffectiveCaps(limits);

            Assert.Equal(100, caps.Indoor);
            Assert.Equal(25, caps.DeathMatch);
            Assert.Equal(50, caps.Outdoor);
        }

        [Fact]
        public void AnyDecreasedComparedTo_detects_any_lower_cap()
        {
            var before = new SpeechEventArchiveLimits.EffectiveCaps(indoor: 100, deathMatch: 25, outdoor: 50);
            var lowerIndoor = new SpeechEventArchiveLimits.EffectiveCaps(indoor: 90, deathMatch: 25, outdoor: 50);
            var lowerDeathMatch = new SpeechEventArchiveLimits.EffectiveCaps(indoor: 100, deathMatch: 20, outdoor: 50);
            var lowerOutdoor = new SpeechEventArchiveLimits.EffectiveCaps(indoor: 100, deathMatch: 25, outdoor: 40);
            var unchanged = new SpeechEventArchiveLimits.EffectiveCaps(indoor: 100, deathMatch: 25, outdoor: 50);
            var increased = new SpeechEventArchiveLimits.EffectiveCaps(indoor: 110, deathMatch: 25, outdoor: 50);

            Assert.True(lowerIndoor.AnyDecreasedComparedTo(before));
            Assert.True(lowerDeathMatch.AnyDecreasedComparedTo(before));
            Assert.True(lowerOutdoor.AnyDecreasedComparedTo(before));
            Assert.False(unchanged.AnyDecreasedComparedTo(before));
            Assert.False(increased.AnyDecreasedComparedTo(before));
        }

        [Fact]
        public void ShouldRetrimAfterCapChange_true_only_when_any_cap_decreases()
        {
            var before = new SpeechEventArchiveLimits.EffectiveCaps(indoor: 100, deathMatch: 25, outdoor: 50);
            var decreased = new SpeechEventArchiveLimits.EffectiveCaps(indoor: 90, deathMatch: 25, outdoor: 50);
            var increased = new SpeechEventArchiveLimits.EffectiveCaps(indoor: 110, deathMatch: 25, outdoor: 50);
            var unchanged = new SpeechEventArchiveLimits.EffectiveCaps(indoor: 100, deathMatch: 25, outdoor: 50);

            Assert.True(SpeechEventArchiveLimits.ShouldRetrimAfterCapChange(before, decreased));
            Assert.False(SpeechEventArchiveLimits.ShouldRetrimAfterCapChange(before, increased));
            Assert.False(SpeechEventArchiveLimits.ShouldRetrimAfterCapChange(before, unchanged));
        }

        [Fact]
        public void FormatEffectiveCaps_split_mode_lists_each_bucket()
        {
            var caps = new SpeechEventArchiveLimits.EffectiveCaps(indoor: 100, deathMatch: 25, outdoor: 50);

            string formatted = SpeechEventArchiveLimits.FormatEffectiveCaps(caps, unifyIndoorOutdoor: false);

            Assert.Equal("indoor=100, deathmatch=25, outdoor=50", formatted);
        }

        [Fact]
        public void FormatEffectiveCaps_unify_mode_reports_shared_pool()
        {
            var caps = new SpeechEventArchiveLimits.EffectiveCaps(indoor: 0, deathMatch: 25, outdoor: 150);

            string formatted = SpeechEventArchiveLimits.FormatEffectiveCaps(caps, unifyIndoorOutdoor: true);

            Assert.Equal("shared=150, deathmatch=25", formatted);
        }

        [Fact]
        public void vanilla_defaults_match_game_serialize_field_defaults()
        {
            Assert.Equal(128, SpeechEventArchiveLimits.VanillaMaxEvents);
            Assert.Equal(20, SpeechEventArchiveLimits.VanillaMaxDeathMatchEvents);
            Assert.Equal(30, SpeechEventArchiveLimits.VanillaMaxOutDoorEvents);
        }
    }
}
