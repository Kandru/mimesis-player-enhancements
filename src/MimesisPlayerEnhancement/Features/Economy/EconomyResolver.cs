namespace MimesisPlayerEnhancement.Features.Economy
{
    internal static class EconomyResolver
    {
        internal static bool IsAutoScaleEnabled(MoneyType type)
        {
            return type switch
            {
                MoneyType.Startup => ModConfig.AutoScaleStartupMoneyByPlayerCount.Value,
                MoneyType.ScrapSellValue => ModConfig.AutoScaleScrapSellValueByPlayerCount.Value,
                MoneyType.ShopBuyPrice => ModConfig.AutoScaleShopBuyPriceByPlayerCount.Value,
                MoneyType.ReinforcePrice => ModConfig.AutoScaleReinforcePriceByPlayerCount.Value,
                _ => false,
            };
        }

        internal static float GetPerTypeMultiplier(MoneyType type)
        {
            return type switch
            {
                MoneyType.Startup => ModConfig.StartupMoneyMultiplier.Value,
                MoneyType.ScrapSellValue => ModConfig.ScrapSellValueMultiplier.Value,
                MoneyType.ShopBuyPrice => ModConfig.ShopBuyPriceMultiplier.Value,
                MoneyType.ReinforcePrice => ModConfig.ReinforcePriceMultiplier.Value,
                _ => 1f,
            };
        }

        internal static float GetPlayerScale(MoneyType type, int playerCount)
        {
            return ScalingMath.GetPlayerScale(
                playerCount,
                IsAutoScaleEnabled(type),
                ModConfig.EconomyPlayerCountScaleRate.Value);
        }

        internal static float GetEffectiveMultiplier(MoneyType type, int playerCount)
        {
            if (!ModConfig.EnableEconomy.Value)
            {
                return FeatureToggleGate.NeutralMultiplier;
            }

            return GetPerTypeMultiplier(type) * GetPlayerScale(type, playerCount);
        }

        internal static int ScaleAmount(int vanilla, float multiplier)
        {
            return ScalingMath.ScaleCount(vanilla, multiplier);
        }
    }
}
