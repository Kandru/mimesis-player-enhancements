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
        public void Bonus_milliseconds_match_applier_conversion(int playerCount, long expectedMilliseconds)
        {
            DungeonTimeSceneConfig config = Config(true, baseline: 4, extraPerPlayer: 10f);

            double bonusSeconds = DungeonTimeResolver.GetBonusSeconds(playerCount, config);
            long bonusMilliseconds = (long)(bonusSeconds * 1000d);

            Assert.Equal(expectedMilliseconds, bonusMilliseconds);
        }
    }
}
