using System;
using System.Collections.Generic;
using System.Reflection;
using Bifrost.ConstEnum;
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

    [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.InitWaitingRoom))]
    internal static class VRoomManagerInitWaitingRoomPatch
    {
        [HarmonyPostfix]
        private static void Postfix(VRoomManager __instance)
        {
            JoinAnytimeRoomTools.RefreshWaitingRoomDisplaysForOccupants(__instance);
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

    [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.EnterMaintenenceRoom))]
    internal static class VRoomManagerEnterMaintenenceRoomPatch
    {
        [HarmonyPrefix]
        private static void Prefix(SessionContext context, int hashCode)
        {
            LateJoinManager.OnServerEnterMaintenance(context);
        }
    }

    [HarmonyPatch(typeof(IVroom), "RunEventActionInternal")]
    internal static class IVroomRunEventActionInternalPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(
            IVroom __instance,
            IGameAction action,
            List<IGameActionParam> paramList,
            ref bool __result)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return true;
            }

            if (action is not GameAction gameAction
                || gameAction.ActionType != DefAction.MOVE_TO_NEXT_ROOM)
            {
                return true;
            }

            if (__instance is not VWaitingRoom waitingRoom || waitingRoom.BackToMaintenance)
            {
                return true;
            }

            if (!JoinAnytimeRoomTools.ShouldBlockWaitingRoomStartGame())
            {
                return true;
            }

            int actorId = GameActionParamHelper.FindParam<GameActionParamActor>(paramList)?.ActorID ?? 0;
            ModLog.Info("JoinAnytime", "Blocked tram lever — players still split between dungeon and waiting room");
            JoinAnytimeUserMessages.OnWaitingRoomStartBlocked(__instance, actorId);
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(NewTramLeverLevelObject), nameof(NewTramLeverLevelObject.OnChangeLevelObjectStateSig))]
    internal static class NewTramLeverLevelObjectOnChangeLevelObjectStateSigPatch
    {
        [HarmonyPostfix]
        private static void Postfix(int actorId, int prevState, int currentState)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            if (currentState != (int)NewTramLeverState.Open)
            {
                return;
            }

            JoinAnytimeUserMessages.OnLocalTramLeverOpened(actorId);
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
    internal static class SteamInviteDispatcherSetLobbyPublicPresencePatch
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
