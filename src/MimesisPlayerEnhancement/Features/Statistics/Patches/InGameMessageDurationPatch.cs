using System.Collections;
using HarmonyLib;

namespace MimesisPlayerEnhancement.Features.Statistics.Patches;

[HarmonyPatch(typeof(UIPrefab_PlayerEnterInfo), "UpdatePlayerInfos")]
internal static class InGameMessageDurationPatch
{
    [HarmonyPrefix]
    private static bool Prefix(UIPrefab_PlayerEnterInfo __instance, ref IEnumerator __result)
    {
        __result = InGameMessageHelper.RunExtendedPlayerEnterInfoUpdates(__instance);
        return false;
    }
}
