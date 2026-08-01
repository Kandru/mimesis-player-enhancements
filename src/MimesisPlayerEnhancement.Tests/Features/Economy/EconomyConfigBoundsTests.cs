using System.Globalization;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Economy
{
    public sealed class EconomyConfigBoundsTests
    {
        private const string SectionId = "MimesisPlayerEnhancement_Economy";

        [Fact]
        public void EconomyBaselinePlayerCount_has_minimum_one()
        {
            Assert.True(ModConfigEntryBounds.TryGet(SectionId, "EconomyBaselinePlayerCount", out ModConfigEntryBound bound));
            Assert.Equal("1", bound.MinValue);
            Assert.Null(bound.MaxValue);
        }

        [Theory]
        [InlineData("ScrapSellValueMultiplier")]
        [InlineData("ScrapSellValuePerPlayerMultiplier")]
        [InlineData("ShopBuyPriceMultiplier")]
        [InlineData("ShopBuyPricePerPlayerMultiplier")]
        [InlineData("ReinforcePriceMultiplier")]
        [InlineData("ReinforcePricePerPlayerMultiplier")]
        public void Float_multipliers_have_minimum_zero(string key)
        {
            Assert.True(ModConfigEntryBounds.TryGet(SectionId, key, out ModConfigEntryBound bound));
            Assert.Equal(0f, float.Parse(bound.MinValue!, CultureInfo.InvariantCulture));
            Assert.Null(bound.MaxValue);
        }

        [Theory]
        [InlineData("ShopDiscountMinPercent")]
        [InlineData("ShopDiscountMaxPercent")]
        [InlineData("ShopDiscountChancePercent")]
        public void Shop_discount_percents_are_clamped_to_0_through_100(string key)
        {
            Assert.True(ModConfigEntryBounds.TryGet(SectionId, key, out ModConfigEntryBound bound));
            Assert.Equal("0", bound.MinValue);
            Assert.Equal("100", bound.MaxValue);
        }
    }
}
