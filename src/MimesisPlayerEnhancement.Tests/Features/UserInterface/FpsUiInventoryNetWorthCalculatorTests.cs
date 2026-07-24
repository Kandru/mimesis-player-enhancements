using MimesisPlayerEnhancement.Features.UserInterface.FpsUi;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.UserInterface
{
    public sealed class FpsUiInventoryNetWorthCalculatorTests
    {
        [Fact]
        public void ComputeEquipmentSellPrice_returns_base_when_no_gauge_bonus()
        {
            int price = FpsUiInventoryNetWorthCalculator.ComputeEquipmentSellPrice(
                basePrice: 100,
                remainGauge: 50,
                overflowPrice: 0,
                priceIncPerGauge: 0);

            Assert.Equal(100, price);
        }

        [Fact]
        public void ComputeEquipmentSellPrice_uses_overflow_when_remain_gauge_is_minus_one()
        {
            int price = FpsUiInventoryNetWorthCalculator.ComputeEquipmentSellPrice(
                basePrice: 100,
                remainGauge: -1,
                overflowPrice: 250,
                priceIncPerGauge: 10);

            Assert.Equal(250, price);
        }

        [Fact]
        public void ComputeEquipmentSellPrice_ignores_zero_overflow_at_minus_one_gauge()
        {
            int price = FpsUiInventoryNetWorthCalculator.ComputeEquipmentSellPrice(
                basePrice: 100,
                remainGauge: -1,
                overflowPrice: 0,
                priceIncPerGauge: 0);

            Assert.Equal(100, price);
        }

        [Theory]
        [InlineData(100, 100, 10, 110)]
        [InlineData(100, 50, 10, 105)]
        [InlineData(200, 0, 25, 200)]
        public void ComputeEquipmentSellPrice_applies_per_gauge_bonus(
            int basePrice,
            int remainGauge,
            int priceIncPerGauge,
            int expected)
        {
            int price = FpsUiInventoryNetWorthCalculator.ComputeEquipmentSellPrice(
                basePrice,
                remainGauge,
                overflowPrice: 0,
                priceIncPerGauge);

            Assert.Equal(expected, price);
        }

        [Fact]
        public void ComputeFromInventoryItems_skips_null_and_fake_entries()
        {
            int total = FpsUiInventoryNetWorthCalculator.ComputeFromInventoryItems([]);

            Assert.Equal(0, total);
        }
    }
}
