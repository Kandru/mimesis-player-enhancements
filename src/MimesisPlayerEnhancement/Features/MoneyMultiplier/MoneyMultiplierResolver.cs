namespace MimesisPlayerEnhancement.Features.MoneyMultiplier;

internal static class MoneyMultiplierResolver
{
    private const int VanillaPlayerBaseline = 4;

    internal static bool IsAutoScaleEnabled(MoneyType type) =>
        type switch
        {
            MoneyType.Startup => ModConfig.AutoScaleStartupMoneyByPlayerCount.Value,
            MoneyType.RoundGoal => ModConfig.AutoScaleRoundGoalMoneyByPlayerCount.Value,
            MoneyType.ScrapSellValue => ModConfig.AutoScaleScrapSellValueByPlayerCount.Value,
            MoneyType.ShopBuyPrice => ModConfig.AutoScaleShopBuyPriceByPlayerCount.Value,
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
            MoneyType.ReinforcePrice => ModConfig.ReinforcePriceMultiplier.Value,
            _ => 1f,
        };

    internal static float GetPlayerScale(MoneyType type, int playerCount)
    {
        if (!IsAutoScaleEnabled(type) || playerCount <= VanillaPlayerBaseline)
            return 1f;

        return playerCount / (float)VanillaPlayerBaseline;
    }

    internal static float GetEffectiveMultiplier(MoneyType type, int playerCount) =>
        GetPerTypeMultiplier(type) * GetPlayerScale(type, playerCount);

    internal static int ScaleAmount(int vanilla, float multiplier)
    {
        if (vanilla == 0)
            return 0;

        if (multiplier <= 0f)
            return 0;

        return System.Math.Max(1, (int)System.Math.Round(vanilla * multiplier));
    }
}
