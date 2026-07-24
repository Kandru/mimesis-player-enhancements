namespace MimesisPlayerEnhancement.Features.Statistics.Patches
{
    // game@0.3.1 Assembly-CSharp/PlayReportManager.cs:L125-149
    [HarmonyPatch(typeof(PlayReportManager), nameof(PlayReportManager.FlushCurrentToAccumulated))]
    public static class PlayReportManagerFlushPatches
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

    // game@0.3.1 Assembly-CSharp/PlayReportManager.cs:L115-118
    [HarmonyPatch(typeof(PlayReportManager), nameof(PlayReportManager.SetDeathMatchSurvivor))]
    public static class PlayReportManagerDeathMatchSurvivorPatches
    {
        [HarmonyPostfix]
        public static void Postfix(ulong steamID)
        {
            StatisticsPatchGuard.Run(nameof(PlayReportManager.SetDeathMatchSurvivor), () =>
            {
                StatisticsTracker.OnDeathmatchSurvivor(steamID);
            });
        }
    }
}
