using MimesisPlayerEnhancement.Features.Economy;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Economy
{
    public sealed class MaintenanceShopPricingTests
    {
        [Theory]
        [InlineData(100, 0f, 100)]
        [InlineData(100, 1f, 100)]
        [InlineData(80, 0.20f, 100)]
        [InlineData(0, 0.50f, 0)]
        [InlineData(-5, 0.10f, 0)]
        public void GetBasePrice_recovers_undiscounted_price(int price, float discountRate, int expected)
        {
            Assert.Equal(expected, MaintenanceShopPricing.GetBasePrice(price, discountRate));
        }

        [Theory]
        [InlineData(100, 0f, 100)]
        [InlineData(100, 1f, 100)]
        [InlineData(100, 0.20f, 80)]
        [InlineData(0, 0.50f, 0)]
        [InlineData(7, 0.10f, 6)]
        public void ApplyDiscountRate_applies_rounded_discount(int basePrice, float discountRate, int expected)
        {
            Assert.Equal(expected, MaintenanceShopPricing.ApplyDiscountRate(basePrice, discountRate));
        }

        [Theory]
        [InlineData(100, 0.25f)]
        [InlineData(50, 0.10f)]
        [InlineData(40, 0.50f)]
        public void GetBasePrice_then_ApplyDiscountRate_round_trips_discounted_price(int basePrice, float discountRate)
        {
            int discounted = MaintenanceShopPricing.ApplyDiscountRate(basePrice, discountRate);
            int recovered = MaintenanceShopPricing.GetBasePrice(discounted, discountRate);

            Assert.Equal(basePrice, recovered);
        }

        [Theory]
        [InlineData(10, 10, 10)]
        [InlineData(5, 5, 5)]
        public void RollDiscountPercent_returns_min_when_range_is_degenerate(int min, int max, int expected)
        {
            Assert.Equal(expected, MaintenanceShopPricing.RollDiscountPercent(min, max));
        }

        [Fact]
        public void RollDiscount_always_true_at_100_percent()
        {
            Assert.True(MaintenanceShopPricing.RollDiscount(100));
        }

        [Fact]
        public void RollDiscount_always_false_at_0_percent()
        {
            Assert.False(MaintenanceShopPricing.RollDiscount(0));
        }
    }
}
