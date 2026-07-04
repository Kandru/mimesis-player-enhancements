using System.Collections.Generic;
using MimesisPlayerEnhancement.Features.WebDashboard;
using ReluNetwork.ConstEnum;
using ReluProtocol;
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
        private const float TramRouteRetryIntervalSeconds = 0.5f;

        private static readonly HashSet<long> SentPreGameStateUids = [];
        private static readonly HashSet<long> PendingTramRouteUids = [];

        private static float _nextTramRouteRetryTime;

        internal static bool IsEnabled => ModConfig.EnableJoinAnytime.Value;

        /// <summary>Clears routing state so stale UIDs cannot leak across sessions or feature toggles.</summary>
        internal static void Reset()
        {
            SentPreGameStateUids.Clear();
            PendingTramRouteUids.Clear();
            _nextTramRouteRetryTime = 0f;
        }

        internal static void OnLevelLoadCompleted(VPlayer player)
        {
            TryRouteLateJoinerToTram(player, allowResend: false);
        }

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
            if (!IsEnabled || PendingTramRouteUids.Count == 0)
            {
                return;
            }

            if (!ShouldRouteToTram())
            {
                return;
            }

            if (Time.time < _nextTramRouteRetryTime)
            {
                return;
            }

            _nextTramRouteRetryTime = Time.time + TramRouteRetryIntervalSeconds;
            RetryPendingTramRoutes();
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

        internal static bool HasPreGameStateBeenSent(long uid) => SentPreGameStateUids.Contains(uid);

        internal static void MarkPreGameStateSent(long uid)
        {
            _ = SentPreGameStateUids.Add(uid);
            _ = PendingTramRouteUids.Remove(uid);
        }

        private static void TryRouteLateJoinerToTram(VPlayer player, bool allowResend)
        {
            if (!IsEnabled || player == null || player.IsHost)
            {
                return;
            }

            if (!player.LevelLoadCompleted)
            {
                return;
            }

            if (player.VRoom is VWaitingRoom)
            {
                MarkPreGameStateSent(player.UID);
                return;
            }

            if (player.VRoom is not MaintenanceRoom)
            {
                return;
            }

            if (!ShouldRouteToTram())
            {
                return;
            }

            ModLog.Info(Feature, $"Late joiner in maintenance — uid={player.UID} hostScene={JoinAnytimeHub.GetPdata()?.main?.GetType().Name ?? "null"}");

            bool resend = allowResend
                || (HasPreGameStateBeenSent(player.UID) && player.VRoom is MaintenanceRoom);

            if (JoinAnytimeNetworkTools.SendPreGameTramStateToClient(player, resend))
            {
                return;
            }

            _ = PendingTramRouteUids.Add(player.UID);
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

                TryRouteLateJoinerToTram(player, allowResend);
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
                if (!WebDashboardSessionAccess.TryGetPlayerByUid(uid, out VPlayer? player))
                {
                    _ = PendingTramRouteUids.Remove(uid);
                    continue;
                }

                if (player!.VRoom is VWaitingRoom)
                {
                    MarkPreGameStateSent(uid);
                    continue;
                }

                if (player.VRoom is not MaintenanceRoom)
                {
                    _ = PendingTramRouteUids.Remove(uid);
                    continue;
                }

                if (JoinAnytimeNetworkTools.SendPreGameTramStateToClient(player, allowResend: true))
                {
                    ModLog.Info(Feature, $"Late joiner tram route retry succeeded — uid={uid}");
                }
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
