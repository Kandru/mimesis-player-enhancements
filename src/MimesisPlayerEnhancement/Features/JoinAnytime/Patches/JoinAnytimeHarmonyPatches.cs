using System;
using System.Reflection;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;
using ReluProtocol;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.JoinAnytime.Patches
{
    [HarmonyPatch(typeof(GameSessionInfo), nameof(GameSessionInfo.CanEnterSession))]
    internal static class GameSessionInfoCanEnterSessionPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(ref bool __result)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return true;
            }

            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(SessionContext), nameof(SessionContext.Login))]
    internal static class SessionContextLoginPatch
    {
        [HarmonyPostfix]
        private static void Postfix(SessionContext __instance)
        {
            HostStatusCache.Invalidate();
            LateJoinManager.OnServerLogin(__instance);
        }
    }

    [HarmonyPatch(typeof(NetworkManagerV2), nameof(NetworkManagerV2.OnRecvPacket), typeof(IMsg))]
    internal static class NetworkManagerV2OnRecvPacketPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(IMsg msg)
        {
            return LateJoinManager.OnClientPacket(msg);
        }
    }

    [HarmonyPatch(typeof(Hub), nameof(Hub.LoadScene), typeof(string))]
    internal static class HubLoadScenePatch
    {
        [HarmonyPrefix]
        private static void Prefix(string sceneName)
        {
            HostStatusCache.Invalidate();
            LateJoinManager.OnLoadScene(sceneName);
        }
    }

    [HarmonyPatch(typeof(MaintenanceScene), "Start")]
    internal static class MaintenanceSceneStartPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            LateJoinManager.OnMaintenanceSceneStart();
        }
    }

    [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.EnterWaitingRoom))]
    internal static class VRoomManagerEnterWaitingRoomPatch
    {
        [HarmonyPrefix]
        private static void Prefix(SessionContext context)
        {
            LateJoinManager.OnServerEnterWaitingRoom(context);
        }
    }

    [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.EnterDungeon))]
    internal static class VRoomManagerEnterDungeonPatch
    {
        [HarmonyPrefix]
        private static void Prefix(SessionContext context, int hashCode, long roomUID)
        {
            LateJoinManager.OnServerEnterDungeon(context, roomUID);
        }
    }

    [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.EnterMaintenenceRoom))]
    internal static class VRoomManagerEnterMaintenenceRoomPatch
    {
        [HarmonyPrefix]
        private static void Prefix(SessionContext context, int hashCode)
        {
            LateJoinManager.OnServerEnterMaintenance(context);
        }
    }

    [HarmonyPatch(typeof(VoiceManager), nameof(VoiceManager.SetVoiceMode))]
    internal static class VoiceManagerSetVoiceModePatch
    {
        [HarmonyPostfix]
        private static void Postfix(VoiceManager __instance, VoiceMode voiceMode)
        {
            LateJoinManager.EnsureVoiceConnected(__instance, voiceMode);
        }
    }

    [HarmonyPatch(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.CreateLobby))]
    internal static class SteamInviteDispatcherCreateLobbyPatch
    {
        [HarmonyPostfix]
        private static void Postfix(SteamInviteDispatcher __instance, bool isOpenForRandomMatch)
        {
            LobbyVisibilityHelper.OnLobbyCreated(__instance, isOpenForRandomMatch);
        }
    }

    [HarmonyPatch(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.SetLobbyPublic))]
    internal static class SteamInviteDispatcherSetLobbyPublicPatch
    {
        [HarmonyPostfix]
        private static void Postfix(SteamInviteDispatcher __instance, bool isPublic)
        {
            LobbyVisibilityHelper.OnSetLobbyPublicCompleted(__instance, isPublic);
        }
    }

    [HarmonyPatch(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.SetPresenceInLobby))]
    internal static class SteamInviteDispatcherSetPresenceInLobbyPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(SteamInviteDispatcher __instance)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return true;
            }

            if (JoinAnytimeHub.IsHostLobbyPublic(__instance))
            {
                __instance.SetPresenceInLobbyPublic();
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch]
    internal static class GameMainBaseCorRefreshSteamLobbyDataPatch
    {
        private static MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(GameMainBase), "CorRefreshSteamLobbyData", [typeof(Action<bool>)]);

        [HarmonyPostfix]
        private static void Postfix()
        {
            LateJoinManager.RefreshLobbyVisibilityAfterSteamUpdate();
        }
    }

    [HarmonyPatch]
    internal static class VPlayerCtorPatch
    {
        private static MethodBase? TargetMethod() =>
            AccessTools.Constructor(
                typeof(VPlayer),
                [
                    typeof(SessionContext),
                    typeof(int),
                    typeof(int),
                    typeof(bool),
                    typeof(string),
                    typeof(string),
                    typeof(PosWithRot),
                    typeof(bool),
                    typeof(IVroom),
                    typeof(ReasonOfSpawn),
                ]);

        [HarmonyPostfix]
        private static void Postfix(VPlayer __instance)
        {
            LateJoinManager.OnServerPlayerCreated(__instance);
        }
    }
}
