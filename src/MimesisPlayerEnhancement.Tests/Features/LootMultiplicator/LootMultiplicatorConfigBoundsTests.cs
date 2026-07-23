using System.Globalization;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.LootMultiplicator
{
    public sealed class LootMultiplicatorConfigBoundsTests
    {
        private const string SectionId = "MimesisPlayerEnhancement_LootMultiplicator";

        [Theory]
        [InlineData("LootMultiplicatorPlayerCountScaleRate")]
        [InlineData("MapLootMultiplier")]
        [InlineData("DropLootMultiplier")]
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
