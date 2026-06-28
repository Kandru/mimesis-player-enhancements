using System.Collections.Concurrent;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;
using Steamworks;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardPatches
    {
        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = HarmonyPatchHelper.ApplyPatchTypes(harmony, "WebDashboard", HarmonyPatchHelper.GetNestedPatchTypes(typeof(WebDashboardPatches)));
        }

        private static readonly ConcurrentDictionary<long, int> GradeByPlayerUid = new();

        internal static bool TryGetCachedGrade(long playerUid, out int grade)
        {
            return GradeByPlayerUid.TryGetValue(playerUid, out grade);
        }

        [HarmonyPatch]
        internal static class NetworkGradeSigPatch
        {
        }

        [HarmonyPatch(typeof(UIPrefab_InGameMenu), nameof(UIPrefab_InGameMenu.GetSteamAvatar))]
        internal static class SteamAvatarLoadedPatch
        {
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
            private static void Postfix(UIPrefab_InGameMenu __instance)
            {
                if (WebDashboardGameAvatarSource.SyncFromInGameMenu(__instance))
                {
                    WebDashboardSnapshotCache.MarkDirty();
                }
            }
        }
    }
}
