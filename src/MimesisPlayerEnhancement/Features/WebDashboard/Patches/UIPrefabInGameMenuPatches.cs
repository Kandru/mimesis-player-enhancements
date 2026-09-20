using UnityEngine;

namespace MimesisPlayerEnhancement.Features.WebDashboard.Patches
{
    // game@0.3.2 Assembly-CSharp/UIPrefab_InGameMenu.cs:L898-937
    [HarmonyPatch(typeof(UIPrefab_InGameMenu), nameof(UIPrefab_InGameMenu.GetSteamAvatar))]
    internal static class SteamAvatarLoadedPatch
    {
        [HarmonyPostfix]
        private static void Postfix(CSteamID steamID, Texture2D __result)
        {
            if (__result == null)
            {
                return;
            }

            if (WebDashboardGameAvatarSource.OnAvatarLoaded(steamID.m_SteamID, __result))
            {
                WebDashboardSnapshotCache.MarkDirty();
            }
        }
    }

    // game@0.3.2 Assembly-CSharp/UIPrefab_InGameMenu.cs:L939-1000
    [HarmonyPatch(typeof(UIPrefab_InGameMenu), nameof(UIPrefab_InGameMenu.SetRemoteVolumeController_v2))]
    internal static class VolumeControllerAvatarSyncPatch
    {
        [HarmonyPostfix]
        private static void Postfix(UIPrefab_InGameMenu __instance)
        {
            if (WebDashboardGameAvatarSource.SyncFromInGameMenu(__instance))
            {
                WebDashboardSnapshotCache.MarkDirty();
            }
        }
    }
}
