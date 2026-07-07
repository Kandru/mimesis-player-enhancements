using System.Collections.Concurrent;
using System.Reflection;
using Steamworks;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardPatches
    {
        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = HarmonyPatchHelper.ApplyPatchTypes(harmony, "WebDashboard", HarmonyPatchHelper.GetNestedPatchTypes(typeof(WebDashboardPatches)));
            WebDashboardHostCheatsPatches.Apply(harmony);
        }

        private static readonly ConcurrentDictionary<long, int> GradeByPlayerUid = new();

        internal static bool TryGetCachedGrade(long playerUid, out int grade)
        {
            return GradeByPlayerUid.TryGetValue(playerUid, out grade);
        }

        [HarmonyPatch]
        internal static class NetworkGradeSigPatch
        {
            private static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(GameMainBase), "OnPacket", [typeof(NetworkGradeSig)])
                    ?? throw new InvalidOperationException("OnPacket(NetworkGradeSig) not found");
            }

            private static void Postfix(NetworkGradeSig sig)
            {
                if (sig?.grades == null)
                {
                    return;
                }

                bool changed = false;
                foreach (System.Collections.Generic.KeyValuePair<long, ReluProtocol.Enum.NetworkGrade> pair in sig.grades)
                {
                    int grade = (int)pair.Value;
                    if (!GradeByPlayerUid.TryGetValue(pair.Key, out int existing) || existing != grade)
                    {
                        GradeByPlayerUid[pair.Key] = grade;
                        changed = true;
                    }
                }

                if (changed)
                {
                    WebDashboardSnapshotCache.MarkDirty();
                }
            }
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

        [HarmonyPatch(typeof(GameMainBase), nameof(GameMainBase.OnPlayerDeath))]
        internal static class PlayerDeathSnapshotPatch
        {
            private static void Postfix()
            {
                WebDashboardHostCheatsRuntime.DisableAll("host died");
                WebDashboardSnapshotCache.MarkDirty();
            }
        }

        [HarmonyPatch(typeof(GameMainBase), nameof(GameMainBase.OnPlayerRevive))]
        internal static class PlayerReviveSnapshotPatch
        {
            private static void Postfix()
            {
                WebDashboardSnapshotCache.MarkDirty();
            }
        }

        [HarmonyPatch(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.SetLobbyName))]
        internal static class SetLobbyNameSnapshotPatch
        {
            private static void Postfix()
            {
                WebDashboardSnapshotCache.MarkDirty();
            }
        }

        [HarmonyPatch(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.LeaveLobby))]
        internal static class LeaveLobbySnapshotPatch
        {
            private static void Postfix()
            {
                WebDashboardSnapshotCache.MarkDirty();
            }
        }
    }
}
