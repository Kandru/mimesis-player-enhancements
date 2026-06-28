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
                ("OnRecvPacket/NetworkManagerV2", AccessTools.Method(typeof(NetworkManagerV2), nameof(NetworkManagerV2.OnRecvPacket), [typeof(IMsg)])),
                ("LoadScene/Hub", AccessTools.Method(typeof(Hub), nameof(Hub.LoadScene), [typeof(string)])),
                ("Start/MaintenanceScene", AccessTools.Method(typeof(MaintenanceScene), "Start")),
                ("EnterWaitingRoom/VRoomManager", AccessTools.Method(typeof(VRoomManager), nameof(VRoomManager.EnterWaitingRoom))),
                ("EnterDungeon/VRoomManager", AccessTools.Method(typeof(VRoomManager), nameof(VRoomManager.EnterDungeon))),
                ("EnterMaintenenceRoom/VRoomManager", AccessTools.Method(typeof(VRoomManager), nameof(VRoomManager.EnterMaintenenceRoom))),
                ("SetVoiceMode/VoiceManager", AccessTools.Method(typeof(VoiceManager), nameof(VoiceManager.SetVoiceMode))),
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
