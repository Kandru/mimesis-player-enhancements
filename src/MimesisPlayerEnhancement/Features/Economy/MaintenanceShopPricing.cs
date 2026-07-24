namespace MimesisPlayerEnhancement.Features.Economy
{
    internal static class MaintenanceShopPricing
    {
        internal static int GetBasePrice(int price, float discountRate)
        {
            return price <= 0
                ? 0
                : discountRate is <= 0f or >= 1f
                    ? price
                    : Math.Max(1, (int)Math.Round(price / (1f - discountRate)));
        }

        internal static int ApplyDiscountRate(int basePrice, float discountRate)
        {
            if (basePrice <= 0)
            {
                return 0;
            }

            return discountRate is <= 0f or >= 1f
                ? basePrice
                : Math.Max(1, (int)Math.Round(basePrice * (1f - discountRate)));
        }

        internal static bool RollDiscount(int chancePercent)
        {
            return chancePercent >= 100 || chancePercent > 0 && SimpleRandUtil.Next(0, 10000) < chancePercent * 100;
        }

        internal static int RollDiscountPercent(int minPercent, int maxPercent)
        {
            return maxPercent <= minPercent ? minPercent : SimpleRandUtil.Next(minPercent, maxPercent + 1);
        }
    }
}
