using HarmonyLib;

namespace MimesisPlayerEnhancement.Features.Statistics.Patches;

[HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.OnRegistPlayer))]
public static class VRoomManagerRegisterPatches
{
    [HarmonyPostfix]
    public static void Postfix(ulong steamID)
    {
        if (!ModConfig.EnableStatistics.Value)
            return;

        StatisticsTracker.OnPlayerRegistered(steamID);
    }
}
