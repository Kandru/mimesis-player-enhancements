using Steamworks;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.WebDashboard.Patches
{
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
