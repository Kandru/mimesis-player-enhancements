namespace MimesisPlayerEnhancement.Features.JoinAnytime.Patches
{
    // game@0.3.1 Assembly-CSharp/VRoomManager.cs:L474-508
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

    // game@0.3.1 Assembly-CSharp/VRoomManager.cs:L681-691
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

    // game@0.3.1 Assembly-CSharp/VRoomManager.cs:L114-140
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

    // game@0.3.1 Assembly-CSharp/VRoomManager.cs:L583-600
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

    // game@0.3.1 Assembly-CSharp/VRoomManager.cs:L372-409
    [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.EnterWaitingRoom))]
    internal static class VRoomManagerEnterWaitingRoomPatch
    {
        [HarmonyPrefix]
        private static void Prefix(SessionContext context)
        {
            JoinAnytimeRoomTools.EnsureWaitingRoomEnterReady();
            LateJoinManager.OnServerEnterWaitingRoom(context);
        }

        [HarmonyPostfix]
        private static void Postfix(SessionContext context)
        {
            if (!ModConfig.EnableJoinAnytime.Value || context == null)
            {
                return;
            }

            VPlayer? player = SessionContextAccess.GetVPlayer(context);
            if (player != null)
            {
                JoinAnytimeConnectingTracker.TryPromoteIfReady(player);
            }

            if (WebDashboardServer.IsRunning)
            {
                WebDashboardSnapshotCache.MarkDirty();
            }
        }
    }

    // game@0.3.1 Assembly-CSharp/VRoomManager.cs:L332-370
    [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.EnterMaintenenceRoom))]
    internal static class VRoomManagerEnterMaintenenceRoomPatch
    {
        [HarmonyPrefix]
        private static void Prefix(SessionContext context, int hashCode)
        {
            LateJoinManager.OnServerEnterMaintenance(context);
        }
    }
}
