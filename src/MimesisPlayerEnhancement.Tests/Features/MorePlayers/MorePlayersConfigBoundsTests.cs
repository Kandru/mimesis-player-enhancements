using System.Globalization;
using MimesisPlayerEnhancement.Features.MorePlayers;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.MorePlayers
{
    public sealed class MorePlayersConfigBoundsTests
    {
        private const string SectionId = "MimesisPlayerEnhancement_MorePlayers";

        [Fact]
        public void MaxPlayers_has_minimum_one()
        {
            Assert.True(ModConfigEntryBounds.TryGet(SectionId, "MaxPlayers", out ModConfigEntryBound bound));
            Assert.Equal("1", bound.MinValue);
            Assert.Null(bound.MaxValue);
        }

        [Theory]
        [InlineData("RoundGoalBasePerZone")]
        [InlineData("RoundGoalMoneyMultiplier")]
        public void Round_goal_floats_have_minimum_zero(string entryId)
        {
            Assert.True(ModConfigEntryBounds.TryGet(SectionId, entryId, out ModConfigEntryBound bound));
            Assert.Equal(0f, float.Parse(bound.MinValue!, CultureInfo.InvariantCulture));
            Assert.Null(bound.MaxValue);
        }

        [Fact]
        public void RoundGoalCurveExponent_uses_resolver_bounds()
        {
            Assert.True(ModConfigEntryBounds.TryGet(SectionId, "RoundGoalCurveExponent", out ModConfigEntryBound bound));
            Assert.Equal(
                RoundGoalScalingResolver.MinCurveExponent,
                float.Parse(bound.MinValue!, CultureInfo.InvariantCulture));
            Assert.Equal(
                RoundGoalScalingResolver.MaxCurveExponent,
                float.Parse(bound.MaxValue!, CultureInfo.InvariantCulture));
        }

        [Fact]
        public void RoundGoalRandomSpreadPercent_is_clamped_to_0_through_100()
        {
            Assert.True(ModConfigEntryBounds.TryGet(SectionId, "RoundGoalRandomSpreadPercent", out ModConfigEntryBound bound));
            Assert.Equal("0", bound.MinValue);
            Assert.Equal("100", bound.MaxValue);
        }
    }
}
