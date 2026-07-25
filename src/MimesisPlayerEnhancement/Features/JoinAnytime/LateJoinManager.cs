using UnityEngine;

namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    /// <summary>
    /// Server-only late join: route joiners through vanilla maintenance -> tram using stock packets.
    /// Joiners wait in the waiting room until active players return from the dungeon.
    /// </summary>
    internal static class LateJoinManager
    {
        private const string Feature = "JoinAnytime";

        private static float _nextTramRouteRetryTime;

        internal static bool IsEnabled => ModConfig.EnableJoinAnytime.Value;

        private const float RouteRetryIntervalSeconds = 0.5f;

        /// <summary>Clears routing state so stale UIDs cannot leak across sessions or feature toggles.</summary>
        internal static void Reset()
        {
            _nextTramRouteRetryTime = 0f;
            LateJoinRouteTracker.Reset();
        }

        internal static void OnPlayerRegistered(long uid) => LateJoinRouteTracker.OnPlayerRegistered(uid);

        internal static void OnPlayerDisconnected(long uid) => LateJoinRouteTracker.OnPlayerDisconnected(uid);

        internal static void OnHostSceneReady()
        {
            if (!IsEnabled || !ShouldRouteToTram())
            {
                return;
            }

            RouteAllMaintenanceLateJoiners();
        }

        /// <summary>
        /// Called after maintenance SyncEnterRoom — AllMemberEnterRoomSig is already queued for the client.
        /// </summary>
        internal static void OnMaintenanceAllMembersEntered(IVroom room)
        {
            if (!IsEnabled || !ShouldRouteToTram() || room is not MaintenanceRoom)
            {
                return;
            }

            room.IterateAllPlayer(player =>
            {
                if (player.IsHost || !player.LevelLoadCompleted)
                {
                    return;
                }

                TryRoutePlayer(player, logFirstAttempt: true);
            });
        }

        internal static void OnUpdate()
        {
            if (!IsEnabled)
            {
                return;
            }

            if (Time.time < _nextTramRouteRetryTime)
            {
                return;
            }

            _nextTramRouteRetryTime = Time.time + RouteRetryIntervalSeconds;

            if (ShouldRouteToTram())
            {
                RetryStuckRoutes();
            }
            else
            {
                SyncMaintenanceLobbyPlayers();
            }
        }

        internal static void OnServerEnterWaitingRoom(SessionContext context)
        {
            if (!IsEnabled || context == null || !context.ExistPlayer())
            {
                return;
            }

            if (context.GetVRoomType() != VRoomType.Maintenance)
            {
                return;
            }

            Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
            if (pdata?.main is not InTramWaitingScene and not GamePlayScene)
            {
                return;
            }

            ModLog.Debug(Feature, "Moving player snapshot Maintenance -> Waiting");
            JoinAnytimeRoomTools.MoveCurrentPlayerToSnapshot(context);
        }

        internal static void OnServerEnterMaintenance(SessionContext context)
        {
            if (!IsEnabled || context == null || !context.ExistPlayer())
            {
                return;
            }

            Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
            if (context.GetVRoomType() == VRoomType.Game
                && pdata?.main is MaintenanceScene)
            {
                ModLog.Debug(Feature, "Moving player snapshot Dungeon -> Maintenance");
                JoinAnytimeRoomTools.MoveCurrentPlayerToSnapshot(context);
            }
        }

        private static void TryRoutePlayer(VPlayer player, bool logFirstAttempt = false)
        {
            if (!IsEnabled || player == null || player.IsHost || !ShouldRouteToTram())
            {
                return;
            }

            LateJoinRouteTracker.SyncFromLivePlayer(player);

            if (player.VRoom is VWaitingRoom)
            {
                LateJoinRouteTracker.MarkInWaitingRoom(player.UID);
                return;
            }

            if (player.VRoom is not MaintenanceRoom || !player.LevelLoadCompleted)
            {
                return;
            }

            // After ReleaseLateJoinerFromMaintenance the server waits for EnterWaitingRoomReq.
            // Resending MakeRoomComplete/MoveToWaitingRoom/LeaveRoom resets the client transition.
            if (LateJoinRouteTracker.GetPhase(player.UID) == LateJoinRoutePhase.AwaitingClient)
            {
                return;
            }

            if (!LateJoinRouteTracker.CanAttempt(player.UID, RouteRetryIntervalSeconds))
            {
                return;
            }

            if (logFirstAttempt)
            {
                ModLog.Info(
                    Feature,
                    $"Late joiner in maintenance — uid={player.UID} hostScene={GameSessionAccess.TryGetPdata()?.main?.GetType().Name ?? "null"}");
            }
            else if (LateJoinRouteTracker.GetStuckSeconds(player.UID) > 0f)
            {
                ModLog.Info(
                    Feature,
                    $"Late joiner route retry — uid={player.UID} stuckFor={LateJoinRouteTracker.GetStuckSeconds(player.UID):F1}s attempts={LateJoinRouteTracker.GetAttemptCount(player.UID)}");
            }

            LateJoinRouteTracker.RecordAttempt(player.UID);
            JoinAnytimeNetworkTools.RouteToTram(player);
        }

        private static void RouteAllMaintenanceLateJoiners()
        {
            SessionManager? sessionManager = SessionContextAccess.GetSessionManager();
            if (sessionManager == null)
            {
                return;
            }

            foreach (SessionContext context in SessionContextAccess.EnumerateSessionContexts(sessionManager))
            {
                VPlayer? player = SessionContextAccess.GetVPlayer(context);
                if (player == null || player.IsHost || !player.LevelLoadCompleted)
                {
                    continue;
                }

                TryRoutePlayer(player);
            }
        }

        private static void RetryStuckRoutes()
        {
            SessionManager? sessionManager = SessionContextAccess.GetSessionManager();
            if (sessionManager == null)
            {
                return;
            }

            foreach (SessionContext context in SessionContextAccess.EnumerateSessionContexts(sessionManager))
            {
                VPlayer? player = SessionContextAccess.GetVPlayer(context);
                if (player == null || player.IsHost)
                {
                    // No VPlayer after maintenance release until EnterWaitingRoomReq — wait.
                    continue;
                }

                if (player.VRoom is VWaitingRoom)
                {
                    LateJoinRouteTracker.MarkInWaitingRoom(player.UID);
                    continue;
                }

                if (player.VRoom is MaintenanceRoom && player.LevelLoadCompleted)
                {
                    TryRoutePlayer(player);
                }
            }
        }

        private static void SyncMaintenanceLobbyPlayers()
        {
            SessionManager? sessionManager = SessionContextAccess.GetSessionManager();
            if (sessionManager == null)
            {
                return;
            }

            foreach (SessionContext context in SessionContextAccess.EnumerateSessionContexts(sessionManager))
            {
                VPlayer? player = SessionContextAccess.GetVPlayer(context);
                if (player == null || player.IsHost)
                {
                    continue;
                }

                LateJoinRouteTracker.SyncFromLivePlayer(player);
            }
        }

        private static bool ShouldRouteToTram() => JoinAnytimeRoomTools.ShouldRouteToTram();
    }
}
