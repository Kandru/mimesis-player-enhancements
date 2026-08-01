using MimesisPlayerEnhancement.Util;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Util
{
    public sealed class ScalingMathTests
    {
        [Fact]
        public void GetAdditiveMultiplier_adds_per_player_above_baseline()
        {
            float result = ScalingMath.GetAdditiveMultiplier(
                generalMultiplier: 1f,
                perPlayerMultiplier: 0.1f,
                playerCount: 5,
                baselinePlayerCount: 4);

            Assert.Equal(1.1f, result);
        }

        [Theory]
        [InlineData(4)]
        [InlineData(3)]
        [InlineData(1)]
        public void GetAdditiveMultiplier_returns_general_when_players_at_or_below_baseline(int playerCount)
        {
            float result = ScalingMath.GetAdditiveMultiplier(
                generalMultiplier: 1.5f,
                perPlayerMultiplier: 0.1f,
                playerCount: playerCount,
                baselinePlayerCount: 4);

            Assert.Equal(1.5f, result);
        }

        [Fact]
        public void GetAdditiveMultiplier_returns_general_when_per_player_is_zero()
        {
            float result = ScalingMath.GetAdditiveMultiplier(
                generalMultiplier: 2f,
                perPlayerMultiplier: 0f,
                playerCount: 8,
                baselinePlayerCount: 4);

            Assert.Equal(2f, result);
        }

        [Fact]
        public void GetAdditiveMultiplier_uses_custom_baseline()
        {
            float result = ScalingMath.GetAdditiveMultiplier(
                generalMultiplier: 2f,
                perPlayerMultiplier: 0.25f,
                playerCount: 8,
                baselinePlayerCount: 6);

            Assert.Equal(2.5f, result);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-3)]
        public void GetAdditiveMultiplier_treats_non_positive_baseline_as_one(int baseline)
        {
            float result = ScalingMath.GetAdditiveMultiplier(
                generalMultiplier: 2f,
                perPlayerMultiplier: 0.25f,
                playerCount: 3,
                baselinePlayerCount: baseline);

            Assert.Equal(2.5f, result);
        }

        [Fact]
        public void GetAdditiveMultiplier_clamps_negative_result_to_zero()
        {
            float result = ScalingMath.GetAdditiveMultiplier(
                generalMultiplier: -2f,
                perPlayerMultiplier: 0.1f,
                playerCount: 8,
                baselinePlayerCount: 4);

            Assert.Equal(0f, result);
        }
    }
}
