using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bifrost.ConstEnum;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;
using Mimic.Actors;
using ReluProtocol;
using ReluProtocol.Enum;
using Steamworks;

namespace MimesisPlayerEnhancement.Features.JoinAnytime.Patches
{
    [HarmonyPatch(typeof(GameSessionInfo), nameof(GameSessionInfo.CanEnterSession))]
    internal static class GameSessionInfoCanEnterSessionPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(GameSessionInfo __instance, ref bool __result)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return true;
            }

            switch (__instance.GameSessionState)
            {
                case VGameSessionState.Ready:
                case VGameSessionState.WaitStartSession:
                case VGameSessionState.EndGame:
                    __result = true;
                    return false;
                case VGameSessionState.OnPlaying:
                case VGameSessionState.DeathMatch:
                case VGameSessionState.AfterGame:
                    __result = false;
                    return false;
                default:
                    __result = JoinAnytimeRoomTools.AreJoinsOpen();
                    return false;
            }
        }
    }

    [HarmonyPatch(typeof(SessionContext), nameof(SessionContext.Login))]
    internal static class SessionContextLoginPatch
    {
        [HarmonyPostfix]
        private static void Postfix(SessionContext __instance)
        {
            HostStatusCache.Invalidate();
            JoinAnytimeConnectingTracker.OnServerLogin(__instance);
        }
    }

    [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.PendMoveToDungeon))]
    internal static class VRoomManagerPendMoveToDungeonPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            JoinAnytimeRoomTools.InvalidateWaitingRoomPrepareCache();
            JoinAnytimeLobbyController.ApplyHostPublicLobbyIntent();
            JoinAnytimeLobbyController.RefreshLobbyState(force: true);
        }
    }

    [HarmonyPatch(typeof(DungeonRoom), "OnAllMemberEntered")]
    internal static class DungeonRoomOnAllMemberEnteredLobbyPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            JoinAnytimeLobbyController.ApplyHostPublicLobbyIntent();
            JoinAnytimeLobbyController.RefreshLobbyState(force: true);
        }
    }

    [HarmonyPatch(typeof(VRoomManager), "BroadcastRoomReady")]
    internal static class VRoomManagerBroadcastRoomReadyPatch
    {
        [HarmonyPrefix]
        private static void Prefix(VRoomManager __instance, VRoomType roomType)
        {
            if (!ModConfig.EnableJoinAnytime.Value || roomType != VRoomType.Waiting)
            {
                return;
            }

            JoinAnytimeRoomTools.PrepareWaitingRoomBeforeBroadcast(__instance);
        }
    }

    [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.InitWaitingRoom))]
    internal static class VRoomManagerInitWaitingRoomPatch
    {
        [HarmonyPostfix]
        private static void Postfix(VRoomManager __instance)
        {
            JoinAnytimeRoomTools.RefreshWaitingRoomDisplaysForOccupants(__instance);
            JoinAnytimeLobbyController.ScheduleDeferredLobbyRefresh();
        }
    }

    [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.OnDungeonFinished))]
    internal static class VRoomManagerOnDungeonFinishedPatch
    {
        [HarmonyPostfix]
        private static void Postfix(bool prevDungeonSuccess)
        {
            if (!prevDungeonSuccess)
            {
                return;
            }

            JoinAnytimeRoomTools.PrepareWaitingRoomAfterDungeonSuccess();
        }
    }

    [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.EnterWaitingRoom))]
    internal static class VRoomManagerEnterWaitingRoomPatch
    {
        [HarmonyPrefix]
        private static void Prefix(SessionContext context)
        {
            JoinAnytimeRoomTools.EnsureWaitingRoomEnterReady();
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

            WaitingRoomBlockReason reason = JoinAnytimeRoomTools.GetWaitingRoomBlockReason();
            if (reason == WaitingRoomBlockReason.None)
            {
                return true;
            }

            int actorId = GameActionParamHelper.FindParam<GameActionParamActor>(paramList)?.ActorID ?? 0;
            ModLog.Info("JoinAnytime", $"Blocked tram lever — reason={reason}");
            JoinAnytimeTramLeverTools.TryResetTramDepartureLever(waitingRoom, actorId);
            JoinAnytimeUserMessages.OnWaitingRoomStartBlocked(__instance, actorId);
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(IVroom), nameof(IVroom.HandleLevelObject))]
    internal static class IVroomHandleLevelObjectTramLeverPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(
            IVroom __instance,
            int actorID,
            int levelObjectID,
            int state,
            bool occupy,
            out int prevState,
            ref MsgErrorCode __result)
        {
            prevState = -1;

            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return true;
            }

            if (!JoinAnytimeTramLeverTools.TryGetLevelObject(__instance, levelObjectID, out ILevelObjectInfo? levelObject)
                || levelObject == null)
            {
                return true;
            }

            if (!JoinAnytimeTramLeverTools.ShouldBlockDepartureLeverUse(__instance, levelObject, state))
            {
                return true;
            }

            if (levelObject is StateLevelObjectInfo stateInfo)
            {
                prevState = stateInfo.CurrentState;
            }

            ModLog.Info(
                "JoinAnytime",
                $"Blocked tram lever state change to {state} — reason={JoinAnytimeRoomTools.GetWaitingRoomBlockReason()}");

            JoinAnytimeUserMessages.OnWaitingRoomStartBlocked(__instance, actorID);
            __result = MsgErrorCode.CantAction;
            return false;
        }
    }

    [HarmonyPatch]
    internal static class NewTramLeverLevelObjectIsTriggerablePatch
    {
        private static MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(NewTramLeverLevelObject), "IsTriggerable", [typeof(ProtoActor), typeof(int)]);

        [HarmonyPostfix]
        private static void Postfix(ref bool __result)
        {
            if (!__result || !ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            if (JoinAnytimeHub.GetPdata()?.main is not InTramWaitingScene)
            {
                return;
            }

            if (!JoinAnytimeRoomTools.ShouldBlockWaitingRoomStartGame())
            {
                return;
            }

            __result = false;
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

    [HarmonyPatch(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.CreateLobby), typeof(bool), typeof(bool))]
    internal static class SteamInviteDispatcherCreateLobbyPatch
    {
        [HarmonyPostfix]
        private static void Postfix(SteamInviteDispatcher __instance, bool isOpenForRandomMatch, bool isRetryAttempt)
        {
            JoinAnytimeLobbyController.OnLobbyCreated(__instance, isOpenForRandomMatch, isRetryAttempt);
        }
    }

    [HarmonyPatch(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.SetLobbyPublic))]
    internal static class SteamInviteDispatcherSetLobbyPublicPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(bool isPublic)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return true;
            }

            if (isPublic)
            {
                JoinAnytimeLobbyController.SetHostWantsPublicMatchmaking(true);
            }
            else if (JoinAnytimeLobbyController.ShouldBlockPublicRoomClose())
            {
                ModLog.Debug("JoinAnytime", "Blocked SetLobbyPublic(false) for join-anytime host.");
                return false;
            }

            return true;
        }

        [HarmonyPostfix]
        private static void Postfix(SteamInviteDispatcher __instance, bool isPublic)
        {
            JoinAnytimeLobbyController.OnSetLobbyPublicCompleted(__instance, isPublic);
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
                JoinAnytimeLobbyController.ApplyLobbyPresence(__instance, wantsPublic: true);
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.SetPresencePlaying))]
    internal static class SteamInviteDispatcherSetPresencePlayingPatch
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
                JoinAnytimeLobbyController.ApplyLobbyPresence(__instance, wantsPublic: true);
                JoinAnytimeLobbyController.RefreshLobbyState(force: true);
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.UpdateLobbyData))]
    internal static class SteamInviteDispatcherUpdateLobbyDataPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(SteamInviteDispatcher __instance, string key, string value)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return true;
            }

            if (string.Equals(key, SteamInviteDispatcher.IS_PUBLIC_KEY, StringComparison.Ordinal)
                && string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                && JoinAnytimeHub.IsHost())
            {
                JoinAnytimeLobbyController.SetHostWantsPublicMatchmaking(true);
            }

            if (string.Equals(key, SteamInviteDispatcher.IS_PUBLIC_KEY, StringComparison.Ordinal)
                && string.Equals(value, "false", StringComparison.OrdinalIgnoreCase)
                && JoinAnytimeLobbyController.ShouldBlockPublicRoomClose())
            {
                ModLog.Debug("JoinAnytime", "Blocked PublicRoom=false lobby data update for join-anytime host.");
                JoinAnytimeLobbyController.ApplyHostPublicLobbyIntent();
                return false;
            }

            return true;
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
            JoinAnytimeConnectingTracker.OnServerPlayerCreated(__instance);
        }
    }

    [HarmonyPatch(typeof(VPlayer), nameof(VPlayer.HandleLevelLoadComplete))]
    internal static class VPlayerHandleLevelLoadCompletePatch
    {
        [HarmonyPostfix]
        private static void Postfix(VPlayer __instance)
        {
            JoinAnytimeConnectingTracker.OnLevelLoadCompleted(__instance);
            LateJoinManager.OnLevelLoadCompleted(__instance);
        }
    }

    [HarmonyPatch(typeof(UIPrefab_InGameMenu), "Start")]
    internal static class UIPrefabInGameMenuStartJoinAnytimePatch
    {
        [HarmonyPostfix]
        private static void Postfix(UIPrefab_InGameMenu __instance)
        {
            JoinAnytimeInGameMenuTools.OnMenuStart(__instance);
        }
    }

    [HarmonyPatch(typeof(UIPrefab_InGameMenu), "OnEnable")]
    internal static class UIPrefabInGameMenuOnEnableJoinAnytimePatch
    {
        [HarmonyPostfix]
        private static void Postfix(UIPrefab_InGameMenu __instance)
        {
            JoinAnytimeInGameMenuTools.EnsurePublicRoomControlsAccessible(__instance);
        }
    }

    [HarmonyPatch]
    internal static class UIPrefabInGameMenuSetPublicRoomNamePatch
    {
        private static MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(UIPrefab_InGameMenu), "SetPublicRoomName");

        [HarmonyPostfix]
        private static void Postfix(UIPrefab_InGameMenu __instance)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            JoinAnytimeLobbyController.OnPublicRoomNameChanged(__instance, __instance.lobbyName);
        }
    }

    [HarmonyPatch]
    internal static class UIPrefabPublicRoomListSetRoomListPatch
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo? RoomListDataField =
            typeof(UIPrefab_PublicRoomList).GetField("roomListData", InstanceFlags);

        private static readonly MethodInfo? SetRoomListUiMethod =
            typeof(UIPrefab_PublicRoomList).GetMethod("SetRoomListUI", InstanceFlags);

        private static MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(UIPrefab_PublicRoomList), "SetRoomList");

        [HarmonyPostfix]
        private static void Postfix(UIPrefab_PublicRoomList __instance, List<CSteamID> lobbyIDs)
        {
            if (!ModConfig.EnableJoinAnytime.Value || lobbyIDs == null || lobbyIDs.Count == 0)
            {
                return;
            }

            FieldInfo? roomListDataField = RoomListDataField;
            if (roomListDataField?.GetValue(__instance) is not List<PublicRoomListData> roomListData)
            {
                return;
            }

            HashSet<CSteamID> existing = roomListData
                .Select(entry => entry.lobbyID)
                .ToHashSet();

            bool added = false;
            foreach (CSteamID lobbyId in lobbyIDs)
            {
                int playerCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
                if (playerCount < JoinAnytimeLobbyDisplay.VanillaBrowseDenominator || existing.Contains(lobbyId))
                {
                    continue;
                }

                if (!JoinAnytimeLobbyDisplay.ShouldIncludeInPublicList(playerCount, lobbyId))
                {
                    continue;
                }

                roomListData.Add(JoinAnytimeLobbyDisplay.CreatePublicRoomListData(lobbyId, playerCount));
                _ = existing.Add(lobbyId);
                added = true;
            }

            if (!added)
            {
                return;
            }

            SetRoomListUiMethod?.Invoke(__instance, null);
        }
    }

    [HarmonyPatch]
    internal static class UiPrefabRoomCardSetRoomDataPatch
    {
        private static MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(UiPrefab_RoomCard), "SetRoomData");

        [HarmonyPostfix]
        private static void Postfix(PublicRoomListData data, UiPrefab_RoomCard __instance)
        {
            JoinAnytimeLobbyDisplay.ApplyBrowsePlayerCountToRoomCard(data, __instance);
        }
    }

    [HarmonyPatch(typeof(MaintenanceScene), "TryInitHostMaintenenceRoom")]
    internal static class MaintenanceSceneTryInitHostMaintenenceRoomPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            JoinAnytimeLobbyController.OnHostSceneReady();
        }
    }

    [HarmonyPatch]
    internal static class InTramWaitingSceneStartPatch
    {
        private static MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(InTramWaitingScene), "Start");

        [HarmonyPostfix]
        private static void Postfix()
        {
            JoinAnytimeLobbyController.OnHostSceneReady();
            JoinAnytimeLobbyController.ApplyHostPublicLobbyIntent();
        }
    }

}
