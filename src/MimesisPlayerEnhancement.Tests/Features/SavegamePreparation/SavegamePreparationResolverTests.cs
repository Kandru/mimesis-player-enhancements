using MimesisPlayerEnhancement.Features.SavegamePreparation;
using MimesisPlayerEnhancement.Util;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.SavegamePreparation
{
    public sealed class SavegamePreparationResolverTests
    {
        [Theory]
        [InlineData(1, 10, 1)]
        [InlineData(0, 10, 1)]
        [InlineData(5, 10, 5)]
        [InlineData(12, 10, 10)]
        public void ClampStartingZone_clamps_to_valid_range(int requested, int maxStage, int expected)
        {
            Assert.Equal(expected, SavegamePreparationResolver.ClampStartingZone(requested, maxStage));
        }

        [Theory]
        [InlineData(4, 1f, 1f, 1f)]
        [InlineData(8, 2f, true, 2.8f)]
        [InlineData(8, 2f, false, 2f)]
        public void ComputeStartupMoneyEffectiveMultiplier_combines_multiplier_and_player_scale(
            int playerCount,
            float startupMultiplier,
            bool autoScale,
            float expected)
        {
            float effective = SavegamePreparationResolver.ComputeStartupMoneyEffectiveMultiplier(
                startupMultiplier,
                autoScale,
                economyPlayerCountScaleRate: 0.10f,
                playerCount);

            Assert.Equal(expected, effective);
        }

        [Theory]
        [InlineData(100, 2f, 200)]
        [InlineData(0, 2f, 0)]
        public void ScaleStartupMoney_uses_effective_multiplier_when_config_reads_neutral(int vanilla, float multiplier, int expected)
        {
            int scaled = ScalingMath.ScaleCount(vanilla, multiplier);
            Assert.Equal(expected, scaled);
        }
    }
}
