using System.Globalization;
using MimesisPlayerEnhancement;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Economy
{
    public sealed class EconomyConfigBoundsTests
    {
        private const string SectionId = "MimesisPlayerEnhancement_Economy";

        [Theory]
        [InlineData("EconomyPlayerCountScaleRate")]
        [InlineData("StartupMoneyMultiplier")]
        [InlineData("ScrapSellValueMultiplier")]
        [InlineData("ShopBuyPriceMultiplier")]
        [InlineData("ReinforcePriceMultiplier")]
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
