using HarmonyLib;
using ReluProtocol;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.JoinAnytime.Patches
{
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
        private static System.Reflection.MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(InTramWaitingScene), "Start");

        [HarmonyPostfix]
        private static void Postfix()
        {
            JoinAnytimeLobbyController.OnHostSceneReady();
            JoinAnytimeLobbyController.ApplyHostPublicLobbyIntent();
        }
    }
}
