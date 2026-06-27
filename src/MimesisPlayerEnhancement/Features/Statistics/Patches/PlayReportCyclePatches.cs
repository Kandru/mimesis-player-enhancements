using HarmonyLib;

namespace MimesisPlayerEnhancement.Features.Statistics.Patches;

[HarmonyPatch(typeof(PlayReportManager), nameof(PlayReportManager.IncreaseCycleCount))]
public static class PlayReportCyclePatches
{
    [HarmonyPostfix]
    public static void Postfix(PlayReportManager __instance)
    {
        if (!ModConfig.EnableStatistics.Value)
            return;

        StatisticsTracker.OnCycleCompleted(__instance);
    }
}
