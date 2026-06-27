using System;
using System.IO;
using HarmonyLib;
using ReluProtocol;

namespace MimesisPlayerEnhancement.Features.Statistics.Patches;

[HarmonyPatch(typeof(PlatformMgr), nameof(PlatformMgr.Delete))]
public static class StatisticsPlatformMgrDeletePatches
{
    private const string Feature = "Statistics";

    [HarmonyPostfix]
    public static void Postfix(string fileName)
    {
        if (!ModConfig.EnableStatistics.Value)
            return;

        try
        {
            if (string.IsNullOrEmpty(fileName) || !fileName.StartsWith("MMGameData", StringComparison.OrdinalIgnoreCase))
                return;

            string slotStr = Path.GetFileNameWithoutExtension(fileName).Replace("MMGameData", "");
            if (int.TryParse(slotStr, out int slotId) && MMSaveGameData.CheckSaveSlotID(slotId, true))
                StatisticsStore.DeleteStatisticsData(slotId);
        }
        catch (Exception ex)
        {
            ModLog.Warn(Feature, $"PlatformMgr.Delete: {ex.Message}");
        }
    }
}
