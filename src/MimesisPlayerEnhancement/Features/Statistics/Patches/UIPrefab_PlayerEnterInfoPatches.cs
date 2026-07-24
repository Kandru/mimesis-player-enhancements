using System.Collections;

namespace MimesisPlayerEnhancement.Features.Statistics.Patches
{
    // game@0.3.1 Assembly-CSharp/UIPrefab_PlayerEnterInfo.cs:L59-73
    [HarmonyPatch(typeof(UIPrefab_PlayerEnterInfo), nameof(UIPrefab_PlayerEnterInfo.AddPlayerInfo))]
    internal static class UIPrefabPlayerEnterInfoAddPlayerInfoPatches
    {
        [HarmonyPostfix]
        private static void Postfix(string userName, bool isEntering)
        {
            StatisticsMessages.OnGamePlayerInfoShown(userName, isEntering);
        }
    }

    // game@0.3.1 Assembly-CSharp/UIPrefab_PlayerEnterInfo.cs:L75-119
    [HarmonyPatch(typeof(UIPrefab_PlayerEnterInfo), "UpdatePlayerInfos")]
    internal static class UIPrefabPlayerEnterInfoDurationPatches
    {
        private const string Feature = "Statistics";

        [HarmonyPrefix]
        private static bool Prefix(UIPrefab_PlayerEnterInfo __instance, ref IEnumerator __result)
        {
            // Only hijack the vanilla toast coroutine when mod toasts can actually be shown;
            // otherwise keep vanilla join/leave toast timing untouched.
            if (!ModConfig.ShowStatisticsToasts.Value && !ModConfig.ShowPlayerAnnouncements.Value)
            {
                return true;
            }

            try
            {
                IEnumerator? extended = InGameMessageHelper.TryCreateExtendedPlayerEnterInfoUpdates(__instance);
                if (extended == null)
                {
                    return true;
                }

                __result = extended;
                return false;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Toast duration patch failed — {ex.Message}");
                return true;
            }
        }
    }
}
