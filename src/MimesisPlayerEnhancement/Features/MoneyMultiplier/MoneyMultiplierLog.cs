namespace MimesisPlayerEnhancement.Features.MoneyMultiplier
{
    internal static class MoneyMultiplierLog
    {
        private const string Feature = "MoneyMultiplier";

        internal static void DebugScaled(MoneyType type, int vanilla, int scaled, int playerCount, float effectiveMultiplier)
        {
            if (!ModConfig.EnableDebugLogging.Value || scaled == vanilla)
            {
                return;
            }

            ModLog.Debug(
                Feature,
                $"{FormatType(type)} scaled {vanilla} -> {scaled} (players={playerCount}, effective={effectiveMultiplier:0.##}×)");
        }

        internal static string FormatType(MoneyType type)
        {
            return type switch
            {
                MoneyType.Startup => "Startup money",
                MoneyType.RoundGoal => "Round goal",
                MoneyType.ScrapSellValue => "Scrap/sell value",
                MoneyType.ShopBuyPrice => "Shop buy price",
                MoneyType.ReinforcePrice => "Reinforce price",
                _ => type.ToString(),
            };
        }
    }
}
