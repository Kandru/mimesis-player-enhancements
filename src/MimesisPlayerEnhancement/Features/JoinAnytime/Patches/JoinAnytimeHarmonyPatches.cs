using System;
using System.Reflection;
using HarmonyLib;
using MimesisPlayerEnhancement.Features.JoinAnytime;
using ReluProtocol;
using ReluProtocol.C2S;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.JoinAnytime.Patches;

[HarmonyPatch(typeof(GameSessionInfo), nameof(GameSessionInfo.CanEnterSession))]
internal static class GameSessionInfoCanEnterSessionPatch
{
    [HarmonyPrefix]
    private static bool Prefix(ref bool __result)
    {
        if (!ModConfig.EnableJoinAnytime.Value)
            return true;

        __result = true;
        return false;
    }
}

[HarmonyPatch(typeof(SessionContext), nameof(SessionContext.Login))]
internal static class SessionContextLoginPatch
{
    [HarmonyPostfix]
    private static void Postfix(SessionContext __instance) =>
        LateJoinManager.OnServerLogin(__instance);
}

[HarmonyPatch(typeof(NetworkManagerV2), nameof(NetworkManagerV2.OnRecvPacket), typeof(IMsg))]
internal static class NetworkManagerV2OnRecvPacketPatch
{
    [HarmonyPrefix]
    private static bool Prefix(IMsg msg) => LateJoinManager.OnClientPacket(msg);
}

[HarmonyPatch(typeof(Hub), nameof(Hub.LoadScene), typeof(string))]
internal static class HubLoadScenePatch
{
    [HarmonyPrefix]
    private static void Prefix(string sceneName) => LateJoinManager.OnLoadScene(sceneName);
}

[HarmonyPatch(typeof(MaintenanceScene), "Start")]
internal static class MaintenanceSceneStartPatch
{
    [HarmonyPostfix]
    private static void Postfix() => LateJoinManager.OnMaintenanceSceneStart();
}

[HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.EnterWaitingRoom))]
internal static class VRoomManagerEnterWaitingRoomPatch
{
    [HarmonyPrefix]
    private static void Prefix(SessionContext context) =>
        LateJoinManager.OnServerEnterWaitingRoom(context);
}

[HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.EnterDungeon))]
internal static class VRoomManagerEnterDungeonPatch
{
    [HarmonyPrefix]
    private static void Prefix(SessionContext context, int hashCode, long roomUID) =>
        LateJoinManager.OnServerEnterDungeon(context, roomUID);
}

[HarmonyPatch(typeof(VoiceManager), nameof(VoiceManager.SetVoiceMode))]
internal static class VoiceManagerSetVoiceModePatch
{
    [HarmonyPostfix]
    private static void Postfix(VoiceManager __instance, VoiceMode voiceMode) =>
        LateJoinManager.EnsureVoiceConnected(__instance, voiceMode);
}

[HarmonyPatch(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.SetLobbyPublic))]
internal static class SteamInviteDispatcherSetLobbyPublicPatch
{
    [HarmonyPrefix]
    private static void Prefix(ref bool isPublic)
    {
        if (!ModConfig.EnableJoinAnytime.Value)
            return;

        isPublic = true;
    }
}

[HarmonyPatch(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.SetPresenceInLobby))]
internal static class SteamInviteDispatcherSetPresenceInLobbyPatch
{
    [HarmonyPrefix]
    private static bool Prefix(SteamInviteDispatcher __instance)
    {
        if (!ModConfig.EnableJoinAnytime.Value)
            return true;

        __instance.SetPresenceInLobbyPublic();
        return false;
    }
}

[HarmonyPatch]
internal static class GameMainBaseCorRefreshSteamLobbyDataPatch
{
    private static MethodBase? TargetMethod() =>
        AccessTools.Method(typeof(GameMainBase), "CorRefreshSteamLobbyData", new[] { typeof(Action<bool>) });

    [HarmonyPostfix]
    private static void Postfix() => LateJoinManager.KeepLobbyOpen();
}

[HarmonyPatch]
internal static class VPlayerCtorPatch
{
    private static MethodBase? TargetMethod() =>
        AccessTools.Constructor(
            typeof(VPlayer),
            new[]
            {
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
            });

    [HarmonyPostfix]
    private static void Postfix(VPlayer __instance) =>
        LateJoinManager.OnServerPlayerCreated(__instance);
}
