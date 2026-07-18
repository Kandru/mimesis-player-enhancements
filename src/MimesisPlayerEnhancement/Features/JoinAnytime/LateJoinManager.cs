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

            RouteAllMaintenanceLateJoiners(allowResend: true);
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

                TryRoutePlayer(player, allowResend: false, logFirstAttempt: true);
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

        private static void TryRoutePlayer(VPlayer player, bool allowResend, bool logFirstAttempt = false)
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

            LateJoinRoutePhase phase = LateJoinRouteTracker.GetPhase(player.UID);
            if (phase == LateJoinRoutePhase.AwaitingClient && !allowResend)
            {
                return;
            }

            if (!LateJoinRouteTracker.CanAttempt(player.UID, RouteRetryIntervalSeconds))
            {
                return;
            }

            bool resend = allowResend
                || LateJoinRouteTracker.IsRoutePending(player.UID)
                || phase == LateJoinRoutePhase.AwaitingClient;

            if (logFirstAttempt && phase != LateJoinRoutePhase.AwaitingClient)
            {
                ModLog.Info(
                    Feature,
                    $"Late joiner in maintenance — uid={player.UID} hostScene={GameSessionAccess.TryGetPdata()?.main?.GetType().Name ?? "null"}");
            }
            else if (LateJoinRouteTracker.GetStuckSeconds(player.UID) > 0f && resend)
            {
                ModLog.Info(
                    Feature,
                    $"Late joiner route retry — uid={player.UID} stuckFor={LateJoinRouteTracker.GetStuckSeconds(player.UID):F1}s attempts={LateJoinRouteTracker.GetAttemptCount(player.UID)}");
            }

            LateJoinRouteTracker.RecordAttempt(player.UID);
            JoinAnytimeNetworkTools.RouteToTram(player, resend);
        }

        private static void RouteAllMaintenanceLateJoiners(bool allowResend)
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

                TryRoutePlayer(player, allowResend);
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
                long uid = context.GetPlayerUID();
                if (uid == 0)
                {
                    continue;
                }

                VPlayer? player = SessionContextAccess.GetVPlayer(context);
                if (player != null)
                {
                    if (player.IsHost)
                    {
                        continue;
                    }

                    if (player.VRoom is VWaitingRoom)
                    {
                        LateJoinRouteTracker.MarkInWaitingRoom(uid);
                        continue;
                    }

                    if (player.VRoom is MaintenanceRoom && player.LevelLoadCompleted)
                    {
                        TryRoutePlayer(player, allowResend: true);
                    }

                    continue;
                }

                if (LateJoinRouteTracker.GetPhase(uid) != LateJoinRoutePhase.AwaitingClient)
                {
                    continue;
                }

                if (!LateJoinRouteTracker.CanAttempt(uid, RouteRetryIntervalSeconds))
                {
                    continue;
                }

                if (LateJoinRouteTracker.GetStuckSeconds(uid) > 0f)
                {
                    ModLog.Info(
                        Feature,
                        $"Late joiner limbo retry — uid={uid} stuckFor={LateJoinRouteTracker.GetStuckSeconds(uid):F1}s attempts={LateJoinRouteTracker.GetAttemptCount(uid)}");
                }

                LateJoinRouteTracker.RecordAttempt(uid);
                JoinAnytimeNetworkTools.RouteToTram(context, allowResend: true);
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
