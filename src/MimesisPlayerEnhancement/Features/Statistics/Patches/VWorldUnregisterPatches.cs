using HarmonyLib;

namespace MimesisPlayerEnhancement.Features.Statistics.Patches;

[HarmonyPatch(typeof(VWorld), nameof(VWorld.OnUnregistPlayer))]
public static class VWorldUnregisterPatches
{
    [HarmonyPostfix]
    public static void Postfix(ulong steamID)
    {
        if (!ModConfig.EnableStatistics.Value)
            return;

        StatisticsTracker.OnPlayerUnregistered(steamID);
    }
}
