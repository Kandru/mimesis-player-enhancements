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
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("CanEnterSession/GameSessionInfo", AccessTools.Method(typeof(GameSessionInfo), nameof(GameSessionInfo.CanEnterSession))),
                ("Login/SessionContext", AccessTools.Method(typeof(SessionContext), nameof(SessionContext.Login))),
                ("EnterWaitingRoom/VRoomManager", AccessTools.Method(typeof(VRoomManager), nameof(VRoomManager.EnterWaitingRoom))),
                ("EnterMaintenenceRoom/VRoomManager", AccessTools.Method(typeof(VRoomManager), nameof(VRoomManager.EnterMaintenenceRoom))),
                ("OnRequestStartGame/VWaitingRoom", AccessTools.Method(typeof(IVroom), "RunEventActionInternal")),
                ("OnChangeLevelObjectStateSig/NewTramLeverLevelObject", AccessTools.Method(typeof(NewTramLeverLevelObject), nameof(NewTramLeverLevelObject.OnChangeLevelObjectStateSig))),
                ("SetLobbyPublic/SteamInviteDispatcher", AccessTools.Method(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.SetLobbyPublic))),
                ("CreateLobby/SteamInviteDispatcher", AccessTools.Method(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.CreateLobby))),
                ("SetPresenceInLobby/SteamInviteDispatcher", AccessTools.Method(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.SetPresenceInLobby))),
                ("CorRefreshSteamLobbyData/GameMainBase", AccessTools.Method(typeof(GameMainBase), "CorRefreshSteamLobbyData", [typeof(Action<bool>)])),
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
