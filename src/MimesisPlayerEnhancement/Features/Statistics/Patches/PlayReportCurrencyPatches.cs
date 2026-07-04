namespace MimesisPlayerEnhancement.Features.Statistics.Patches
{
    [HarmonyPatch(typeof(PlayReportManager), nameof(PlayReportManager.IncreaseCurrency))]
    public static class PlayReportCurrencyPatches
    {
        [HarmonyPostfix]
        public static void Postfix(long currency)
        {
            StatisticsPatchGuard.Run(nameof(PlayReportManager.IncreaseCurrency), () =>
            {
                StatisticsTracker.OnCurrencyEarned(currency);
            });
        }
    }
}
