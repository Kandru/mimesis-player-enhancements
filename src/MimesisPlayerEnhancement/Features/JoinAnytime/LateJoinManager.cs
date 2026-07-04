using MimesisPlayerEnhancement.Features.WebDashboard;
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

        private static readonly HashSet<long> PendingTramRouteUids = [];

        private static float _nextTramRouteRetryTime;

        internal static bool IsEnabled => ModConfig.EnableJoinAnytime.Value;

        internal static float RouteRetryIntervalSeconds =>
            Mathf.Max(0.1f, ModConfig.JoinTramRouteRetrySeconds.Value);

        /// <summary>Clears routing state so stale UIDs cannot leak across sessions or feature toggles.</summary>
        internal static void Reset()
        {
            PendingTramRouteUids.Clear();
            _nextTramRouteRetryTime = 0f;
            LateJoinRouteTracker.Reset();
        }

        internal static void OnPlayerRegistered(long uid) => LateJoinRouteTracker.OnPlayerRegistered(uid);

        internal static void OnPlayerDisconnected(long uid) => LateJoinRouteTracker.OnPlayerDisconnected(uid);

        internal static void OnLevelLoadCompleted(VPlayer player)
        {
            AttemptTramRoute(player, allowResend: false, TramRouteAttemptReason.LevelLoadComplete);
        }

        internal static void OnHostSceneReady()
        {
            if (!IsEnabled || !ShouldRouteToTram())
            {
                return;
            }

            RouteAllMaintenanceLateJoiners(allowResend: true, TramRouteAttemptReason.HostSceneReady);
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
                RetryPendingTramRoutes();
                SweepMaintenanceLateJoiners();
                ResendAwaitingClientRoutes();
            }
            else
            {
                SyncWaitingHostPhasePlayers();
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

        internal static bool HasPreGameStateBeenSent(long uid) =>
            LateJoinRouteTracker.HasPreGameStateBeenSent(uid);

        internal static void MarkPreGameStateSent(long uid)
        {
            LateJoinRouteTracker.MarkInWaitingRoom(uid);
            _ = PendingTramRouteUids.Remove(uid);
        }

        private static void AttemptTramRoute(
            VPlayer player,
            bool allowResend,
            TramRouteAttemptReason reason)
        {
            if (!IsEnabled || player == null || player.IsHost)
            {
                return;
            }

            AttemptTramRoute(player.UID, player, allowResend, reason);
        }

        private static void AttemptTramRoute(
            long uid,
            VPlayer? player,
            bool allowResend,
            TramRouteAttemptReason reason)
        {
            if (!IsEnabled || uid == 0)
            {
                return;
            }

            if (player != null)
            {
                LateJoinRouteTracker.SyncFromLivePlayer(player);

                if (player.VRoom is VWaitingRoom)
                {
                    MarkPreGameStateSent(uid);
                    return;
                }

                if (player.VRoom is not MaintenanceRoom)
                {
                    return;
                }

                if (!player.LevelLoadCompleted)
                {
                    return;
                }
            }
            else if (!WebDashboardSessionAccess.TryGetSessionContextByUid(uid, out SessionContext? context)
                || context == null)
            {
                return;
            }
            else if (LateJoinRouteTracker.GetPhase(uid) != LateJoinRoutePhase.AwaitingClient)
            {
                return;
            }

            if (player != null && !ShouldRouteToTram())
            {
                LateJoinRouteTracker.SyncFromLivePlayer(player);
                return;
            }

            if (!ShouldRouteToTram())
            {
                return;
            }

            PrepareWaitingRoomIfHostInDungeon();

            if (!LateJoinRouteTracker.CanAttempt(uid, RouteRetryIntervalSeconds))
            {
                return;
            }

            bool resend = allowResend
                || LateJoinRouteTracker.GetPhase(uid) == LateJoinRoutePhase.AwaitingClient
                || (LateJoinRouteTracker.HasCompletedServerRoute(uid)
                    && player?.VRoom is MaintenanceRoom);

            if (LateJoinRouteTracker.GetStuckSeconds(uid) > 0f && resend)
            {
                ModLog.Info(
                    Feature,
                    $"Late joiner route retry — uid={uid} reason={reason} stuckFor={LateJoinRouteTracker.GetStuckSeconds(uid):F1}s attempts={LateJoinRouteTracker.GetAttemptCount(uid)}");
            }
            else if (reason == TramRouteAttemptReason.LevelLoadComplete)
            {
                ModLog.Info(
                    Feature,
                    $"Late joiner in maintenance — uid={uid} hostScene={JoinAnytimeHub.GetPdata()?.main?.GetType().Name ?? "null"}");
            }

            LateJoinRouteTracker.RecordAttempt(uid);

            bool success = player != null
                ? JoinAnytimeNetworkTools.SendPreGameTramStateToClient(player, resend)
                : WebDashboardSessionAccess.TryGetSessionContextByUid(uid, out SessionContext? sessionContext)
                    && sessionContext != null
                    && JoinAnytimeNetworkTools.ResendPreGameTramStateToSession(sessionContext, resend);

            if (success)
            {
                _ = PendingTramRouteUids.Remove(uid);
                return;
            }

            _ = PendingTramRouteUids.Add(uid);
        }

        private static void RouteAllMaintenanceLateJoiners(bool allowResend, TramRouteAttemptReason reason)
        {
            SessionManager? sessionManager = WebDashboardSessionAccess.GetSessionManager();
            if (sessionManager == null)
            {
                return;
            }

            PrepareWaitingRoomIfHostInDungeon();

            foreach (SessionContext context in WebDashboardSessionAccess.EnumerateSessionContexts(sessionManager))
            {
                VPlayer? player = WebDashboardSessionAccess.GetVPlayer(context);
                if (player == null || player.IsHost || !player.LevelLoadCompleted)
                {
                    continue;
                }

                AttemptTramRoute(player, allowResend, reason);
            }
        }

        private static void RetryPendingTramRoutes()
        {
            if (PendingTramRouteUids.Count == 0)
            {
                return;
            }

            List<long> pending = [.. PendingTramRouteUids];
            foreach (long uid in pending)
            {
                WebDashboardSessionAccess.TryGetPlayerByUid(uid, out VPlayer? player);
                AttemptTramRoute(uid, player, allowResend: true, TramRouteAttemptReason.PendingRetry);
            }
        }

        private static void SweepMaintenanceLateJoiners()
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

                if (player.VRoom is not MaintenanceRoom)
                {
                    continue;
                }

                AttemptTramRoute(player, allowResend: true, TramRouteAttemptReason.StuckSweep);
            }
        }

        private static void ResendAwaitingClientRoutes()
        {
            foreach (long uid in LateJoinRouteTracker.GetUidsNeedingResend())
            {
                if (WebDashboardSessionAccess.TryGetPlayerByUid(uid, out VPlayer? player)
                    && player!.VRoom is VWaitingRoom)
                {
                    MarkPreGameStateSent(uid);
                    continue;
                }

                AttemptTramRoute(uid, player, allowResend: true, TramRouteAttemptReason.AwaitingClientResend);
            }
        }

        private static void SyncWaitingHostPhasePlayers()
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

        private static void PrepareWaitingRoomIfHostInDungeon()
        {
            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
            if (pdata?.main is not GamePlayScene)
            {
                return;
            }

            JoinAnytimeRoomTools.PrepareWaitingRoomForEnter(force: true);
        }

        private static bool ShouldRouteToTram()
        {
            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
            return pdata?.ClientMode == NetworkClientMode.Host
                && pdata.main is InTramWaitingScene or GamePlayScene;
        }

        private enum TramRouteAttemptReason
        {
            LevelLoadComplete,
            HostSceneReady,
            PendingRetry,
            StuckSweep,
            AwaitingClientResend,
        }
    }
}
