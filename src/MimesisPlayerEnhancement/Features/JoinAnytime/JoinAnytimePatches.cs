using System;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;
using ReluProtocol;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    public static class JoinAnytimePatches
    {
        private const string Feature = "JoinAnytime";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(JoinAnytimePatches)));

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            Type[] corRefreshParams = { typeof(Action<bool>) };

            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("CanEnterSession/GameSessionInfo", AccessTools.Method(typeof(GameSessionInfo), nameof(GameSessionInfo.CanEnterSession))),
                ("Login/SessionContext", AccessTools.Method(typeof(SessionContext), nameof(SessionContext.Login))),
                ("EnterWaitingRoom/VRoomManager", AccessTools.Method(typeof(VRoomManager), nameof(VRoomManager.EnterWaitingRoom))),
                ("EnterMaintenenceRoom/VRoomManager", AccessTools.Method(typeof(VRoomManager), nameof(VRoomManager.EnterMaintenenceRoom))),
                ("PendMoveToDungeon/VRoomManager", AccessTools.Method(typeof(VRoomManager), nameof(VRoomManager.PendMoveToDungeon))),
                ("OnAllMemberEntered/DungeonRoom", AccessTools.Method(typeof(DungeonRoom), "OnAllMemberEntered")),
                ("RunEventActionInternal/IVroom", AccessTools.Method(typeof(IVroom), "RunEventActionInternal")),
                ("OnChangeLevelObjectStateSig/NewTramLeverLevelObject", AccessTools.Method(typeof(NewTramLeverLevelObject), nameof(NewTramLeverLevelObject.OnChangeLevelObjectStateSig))),
                ("CreateLobby/SteamInviteDispatcher", AccessTools.Method(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.CreateLobby))),
                ("SetLobbyPublic/SteamInviteDispatcher", AccessTools.Method(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.SetLobbyPublic))),
                ("SetPresenceInLobby/SteamInviteDispatcher", AccessTools.Method(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.SetPresenceInLobby))),
                ("SetPresencePlaying/SteamInviteDispatcher", AccessTools.Method(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.SetPresencePlaying))),
                ("UpdateLobbyData/SteamInviteDispatcher", AccessTools.Method(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.UpdateLobbyData))),
                ("HandleLevelLoadComplete/VPlayer", AccessTools.Method(typeof(VPlayer), nameof(VPlayer.HandleLevelLoadComplete))),
                ("SetPublicRoomName/UIPrefab_InGameMenu", AccessTools.Method(typeof(UIPrefab_InGameMenu), "SetPublicRoomName")),
                ("SetRoomList/UIPrefab_PublicRoomList", AccessTools.Method(typeof(UIPrefab_PublicRoomList), "SetRoomList")),
                ("SetRoomData/UiPrefab_RoomCard", AccessTools.Method(typeof(UiPrefab_RoomCard), "SetRoomData")),
                ("TryInitHostMaintenenceRoom/MaintenanceScene", AccessTools.Method(typeof(MaintenanceScene), "TryInitHostMaintenenceRoom")),
                ("Start/InTramWaitingScene", AccessTools.Method(typeof(InTramWaitingScene), "Start")),
                ("Start/GamePlayScene", AccessTools.Method(typeof(GamePlayScene), "Start")),
                ("CorRefreshSteamLobbyData/GameMainBase", AccessTools.Method(typeof(GameMainBase), "CorRefreshSteamLobbyData", corRefreshParams)),
                (".ctor/VPlayer", AccessTools.Constructor(
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
                    ])),
            ]);
        }
    }
}
