namespace MimesisPlayerEnhancement.Features.Economy
{
    internal static class EconomyLog
    {
        private const string Feature = "Economy";

        internal static void InfoApplied(MoneyType type, int vanilla, int scaled, int playerCount, float effectiveMultiplier)
        {
            if (scaled == vanilla)
            {
                return;
            }

            ModLog.Info(
                Feature,
                $"{FormatType(type)} applied — {vanilla} -> {scaled} (players={playerCount}, effective={effectiveMultiplier:0.##}×)");
        }

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

        internal static void InfoRetainedCurrency(int amount, int stage)
        {
            ModLog.Info(Feature, $"Retained unspent currency — amount={amount}, stage={stage}");
        }

        internal static void InfoScrapScalingActive(int playerCount, float effectiveMultiplier)
        {
            ModLog.Info(
                Feature,
                $"Scrap/sell value scaling active — players={playerCount}, effective={effectiveMultiplier:0.##}×");
        }

        internal static string FormatType(MoneyType type)
        {
            return type switch
            {
                MoneyType.Startup => "Startup money",
                MoneyType.ScrapSellValue => "Scrap/sell value",
                MoneyType.ShopBuyPrice => "Shop buy price",
                MoneyType.ReinforcePrice => "Reinforce price",
                _ => type.ToString(),
            };
        }
    }
}
