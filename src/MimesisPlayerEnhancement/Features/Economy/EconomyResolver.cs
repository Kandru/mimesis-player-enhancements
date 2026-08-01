namespace MimesisPlayerEnhancement.Features.Economy
{
    internal static class EconomyResolver
    {
        internal static float GetPerTypeMultiplier(MoneyType type, EconomySceneConfig config)
        {
            return type switch
            {
                MoneyType.ScrapSellValue => config.ScrapSellValueMultiplier,
                MoneyType.ShopBuyPrice => config.ShopBuyPriceMultiplier,
                MoneyType.ReinforcePrice => config.ReinforcePriceMultiplier,
                _ => 1f,
            };
        }

        internal static float GetPerPlayerMultiplier(MoneyType type, EconomySceneConfig config)
        {
            return type switch
            {
                MoneyType.ScrapSellValue => config.ScrapSellValuePerPlayerMultiplier,
                MoneyType.ShopBuyPrice => config.ShopBuyPricePerPlayerMultiplier,
                MoneyType.ReinforcePrice => config.ReinforcePricePerPlayerMultiplier,
                _ => 0f,
            };
        }

        internal static float GetEffectiveMultiplier(MoneyType type, int playerCount)
        {
            return GetEffectiveMultiplier(type, playerCount, SceneScopedConfigGate.Economy);
        }

        internal static float GetEffectiveMultiplier(MoneyType type, int playerCount, EconomySceneConfig config)
        {
            if (!config.EnableEconomy)
            {
                return FeatureToggleGate.NeutralMultiplier;
            }

            return ScalingMath.GetAdditiveMultiplier(
                GetPerTypeMultiplier(type, config),
                GetPerPlayerMultiplier(type, config),
                playerCount,
                config.EconomyBaselinePlayerCount);
        }

        internal static int ScaleAmount(int vanilla, float multiplier)
        {
            return ScalingMath.ScaleCount(vanilla, multiplier);
        }
    }
}
