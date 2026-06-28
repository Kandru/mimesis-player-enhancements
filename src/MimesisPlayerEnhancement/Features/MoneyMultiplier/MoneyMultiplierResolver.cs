using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.MoneyMultiplier;

internal static class MoneyMultiplierResolver
{
    internal static bool IsAutoScaleEnabled(MoneyType type) =>
        type switch
        {
            MoneyType.Startup => ModConfig.AutoScaleStartupMoneyByPlayerCount.Value,
            MoneyType.RoundGoal => ModConfig.AutoScaleRoundGoalMoneyByPlayerCount.Value,
            MoneyType.ScrapSellValue => ModConfig.AutoScaleScrapSellValueByPlayerCount.Value,
            MoneyType.ShopBuyPrice => ModConfig.AutoScaleShopBuyPriceByPlayerCount.Value,
            MoneyType.ShopItems => ModConfig.AutoScaleShopItemsByPlayerCount.Value,
            MoneyType.ReinforcePrice => ModConfig.AutoScaleReinforcePriceByPlayerCount.Value,
            _ => false,
        };

    internal static float GetPerTypeMultiplier(MoneyType type) =>
        type switch
        {
            MoneyType.Startup => ModConfig.StartupMoneyMultiplier.Value,
            MoneyType.RoundGoal => ModConfig.RoundGoalMoneyMultiplier.Value,
            MoneyType.ScrapSellValue => ModConfig.ScrapSellValueMultiplier.Value,
            MoneyType.ShopBuyPrice => ModConfig.ShopBuyPriceMultiplier.Value,
            MoneyType.ShopItems => ModConfig.ShopItemsMultiplier.Value,
            MoneyType.ReinforcePrice => ModConfig.ReinforcePriceMultiplier.Value,
            _ => 1f,
        };

    internal static float GetPlayerScale(MoneyType type, int playerCount) =>
        ScalingMath.GetPlayerScale(playerCount, IsAutoScaleEnabled(type));

    internal static float GetEffectiveMultiplier(MoneyType type, int playerCount) =>
        GetPerTypeMultiplier(type) * GetPlayerScale(type, playerCount);

    internal static int ScaleAmount(int vanilla, float multiplier) =>
        ScalingMath.ScaleCount(vanilla, multiplier);
}
