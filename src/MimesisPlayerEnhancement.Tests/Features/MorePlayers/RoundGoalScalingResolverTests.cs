using MimesisPlayerEnhancement.Features.MorePlayers;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.MorePlayers
{
    public sealed class RoundGoalScalingResolverTests
    {
        private const float DefaultBase = RoundGoalScalingResolver.DefaultBasePerZone;
        private const float DefaultExponent = RoundGoalScalingResolver.DefaultCurveExponent;
        private const int DefaultSpread = RoundGoalScalingResolver.DefaultRandomSpreadPercent;

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ComputeCenter_returns_zero_for_non_positive_stage_count(int stageCount)
        {
            int center = RoundGoalScalingResolver.ComputeCenter(stageCount, DefaultBase, DefaultExponent, 1f);

            Assert.Equal(0, center);
        }

        [Theory]
        [InlineData(1, 200)]
        [InlineData(2, 373)]
        [InlineData(3, 538)]
        public void ComputeCenter_scales_with_defaults(int stageCount, int expected)
        {
            int center = RoundGoalScalingResolver.ComputeCenter(stageCount, DefaultBase, DefaultExponent, 1f);

            Assert.Equal(expected, center);
        }

        [Fact]
        public void ComputeCenter_returns_zero_when_money_multiplier_is_zero()
        {
            int center = RoundGoalScalingResolver.ComputeCenter(3, DefaultBase, DefaultExponent, 0f);

            Assert.Equal(0, center);
        }

        [Fact]
        public void ComputeCenter_doubles_with_money_multiplier_two()
        {
            int center = RoundGoalScalingResolver.ComputeCenter(1, DefaultBase, DefaultExponent, 2f);

            Assert.Equal(400, center);
        }

        [Fact]
        public void ComputeCenter_respects_higher_curve_exponent()
        {
            int lowExponent = RoundGoalScalingResolver.ComputeCenter(3, DefaultBase, 0.5f, 1f);
            int highExponent = RoundGoalScalingResolver.ComputeCenter(3, DefaultBase, 1.5f, 1f);

            Assert.True(highExponent > lowExponent);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-2)]
        public void ComputeMin_returns_zero_for_non_positive_stage_count(int stageCount)
        {
            int min = RoundGoalScalingResolver.ComputeMin(stageCount, DefaultBase, DefaultExponent, 1f, DefaultSpread);

            Assert.Equal(0, min);
        }

        [Fact]
        public void ComputeMin_applies_spread_and_never_drops_below_one()
        {
            int center = RoundGoalScalingResolver.ComputeCenter(1, DefaultBase, DefaultExponent, 1f);
            int min = RoundGoalScalingResolver.ComputeMin(1, DefaultBase, DefaultExponent, 1f, DefaultSpread);

            Assert.Equal(Math.Max(1, (int)Math.Round(center * 0.9f)), min);
        }

        [Fact]
        public void ComputeMax_is_at_least_compute_min()
        {
            int min = RoundGoalScalingResolver.ComputeMin(2, DefaultBase, DefaultExponent, 1f, DefaultSpread);
            int max = RoundGoalScalingResolver.ComputeMax(2, DefaultBase, DefaultExponent, 1f, DefaultSpread);

            Assert.True(max >= min);
        }

        [Fact]
        public void ComputeMax_applies_positive_spread_from_center()
        {
            int center = RoundGoalScalingResolver.ComputeCenter(1, DefaultBase, DefaultExponent, 1f);
            int max = RoundGoalScalingResolver.ComputeMax(1, DefaultBase, DefaultExponent, 1f, DefaultSpread);

            Assert.Equal((int)Math.Round(center * 1.1f), max);
        }
    }
}
