using HarmonyLib;

namespace MimesisPlayerEnhancement.Features.Statistics.Patches
{
    [HarmonyPatch(typeof(UIPrefab_PlayerEnterInfo), "UpdatePlayerInfos")]
    internal static class InGameMessageDurationPatch
    {
    }
}
