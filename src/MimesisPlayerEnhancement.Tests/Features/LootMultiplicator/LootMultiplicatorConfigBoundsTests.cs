using System.Globalization;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.LootMultiplicator
{
    public sealed class LootMultiplicatorConfigBoundsTests
    {
        private const string SectionId = "MimesisPlayerEnhancement_LootMultiplicator";

        [Fact]
        public void LootMultiplicatorBaselinePlayerCount_has_minimum_one()
        {
            Assert.True(ModConfigEntryBounds.TryGet(SectionId, "LootMultiplicatorBaselinePlayerCount", out ModConfigEntryBound bound));
            Assert.Equal("1", bound.MinValue);
            Assert.Null(bound.MaxValue);
        }

        [Theory]
        [InlineData("MapLootMultiplier")]
        [InlineData("MapLootPerPlayerMultiplier")]
        [InlineData("DropLootMultiplier")]
        [InlineData("DropLootPerPlayerMultiplier")]
        public void Float_multipliers_have_minimum_zero(string key)
        {
            Assert.True(ModConfigEntryBounds.TryGet(SectionId, key, out ModConfigEntryBound bound));
            Assert.Equal(0f, float.Parse(bound.MinValue!, CultureInfo.InvariantCulture));
            Assert.Null(bound.MaxValue);
        }

        [Fact]
        public void ConvertFakeActorDyingDropChancePercent_is_clamped_to_0_through_100()
        {
            Assert.True(ModConfigEntryBounds.TryGet(
                SectionId,
                "ConvertFakeActorDyingDropChancePercent",
                out ModConfigEntryBound bound));
            Assert.Equal("0", bound.MinValue);
            Assert.Equal("100", bound.MaxValue);
        }
    }
}
