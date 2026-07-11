using MimesisPlayerEnhancement.Features.WebDashboard;
using ReluNetwork.ConstEnum;
using ReluProtocol.Enum;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    internal static class JoinAnytimeConnectingTracker
    {
        private const string Feature = "JoinAnytime";

        private static readonly Dictionary<long, PendingConnection> PendingByUid = [];

        private static int _blockingPendingCount;

        internal static void Reset()
        {
            PendingByUid.Clear();
            _blockingPendingCount = 0;
            JoinAnytimePlayerRegistration.Reset();
        }

        internal static void OnServerLogin(SessionContext context)
        {
            if (!ModConfig.EnableJoinAnytime.Value || context == null)
            {
                return;
            }

            if (JoinAnytimeHub.GetPdata()?.ClientMode != NetworkClientMode.Host)
            {
                return;
            }

            if (IsHostSession(context))
            {
                return;
            }

            long uid = context.GetPlayerUID();
            if (uid == 0)
            {
                return;
            }

            RegisterPending(uid, context.GetSessionID());
            JoinAnytimePlayerRegistration.NoteDeferredConnect(uid, context.PlayerInfoSnapshot?.SteamID ?? 0);
            LateJoinManager.OnPlayerRegistered(uid);
            ModLog.Debug(Feature, $"Connecting tracker — registered uid={uid}, deadline={GraceSeconds}s");
        }

        internal static void OnLevelLoadCompleted(VPlayer player)
        {
            if (!ModConfig.EnableJoinAnytime.Value || player == null || player.IsHost)
            {
                return;
            }

            if (JoinAnytimeHub.GetPdata()?.ClientMode != NetworkClientMode.Host)
            {
                return;
            }

            TryMarkReady(player.UID);
            TryPromoteIfReady(player);
        }

        internal static void OnServerPlayerCreated(VPlayer player)
        {
            if (!ModConfig.EnableJoinAnytime.Value || player == null || player.IsHost)
            {
                return;
            }

            if (player.LevelLoadCompleted)
            {
                TryMarkReady(player.UID);
                TryPromoteIfReady(player);
            }
        }

        internal static void TryPromoteIfReady(VPlayer player)
        {
            if (!ModConfig.EnableJoinAnytime.Value || player == null || player.IsHost)
            {
                return;
            }

            if (JoinAnytimeHub.GetPdata()?.ClientMode != NetworkClientMode.Host)
            {
                return;
            }

            if (!IsPlayerFullyReady(player))
            {
                return;
            }

            JoinAnytimePlayerRegistration.MarkFullyReady(player);
            RemovePending(player.UID);
            ModLog.Debug(Feature, $"Connecting tracker — uid={player.UID} promoted ready");
        }

        internal static void OnUpdate()
        {
            if (!ModConfig.EnableJoinAnytime.Value || PendingByUid.Count == 0)
            {
                return;
            }

            if (JoinAnytimeHub.GetPdata()?.ClientMode != NetworkClientMode.Host)
            {
                PendingByUid.Clear();
                _blockingPendingCount = 0;
                return;
            }

            float now = Time.time;
            List<long> toRemove = [];
            List<long> timedOut = [];

            foreach (KeyValuePair<long, PendingConnection> entry in PendingByUid)
            {
                PendingConnection pending = entry.Value;
                if (ShouldIgnoreUid(pending.Uid))
                {
                    toRemove.Add(entry.Key);
                    continue;
                }

                if (!WebDashboardSessionAccess.TryGetPlayerByUid(pending.Uid, out VPlayer? player))
                {
                    if (!IsPendingSessionGone(pending))
                    {
                        continue;
                    }

                    toRemove.Add(entry.Key);
                    continue;
                }

                if (player!.IsHost)
                {
                    toRemove.Add(entry.Key);
                    continue;
                }

                if (IsPlayerFullyReady(player))
                {
                    toRemove.Add(entry.Key);
                    JoinAnytimePlayerRegistration.MarkFullyReady(player);
                    ModLog.Debug(Feature, $"Connecting tracker — uid={entry.Key} ready");
                    continue;
                }

                if (now >= pending.Deadline)
                {
                    timedOut.Add(entry.Key);
                }
            }

            foreach (long uid in toRemove)
            {
                RemovePending(uid);
            }

            foreach (long uid in timedOut)
            {
                if (!PendingByUid.TryGetValue(uid, out PendingConnection pending))
                {
                    continue;
                }

                RemovePending(uid);

                if (ShouldIgnoreUid(uid))
                {
                    continue;
                }

                if (WebDashboardSessionAccess.TryGetPlayerByUid(uid, out VPlayer? player)
                    && (player!.IsHost || IsPlayerFullyReady(player)))
                {
                    continue;
                }

                KickTimedOutPlayer(pending);
            }
        }

        internal static bool HasPending() => _blockingPendingCount > 0;

        private static void RegisterPending(long uid, long sessionId)
        {
            if (PendingByUid.ContainsKey(uid))
            {
                RemovePending(uid);
            }

            bool countsAsBlocking = !ShouldIgnoreUid(uid);
            float deadline = Time.time + GraceSeconds;
            PendingByUid[uid] = new PendingConnection(uid, sessionId, deadline, countsAsBlocking);

            if (countsAsBlocking)
            {
                _blockingPendingCount++;
            }
        }

        private static void RemovePending(long uid)
        {
            if (!PendingByUid.TryGetValue(uid, out PendingConnection pending))
            {
                return;
            }

            _ = PendingByUid.Remove(uid);
            if (pending.CountsAsBlocking && _blockingPendingCount > 0)
            {
                _blockingPendingCount--;
            }
        }

        private static void TryMarkReady(long uid)
        {
            if (!PendingByUid.ContainsKey(uid))
            {
                return;
            }

            if (!WebDashboardSessionAccess.TryGetPlayerByUid(uid, out VPlayer? player))
            {
                return;
            }

            if (IsPlayerFullyReady(player!))
            {
                JoinAnytimePlayerRegistration.MarkFullyReady(player!);
                RemovePending(uid);
                ModLog.Debug(Feature, $"Connecting tracker — uid={uid} marked ready");
            }
        }

        private static bool IsPlayerFullyReady(VPlayer player)
        {
            if (!player.LevelLoadCompleted)
            {
                return false;
            }

            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
            return pdata?.main switch
            {
                MaintenanceScene => player.VRoom is MaintenanceRoom,
                InTramWaitingScene or GamePlayScene =>
                    player.VRoom is VWaitingRoom && LateJoinManager.HasPreGameStateBeenSent(player.UID),
                _ => true,
            };
        }

        private static void KickTimedOutPlayer(PendingConnection pending)
        {
            if (ShouldIgnoreUid(pending.Uid))
            {
                return;
            }

            if (WebDashboardSessionAccess.TryGetPlayerByUid(pending.Uid, out VPlayer? player) && player!.IsHost)
            {
                return;
            }

            SessionManager? sessionManager = WebDashboardSessionAccess.GetSessionManager();
            if (sessionManager == null)
            {
                ModLog.Warn(Feature, $"Connecting tracker — kick skipped, no SessionManager (uid={pending.Uid})");
                return;
            }

            if (WebDashboardSessionAccess.TryGetPlayerByUid(pending.Uid, out VPlayer? kickPlayer))
            {
                JoinAnytimePlayerRegistration.AbandonIncomplete(
                    pending.Uid,
                    GameSessionAccess.ResolveSteamId(pending.Uid, kickPlayer!.IsHost));
            }
            else
            {
                JoinAnytimePlayerRegistration.AbandonIncomplete(pending.Uid);
            }

            WebDashboardSessionAccess.DisconnectSession(
                sessionManager,
                pending.SessionId,
                DisconnectReason.KickByServer);

            ModLog.Info(Feature, $"Connecting tracker — kicked uid={pending.Uid} after {GraceSeconds}s timeout");
        }

        private static bool IsPendingSessionGone(PendingConnection pending)
        {
            SessionManager? sessionManager = WebDashboardSessionAccess.GetSessionManager();
            if (sessionManager == null)
            {
                return true;
            }

            return !WebDashboardSessionAccess.TryGetSessionContextBySessionId(
                sessionManager,
                pending.SessionId,
                out _);
        }

        private static bool IsHostSession(SessionContext context)
        {
            if (context.PlayerInfoSnapshot?.IsHost == true)
            {
                return true;
            }

            return WebDashboardSessionAccess.TryGetHostPlayerUid(out long hostUid)
                && context.GetPlayerUID() == hostUid;
        }

        private static bool ShouldIgnoreUid(long uid)
        {
            if (uid == 0)
            {
                return true;
            }

            if (WebDashboardSessionAccess.TryGetHostPlayerUid(out long hostUid) && uid == hostUid)
            {
                return true;
            }

            return WebDashboardSessionAccess.TryGetPlayerByUid(uid, out VPlayer? player) && player!.IsHost;
        }

        private static float GraceSeconds => ModConfig.JoinConnectionGraceSeconds.Value;

        private sealed class PendingConnection
        {
            internal PendingConnection(long uid, long sessionId, float deadline, bool countsAsBlocking)
            {
                Uid = uid;
                SessionId = sessionId;
                Deadline = deadline;
                CountsAsBlocking = countsAsBlocking;
            }

            internal long Uid { get; }

            internal long SessionId { get; }

            internal float Deadline { get; }

            internal bool CountsAsBlocking { get; }
        }
    }
}
