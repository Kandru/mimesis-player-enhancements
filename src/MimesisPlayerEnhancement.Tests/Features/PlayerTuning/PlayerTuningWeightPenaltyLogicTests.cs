using MimesisPlayerEnhancement.Features.PlayerTuning;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.PlayerTuning
{
    public sealed class PlayerTuningWeightPenaltyLogicTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void ComputeRate_returns_zero_when_total_weight_not_positive(int totalWeight)
        {
            int rate = PlayerTuningWeightPenaltyLogic.ComputeRate(
                totalWeight,
                effectiveMaxCarryWeight: 100,
                minThresholdMoveSpeedRate: 0);

            Assert.Equal(0, rate);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ComputeRate_returns_zero_when_effective_max_not_positive(int effectiveMax)
        {
            int rate = PlayerTuningWeightPenaltyLogic.ComputeRate(
                totalWeight: 50,
                effectiveMax,
                minThresholdMoveSpeedRate: 0);

            Assert.Equal(0, rate);
        }

        [Fact]
        public void ComputeRate_at_max_weight_returns_threshold_capped_rate()
        {
            int rate = PlayerTuningWeightPenaltyLogic.ComputeRate(
                totalWeight: 100,
                effectiveMaxCarryWeight: 100,
                minThresholdMoveSpeedRate: 0);

            Assert.Equal(10_000, rate);
        }

        [Fact]
        public void ComputeRate_above_max_weight_clamps_to_full_encumbrance()
        {
            int atMax = PlayerTuningWeightPenaltyLogic.ComputeRate(100, 100, minThresholdMoveSpeedRate: 0);
            int aboveMax = PlayerTuningWeightPenaltyLogic.ComputeRate(200, 100, minThresholdMoveSpeedRate: 0);

            Assert.Equal(atMax, aboveMax);
        }

        [Fact]
        public void ComputeRate_scales_cubically_at_half_encumbrance()
        {
            int halfRate = PlayerTuningWeightPenaltyLogic.ComputeRate(50, 100, minThresholdMoveSpeedRate: 0);
            int maxRate = PlayerTuningWeightPenaltyLogic.ComputeRate(100, 100, minThresholdMoveSpeedRate: 0);

            Assert.Equal(1_250, halfRate);
            Assert.Equal(maxRate / 8, halfRate);
        }

        [Fact]
        public void ComputeRate_applies_min_threshold_move_speed_rate_cap()
        {
            int rate = PlayerTuningWeightPenaltyLogic.ComputeRate(
                totalWeight: 100,
                effectiveMaxCarryWeight: 100,
                minThresholdMoveSpeedRate: 2_500);

            Assert.Equal(7_500, rate);
        }
    }
}
