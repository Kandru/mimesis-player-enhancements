using ReluNetwork.ConstEnum;
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

        internal static void OnLevelLoadCompleted(VPlayer player) =>
            TryRoutePlayer(player, allowResend: false, logFirstAttempt: true);

        internal static void OnHostSceneReady()
        {
            if (!IsEnabled || !ShouldRouteToTram())
            {
                return;
            }

            RouteAllMaintenanceLateJoiners(allowResend: true);
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

            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
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

            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
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

            if (LateJoinRouteTracker.GetPhase(player.UID) == LateJoinRoutePhase.AwaitingClient)
            {
                return;
            }

            if (!LateJoinRouteTracker.CanAttempt(player.UID, RouteRetryIntervalSeconds))
            {
                return;
            }

            bool resend = allowResend || LateJoinRouteTracker.IsRoutePending(player.UID);

            if (logFirstAttempt)
            {
                ModLog.Info(
                    Feature,
                    $"Late joiner in maintenance — uid={player.UID} hostScene={JoinAnytimeHub.GetPdata()?.main?.GetType().Name ?? "null"}");
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
            SessionManager? sessionManager = WebDashboardSessionAccess.GetSessionManager();
            if (sessionManager == null)
            {
                return;
            }

            foreach (SessionContext context in WebDashboardSessionAccess.EnumerateSessionContexts(sessionManager))
            {
                VPlayer? player = WebDashboardSessionAccess.GetVPlayer(context);
                if (player == null || player.IsHost || !player.LevelLoadCompleted)
                {
                    continue;
                }

                TryRoutePlayer(player, allowResend);
            }
        }

        private static void RetryStuckRoutes()
        {
            foreach (long uid in LateJoinRouteTracker.GetActiveRouteUids())
            {
                LateJoinRoutePhase phase = LateJoinRouteTracker.GetPhase(uid);

                if (WebDashboardSessionAccess.TryGetPlayerByUid(uid, out VPlayer? player) && player != null)
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

                    if (phase == LateJoinRoutePhase.AwaitingClient)
                    {
                        continue;
                    }

                    if (player.VRoom is MaintenanceRoom && player.LevelLoadCompleted)
                    {
                        TryRoutePlayer(player, allowResend: true);
                    }

                    continue;
                }

                if (phase != LateJoinRoutePhase.AwaitingClient)
                {
                    continue;
                }

                if (!LateJoinRouteTracker.CanAttempt(uid, RouteRetryIntervalSeconds))
                {
                    continue;
                }

                if (!WebDashboardSessionAccess.TryGetSessionContextByUid(uid, out SessionContext? context)
                    || context == null
                    || context.ExistPlayer())
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
            SessionManager? sessionManager = WebDashboardSessionAccess.GetSessionManager();
            if (sessionManager == null)
            {
                return;
            }

            foreach (SessionContext context in WebDashboardSessionAccess.EnumerateSessionContexts(sessionManager))
            {
                VPlayer? player = WebDashboardSessionAccess.GetVPlayer(context);
                if (player == null || player.IsHost)
                {
                    continue;
                }

                LateJoinRouteTracker.SyncFromLivePlayer(player);
            }
        }

        private static bool ShouldRouteToTram()
        {
            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
            return pdata?.ClientMode == NetworkClientMode.Host
                && pdata.main is InTramWaitingScene or GamePlayScene;
        }
    }
}
