using System;
using System.Collections.Generic;
using System.Reflection;
using MimesisPlayerEnhancement.Features.WebDashboard;
using ReluNetwork.ConstEnum;
using ReluProtocol.Enum;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    internal static class JoinAnytimeConnectingTracker
    {
        private const string Feature = "JoinAnytime";

        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo? SessionVPlayerField =
            typeof(SessionContext).GetField("_vPlayer", InstanceFlags);

        private static readonly Dictionary<long, PendingConnection> PendingByUid = [];

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
            }
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

                if (!TryResolvePlayer(pending.Uid, out VPlayer? player))
                {
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
                _ = PendingByUid.Remove(uid);
            }

            foreach (long uid in timedOut)
            {
                if (!PendingByUid.TryGetValue(uid, out PendingConnection pending))
                {
                    continue;
                }

                _ = PendingByUid.Remove(uid);

                if (ShouldIgnoreUid(uid))
                {
                    continue;
                }

                if (TryResolvePlayer(uid, out VPlayer? player) && (player!.IsHost || IsPlayerFullyReady(player)))
                {
                    continue;
                }

                KickTimedOutPlayer(pending);
            }
        }

        internal static bool HasPending()
        {
            foreach (long uid in PendingByUid.Keys)
            {
                if (!ShouldIgnoreUid(uid))
                {
                    return true;
                }
            }

            return false;
        }

        private static void RegisterPending(long uid, long sessionId)
        {
            float deadline = Time.time + GraceSeconds;
            PendingByUid[uid] = new PendingConnection(uid, sessionId, deadline);
        }

        private static void TryMarkReady(long uid)
        {
            if (!PendingByUid.ContainsKey(uid))
            {
                return;
            }

            if (!TryResolvePlayer(uid, out VPlayer? player))
            {
                return;
            }

            if (IsPlayerFullyReady(player!))
            {
                _ = PendingByUid.Remove(uid);
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

            if (TryResolvePlayer(pending.Uid, out VPlayer? player) && player!.IsHost)
            {
                return;
            }

            SessionManager? sessionManager = WebDashboardSessionAccess.GetSessionManager();
            if (sessionManager == null)
            {
                ModLog.Warn(Feature, $"Connecting tracker — kick skipped, no SessionManager (uid={pending.Uid})");
                return;
            }

            WebDashboardSessionAccess.DisconnectSession(
                sessionManager,
                pending.SessionId,
                DisconnectReason.KickByServer);

            ModLog.Info(Feature, $"Connecting tracker — kicked uid={pending.Uid} after {GraceSeconds}s timeout");
        }

        private static bool TryResolvePlayer(long uid, out VPlayer? player)
        {
            player = null;
            SessionManager? sessionManager = WebDashboardSessionAccess.GetSessionManager();
            if (sessionManager == null)
            {
                return false;
            }

            foreach (SessionContext context in WebDashboardSessionAccess.EnumerateSessionContexts(sessionManager))
            {
                if (context.GetPlayerUID() != uid)
                {
                    continue;
                }

                if (SessionVPlayerField?.GetValue(context) is VPlayer resolved)
                {
                    player = resolved;
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetHostPlayerUid(out long hostUid)
        {
            hostUid = 0;
            SessionManager? sessionManager = WebDashboardSessionAccess.GetSessionManager();
            if (sessionManager == null)
            {
                return false;
            }

            foreach (SessionContext context in WebDashboardSessionAccess.EnumerateSessionContexts(sessionManager))
            {
                if (SessionVPlayerField?.GetValue(context) is not VPlayer player || !player.IsHost)
                {
                    continue;
                }

                hostUid = player.UID;
                return true;
            }

            return false;
        }

        private static bool IsHostSession(SessionContext context)
        {
            if (context.PlayerInfoSnapshot?.IsHost == true)
            {
                return true;
            }

            return TryGetHostPlayerUid(out long hostUid) && context.GetPlayerUID() == hostUid;
        }

        private static bool ShouldIgnoreUid(long uid)
        {
            if (uid == 0)
            {
                return true;
            }

            if (TryGetHostPlayerUid(out long hostUid) && uid == hostUid)
            {
                return true;
            }

            if (TryResolvePlayer(uid, out VPlayer? player) && player!.IsHost)
            {
                return true;
            }

            return false;
        }

        private static float GraceSeconds => ModConfig.JoinConnectionGraceSeconds.Value;

        private sealed class PendingConnection
        {
            internal PendingConnection(long uid, long sessionId, float deadline)
            {
                Uid = uid;
                SessionId = sessionId;
                Deadline = deadline;
            }

            internal long Uid { get; }

            internal long SessionId { get; }

            internal float Deadline { get; }
        }
    }
}
