namespace MimesisPlayerEnhancement.Features.Economy
{
    internal static class EconomyResolver
    {
        internal static bool IsAutoScaleEnabled(MoneyType type)
        {
            return IsAutoScaleEnabled(type, SceneScopedConfigGate.Economy);
        }

        internal static bool IsAutoScaleEnabled(MoneyType type, EconomySceneConfig config)
        {
            return type switch
            {
                MoneyType.Startup => config.AutoScaleStartupMoneyByPlayerCount,
                MoneyType.ScrapSellValue => config.AutoScaleScrapSellValueByPlayerCount,
                MoneyType.ShopBuyPrice => config.AutoScaleShopBuyPriceByPlayerCount,
                MoneyType.ReinforcePrice => config.AutoScaleReinforcePriceByPlayerCount,
                _ => false,
            };
        }

        internal static float GetPerTypeMultiplier(MoneyType type)
        {
            return GetPerTypeMultiplier(type, SceneScopedConfigGate.Economy);
        }

        internal static float GetPerTypeMultiplier(MoneyType type, EconomySceneConfig config)
        {
            return type switch
            {
                MoneyType.Startup => config.StartupMoneyMultiplier,
                MoneyType.ScrapSellValue => config.ScrapSellValueMultiplier,
                MoneyType.ShopBuyPrice => config.ShopBuyPriceMultiplier,
                MoneyType.ReinforcePrice => config.ReinforcePriceMultiplier,
                _ => 1f,
            };
        }

        internal static float GetPlayerScale(MoneyType type, int playerCount)
        {
            return GetPlayerScale(type, playerCount, SceneScopedConfigGate.Economy);
        }

        internal static float GetPlayerScale(MoneyType type, int playerCount, EconomySceneConfig config)
        {
            return ScalingMath.GetPlayerScale(
                playerCount,
                IsAutoScaleEnabled(type, config),
                config.EconomyPlayerCountScaleRate);
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

            return GetPerTypeMultiplier(type, config) * GetPlayerScale(type, playerCount, config);
        }

        internal static int ScaleAmount(int vanilla, float multiplier)
        {
            return ScalingMath.ScaleCount(vanilla, multiplier);
        }
    }
}
