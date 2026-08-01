using MimesisPlayerEnhancement.Features.DungeonTime;
using MimesisPlayerEnhancement.Util;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.DungeonTime
{
    public sealed class DungeonTimeResolverTests
    {
        private const long VanillaStartSeconds = 10 * 3600L;
        private const long EndSeconds = 24 * 3600L;
        private const long VanillaRemainingMs = 600_000L;

        private static DungeonTimeSceneConfig Config(
            bool enabled,
            int baseline,
            float extraPerPlayer,
            StartTimePreset startTimePreset = StartTimePreset.Vanilla) =>
            new(enabled, baseline, extraPerPlayer, startTimePreset);

        [Theory]
        [InlineData(1)]
        [InlineData(4)]
        [InlineData(8)]
        public void GetBonusSeconds_returns_zero_when_feature_disabled(int playerCount)
        {
            DungeonTimeSceneConfig config = Config(false, baseline: 4, extraPerPlayer: 10f);

            double bonusSeconds = DungeonTimeResolver.GetBonusSeconds(playerCount, config);

            Assert.Equal(0d, bonusSeconds);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(4)]
        public void GetBonusSeconds_returns_zero_when_player_count_at_or_below_baseline(int playerCount)
        {
            DungeonTimeSceneConfig config = Config(true, baseline: 4, extraPerPlayer: 10f);

            double bonusSeconds = DungeonTimeResolver.GetBonusSeconds(playerCount, config);

            Assert.Equal(0d, bonusSeconds);
        }

        [Theory]
        [InlineData(5, 10d)]
        [InlineData(6, 20d)]
        [InlineData(8, 40d)]
        public void GetBonusSeconds_scales_players_above_baseline(int playerCount, double expectedSeconds)
        {
            DungeonTimeSceneConfig config = Config(true, baseline: 4, extraPerPlayer: 10f);

            double bonusSeconds = DungeonTimeResolver.GetBonusSeconds(playerCount, config);

            Assert.Equal(expectedSeconds, bonusSeconds);
        }

        [Fact]
        public void GetBonusSeconds_supports_fractional_extra_seconds_per_player()
        {
            DungeonTimeSceneConfig config = Config(true, baseline: 1, extraPerPlayer: 2.5f);

            double bonusSeconds = DungeonTimeResolver.GetBonusSeconds(playerCount: 3, config);

            Assert.Equal(5d, bonusSeconds);
        }

        [Fact]
        public void GetBonusSeconds_returns_zero_when_extra_per_player_is_zero()
        {
            DungeonTimeSceneConfig config = Config(true, baseline: 4, extraPerPlayer: 0f);

            double bonusSeconds = DungeonTimeResolver.GetBonusSeconds(playerCount: 8, config);

            Assert.Equal(0d, bonusSeconds);
        }

        [Theory]
        [InlineData(5, 10_000L)]
        [InlineData(6, 20_000L)]
        [InlineData(8, 40_000L)]
        public void GetBonusMilliseconds_matches_seconds_conversion(int playerCount, long expectedMilliseconds)
        {
            DungeonTimeSceneConfig config = Config(true, baseline: 4, extraPerPlayer: 10f);

            long bonusMilliseconds = DungeonTimeResolver.GetBonusMilliseconds(playerCount, config);

            Assert.Equal(expectedMilliseconds, bonusMilliseconds);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(4)]
        [InlineData(8)]
        public void GetBonusMilliseconds_returns_zero_when_feature_disabled(int playerCount)
        {
            DungeonTimeSceneConfig config = Config(false, baseline: 4, extraPerPlayer: 10f);

            long bonusMilliseconds = DungeonTimeResolver.GetBonusMilliseconds(playerCount, config);

            Assert.Equal(0L, bonusMilliseconds);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(4)]
        public void GetBonusMilliseconds_returns_zero_when_player_count_at_or_below_baseline(int playerCount)
        {
            DungeonTimeSceneConfig config = Config(true, baseline: 4, extraPerPlayer: 10f);

            long bonusMilliseconds = DungeonTimeResolver.GetBonusMilliseconds(playerCount, config);

            Assert.Equal(0L, bonusMilliseconds);
        }

        [Fact]
        public void GetBonusMilliseconds_supports_fractional_extra_seconds_per_player()
        {
            DungeonTimeSceneConfig config = Config(true, baseline: 1, extraPerPlayer: 2.5f);

            long bonusMilliseconds = DungeonTimeResolver.GetBonusMilliseconds(playerCount: 3, config);

            Assert.Equal(5_000L, bonusMilliseconds);
        }

        [Theory]
        [InlineData(10 * 3600L, 24 * 3600L, 14 * 3600L)]
        [InlineData(8 * 3600L, 24 * 3600L, 16 * 3600L)]
        [InlineData(12 * 3600L, 24 * 3600L, 12 * 3600L)]
        [InlineData(21 * 3600L, 24 * 3600L, 3 * 3600L)]
        [InlineData(0L, 0L, 86400L)]
        [InlineData(0L, 24 * 3600L, 24 * 3600L)]
        public void GetDisplaySpanSeconds_matches_vanilla_wrap(
            long startSeconds,
            long endSeconds,
            long expectedSpan)
        {
            long span = DungeonTimeResolver.GetDisplaySpanSeconds(startSeconds, endSeconds);

            Assert.Equal(expectedSpan, span);
        }

        [Fact]
        public void GetPresetAdjustedRemainingMs_unchanged_when_start_matches_vanilla()
        {
            long adjusted = DungeonTimeResolver.GetPresetAdjustedRemainingMs(
                VanillaRemainingMs,
                VanillaStartSeconds,
                VanillaStartSeconds,
                EndSeconds);

            Assert.Equal(VanillaRemainingMs, adjusted);
        }

        [Fact]
        public void GetPresetAdjustedRemainingMs_lengthens_for_morning_start()
        {
            // 08:00→24:00 is 16h vs vanilla 10:00→24:00 (14h) → 16/14
            long adjusted = DungeonTimeResolver.GetPresetAdjustedRemainingMs(
                VanillaRemainingMs,
                VanillaStartSeconds,
                effectiveStartSeconds: 8 * 3600L,
                EndSeconds);

            Assert.Equal((long)(VanillaRemainingMs * 16d / 14d), adjusted);
        }

        [Fact]
        public void GetPresetAdjustedRemainingMs_shortens_for_noon_start()
        {
            // 12:00→24:00 is 12h vs 14h → 12/14
            long adjusted = DungeonTimeResolver.GetPresetAdjustedRemainingMs(
                VanillaRemainingMs,
                VanillaStartSeconds,
                effectiveStartSeconds: 12 * 3600L,
                EndSeconds);

            Assert.Equal((long)(VanillaRemainingMs * 12d / 14d), adjusted);
        }

        [Fact]
        public void GetPresetAdjustedRemainingMs_shortens_for_night_start()
        {
            // 21:00→24:00 is 3h vs 14h → 3/14
            long adjusted = DungeonTimeResolver.GetPresetAdjustedRemainingMs(
                VanillaRemainingMs,
                VanillaStartSeconds,
                effectiveStartSeconds: 21 * 3600L,
                EndSeconds);

            Assert.Equal((long)(VanillaRemainingMs * 3d / 14d), adjusted);
        }

        [Fact]
        public void GetPresetAdjustedRemainingMs_lengthens_for_midnight_with_end_wrap()
        {
            // 00:00→00:00 wraps to 24h vs 14h → 24/14
            long adjusted = DungeonTimeResolver.GetPresetAdjustedRemainingMs(
                VanillaRemainingMs,
                VanillaStartSeconds,
                effectiveStartSeconds: 0L,
                endSeconds: 0L);

            Assert.Equal((long)(VanillaRemainingMs * 24d / 14d), adjusted);
        }

        [Fact]
        public void GetPresetAdjustedRemainingMs_stacks_with_player_bonus_as_base_plus_bonus()
        {
            long baseRemaining = DungeonTimeResolver.GetPresetAdjustedRemainingMs(
                VanillaRemainingMs,
                VanillaStartSeconds,
                effectiveStartSeconds: 8 * 3600L,
                EndSeconds);
            DungeonTimeSceneConfig config = Config(true, baseline: 4, extraPerPlayer: 10f);
            long bonusMs = DungeonTimeResolver.GetBonusMilliseconds(playerCount: 6, config);

            long targetRemaining = baseRemaining + bonusMs;

            Assert.Equal(baseRemaining + 20_000L, targetRemaining);
            Assert.True(targetRemaining > VanillaRemainingMs);
        }

        [Theory]
        [InlineData(0, 10_000, 1d)]
        [InlineData(600_000, 0, 1d)]
        [InlineData(600_000, 20_000, 600_000d / 620_000d)]
        [InlineData(100_000, 100_000, 0.5d)]
        public void GetStretchScaleFromBonus_maps_base_remaining_over_extended(
            long baseRemainingMs,
            long bonusMs,
            double expectedScale)
        {
            double scale = DungeonTimeResolver.GetStretchScaleFromBonus(baseRemainingMs, bonusMs);

            Assert.Equal(expectedScale, scale, 10);
        }

        [Theory]
        [InlineData(1d, 1f, 1d)]
        [InlineData(0.5d, 2f, 1d)]
        [InlineData(1d, -1f, -1d)]
        [InlineData(0.5d, -2f, -1d)]
        [InlineData(1d, 0f, 0d)]
        public void GetEffectiveDisplayRate_multiplies_stretch_and_multiplier(
            double stretch,
            float multiplier,
            double expected)
        {
            double rate = DungeonTimeResolver.GetEffectiveDisplayRate(stretch, multiplier);

            Assert.Equal(expected, rate, 10);
        }

        [Theory]
        [InlineData(1000, 1000, 1d, 1000)]
        [InlineData(1000, 1000, 0.5d, 500)]
        [InlineData(1000, 1000, 2d, 2000)]
        [InlineData(1000, 1000, 0d, 0)]
        [InlineData(1000, 1000, -1d, -1000)]
        [InlineData(500, 1000, -1d, -1000)]
        [InlineData(100, 1000, -1d, -1000)]
        public void GetClockBeforeAdd_applies_rate_and_floors_at_min(
            long valueMs,
            long deltaMs,
            double rate,
            long expectedBeforeAdd)
        {
            long before = DungeonTimeResolver.GetClockBeforeAdd(valueMs, deltaMs, rate);

            Assert.Equal(expectedBeforeAdd, before);
            Assert.True(before + deltaMs >= 0);
        }

        [Fact]
        public void GetClockBeforeAdd_respects_custom_floor()
        {
            long before = DungeonTimeResolver.GetClockBeforeAdd(
                valueMs: 5_000,
                deltaMs: 1_000,
                rate: -1d,
                minValueMs: 4_000);

            Assert.Equal(3_000, before);
            Assert.Equal(4_000, before + 1_000);
        }

        [Theory]
        [InlineData(5.5f, 5f)]
        [InlineData(-5.5f, -5f)]
        [InlineData(0f, 0f)]
        [InlineData(1f, 1f)]
        public void ClampTimeMultiplier_clamps_to_range(float value, float expected)
        {
            Assert.Equal(expected, DungeonTimeResolver.ClampTimeMultiplier(value));
        }

        [Theory]
        [InlineData(9.5d, 10 * 3600, 24 * 3600, 10d)]
        [InlineData(10d, 10 * 3600, 24 * 3600, 10d)]
        [InlineData(24d, 10 * 3600, 24 * 3600, 24d)]
        [InlineData(25d, 10 * 3600, 24 * 3600, 24d)]
        [InlineData(-1d, 0, 0, 0d)]
        [InlineData(0d, 0, 0, 0d)]
        [InlineData(24d, 0, 0, 24d)]
        [InlineData(25d, 0, 0, 24d)]
        public void ClampDisplayHours_stops_reverse_at_start_and_forward_at_end(
            double hours,
            long startSeconds,
            long endSeconds,
            double expected)
        {
            double clamped = DungeonTimeResolver.ClampDisplayHours(hours, startSeconds, endSeconds);

            Assert.Equal(expected, clamped, 5);
        }

        [Theory]
        [InlineData(0d, 0f)]
        [InlineData(10.5d, 10.5f)]
        [InlineData(24d, 0f)]
        [InlineData(25d, 1f)]
        public void ToClockFaceHours_maps_end_twenty_four_to_zero(double hours, float expectedFace)
        {
            Assert.Equal(expectedFace, DungeonTimeResolver.ToClockFaceHours(hours), 3);
        }

        [Theory]
        [InlineData(0d, 0, 0, 0, 0)]
        [InlineData(0d, 10 * 3600, 10, 0, 0)]
        [InlineData(3600d, 10 * 3600, 11, 0, 0)]
        public void ToDisplayTimeSpan_uses_zero_for_midnight_start(
            double elapsedGameSeconds,
            long startSeconds,
            int expectedHours,
            int expectedMinutes,
            int expectedDays)
        {
            TimeSpan time = DungeonTimeResolver.ToDisplayTimeSpan(elapsedGameSeconds, startSeconds);

            Assert.Equal(expectedDays, time.Days);
            Assert.Equal(expectedHours, time.Hours);
            Assert.Equal(expectedMinutes, time.Minutes);
        }

        [Theory]
        [InlineData(0d, 10 * 3600, 24 * 3600, false)]
        [InlineData(14 * 3600 - 1, 10 * 3600, 24 * 3600, false)]
        [InlineData(14 * 3600, 10 * 3600, 24 * 3600, true)]
        [InlineData(15 * 3600, 10 * 3600, 24 * 3600, true)]
        [InlineData(0d, 0, 0, false)]
        [InlineData(86400 - 1, 0, 0, false)]
        [InlineData(86400, 0, 0, true)]
        [InlineData(0d, 21 * 3600, 0, false)]
        [InlineData(3 * 3600, 21 * 3600, 0, true)]
        public void HasReachedOrPassedDisplayEnd_uses_elapsed_vs_span(
            double elapsedGameSeconds,
            long startSeconds,
            long endSeconds,
            bool expectedPastEnd)
        {
            bool past = DungeonTimeResolver.HasReachedOrPassedDisplayEnd(
                elapsedGameSeconds,
                startSeconds,
                endSeconds);

            Assert.Equal(expectedPastEnd, past);
        }
    }
}
