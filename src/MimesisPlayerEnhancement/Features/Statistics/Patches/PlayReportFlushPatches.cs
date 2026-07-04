namespace MimesisPlayerEnhancement.Features.Statistics.Patches
{
    [HarmonyPatch(typeof(PlayReportManager), nameof(PlayReportManager.FlushCurrentToAccumulated))]
    public static class PlayReportFlushPatches
    {
        [HarmonyPrefix]
        public static void Prefix(PlayReportManager __instance)
        {
            StatisticsPatchGuard.Run(nameof(PlayReportManager.FlushCurrentToAccumulated), () =>
            {
                Dictionary<ulong, PlayReportData> snapshot = new(__instance.CurrentReportDict);
                StatisticsTracker.OnDungeonReportFlushed(__instance, snapshot);
            });
        }
    }
}
