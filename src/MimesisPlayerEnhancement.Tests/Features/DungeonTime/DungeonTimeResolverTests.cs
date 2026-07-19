using MimesisPlayerEnhancement.Features.DungeonTime;
using MimesisPlayerEnhancement.Util;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.DungeonTime
{
    public sealed class DungeonTimeResolverTests
    {
        private static DungeonTimeSceneConfig Config(bool enabled, int baseline, float extraPerPlayer) =>
            new(enabled, baseline, extraPerPlayer);

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
        [InlineData(4, 0d)]
        [InlineData(5, 10d)]
        [InlineData(6, 20d)]
        [InlineData(8, 40d)]
        public void GetBonusSeconds_uses_default_like_scaling(int playerCount, double expectedSeconds)
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

        [Theory]
        [InlineData(5, 10d, 10_000L)]
        [InlineData(6, 20d, 20_000L)]
        public void GetBonusSeconds_converts_to_milliseconds_like_applier(
            int playerCount,
            double expectedSeconds,
            long expectedMilliseconds)
        {
            DungeonTimeSceneConfig config = Config(true, baseline: 4, extraPerPlayer: 10f);

            double bonusSeconds = DungeonTimeResolver.GetBonusSeconds(playerCount, config);
            long bonusMilliseconds = (long)(bonusSeconds * 1000d);

            Assert.Equal(expectedSeconds, bonusSeconds);
            Assert.Equal(expectedMilliseconds, bonusMilliseconds);
        }

        [Fact]
        public void GetBonusMilliseconds_matches_seconds_conversion_for_positive_bonus()
        {
            DungeonTimeSceneConfig config = Config(true, baseline: 1, extraPerPlayer: 2.5f);
            double bonusSeconds = DungeonTimeResolver.GetBonusSeconds(3, config);

            long expectedMilliseconds = (long)(bonusSeconds * 1000d);

            Assert.Equal(5d, bonusSeconds);
            Assert.Equal(5_000L, expectedMilliseconds);
        }
    }
}
