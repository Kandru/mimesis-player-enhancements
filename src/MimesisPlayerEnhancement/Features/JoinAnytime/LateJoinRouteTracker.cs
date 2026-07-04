using MimesisPlayerEnhancement.Features.WebDashboard;
using ReluNetwork.ConstEnum;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    /// <summary>Tracks per-UID late join route phase for retries, dashboard display, and cleanup.</summary>
    internal static class LateJoinRouteTracker
    {
        private static readonly Dictionary<long, RouteState> StatesByUid = [];

        internal static void Reset()
        {
            StatesByUid.Clear();
        }

        internal static void OnPlayerRegistered(long uid)
        {
            if (uid == 0)
            {
                return;
            }

            StatesByUid.TryAdd(uid, new RouteState(uid));
            NotifyDashboardIfChanged();
        }

        internal static void OnPlayerDisconnected(long uid)
        {
            if (uid == 0)
            {
                return;
            }

            if (StatesByUid.Remove(uid))
            {
                NotifyDashboardIfChanged();
            }
        }

        internal static void SyncFromLivePlayer(VPlayer player)
        {
            if (player == null || player.IsHost || player.UID == 0)
            {
                return;
            }

            RouteState state = GetOrCreate(player.UID);
            LateJoinRoutePhase previous = state.Phase;

            if (player.VRoom is VWaitingRoom)
            {
                state.SetPhase(LateJoinRoutePhase.InWaitingRoom);
            }
            else if (player.VRoom is MaintenanceRoom)
            {
                state.SetPhase(ShouldRouteToTram()
                    ? LateJoinRoutePhase.InMaintenance
                    : LateJoinRoutePhase.WaitingHostPhase);
            }

            if (state.Phase != previous)
            {
                NotifyDashboardIfChanged();
            }
        }

        internal static void MarkAwaitingClient(long uid)
        {
            RouteState state = GetOrCreate(uid);
            LateJoinRoutePhase previous = state.Phase;
            state.SetPhase(LateJoinRoutePhase.AwaitingClient);
            if (state.Phase != previous)
            {
                NotifyDashboardIfChanged();
            }
        }

        internal static void MarkInWaitingRoom(long uid)
        {
            RouteState state = GetOrCreate(uid);
            LateJoinRoutePhase previous = state.Phase;
            state.SetPhase(LateJoinRoutePhase.InWaitingRoom);
            if (state.Phase != previous)
            {
                NotifyDashboardIfChanged();
            }
        }

        internal static void RecordAttempt(long uid)
        {
            RouteState state = GetOrCreate(uid);
            state.RecordAttempt();
        }

        internal static bool CanAttempt(long uid, float retryIntervalSeconds)
        {
            if (!StatesByUid.TryGetValue(uid, out RouteState? state))
            {
                return true;
            }

            return Time.time >= state.LastAttemptTime + retryIntervalSeconds;
        }

        internal static bool HasCompletedServerRoute(long uid) =>
            StatesByUid.TryGetValue(uid, out RouteState? state)
            && state.Phase is LateJoinRoutePhase.AwaitingClient or LateJoinRoutePhase.InWaitingRoom;

        internal static bool HasPreGameStateBeenSent(long uid) => HasCompletedServerRoute(uid);

        internal static LateJoinRoutePhase GetPhase(long uid) =>
            StatesByUid.TryGetValue(uid, out RouteState? state) ? state.Phase : LateJoinRoutePhase.None;

        internal static float GetStuckSeconds(long uid)
        {
            if (!StatesByUid.TryGetValue(uid, out RouteState? state) || state.FirstAttemptTime <= 0f)
            {
                return 0f;
            }

            if (state.Phase is LateJoinRoutePhase.InWaitingRoom or LateJoinRoutePhase.None)
            {
                return 0f;
            }

            return Time.time - state.FirstAttemptTime;
        }

        internal static int GetAttemptCount(long uid) =>
            StatesByUid.TryGetValue(uid, out RouteState? state) ? state.AttemptCount : 0;

        internal static int GetActiveRoutingCount()
        {
            int count = 0;
            foreach (RouteState state in StatesByUid.Values)
            {
                if (state.Phase is LateJoinRoutePhase.WaitingHostPhase
                    or LateJoinRoutePhase.InMaintenance
                    or LateJoinRoutePhase.AwaitingClient)
                {
                    count++;
                }
            }

            return count;
        }

        internal static IEnumerable<long> GetUidsNeedingResend()
        {
            foreach (RouteState state in StatesByUid.Values)
            {
                if (state.Phase == LateJoinRoutePhase.AwaitingClient)
                {
                    yield return state.Uid;
                }
            }
        }

        internal static string GetDisplayLabel(long uid)
        {
            return GetPhase(uid) switch
            {
                LateJoinRoutePhase.WaitingHostPhase => "late join — waiting for host",
                LateJoinRoutePhase.InMaintenance => "late join — routing to tram",
                LateJoinRoutePhase.AwaitingClient => "late join — awaiting tram load",
                LateJoinRoutePhase.InWaitingRoom => "late join — in tram",
                _ => "",
            };
        }

        internal static void ApplyDashboardFields(WebDashboard.Models.WebDashboardPlayerDto dto, SessionContext? context)
        {
            if (!ModConfig.EnableJoinAnytime.Value || dto.IsHost || dto.PlayerUid == 0)
            {
                return;
            }

            VPlayer? vPlayer = context != null ? WebDashboardSessionAccess.GetVPlayer(context) : null;
            if (vPlayer != null)
            {
                SyncFromLivePlayer(vPlayer);
            }

            LateJoinRoutePhase phase = GetPhase(dto.PlayerUid);
            if (phase == LateJoinRoutePhase.None && vPlayer?.VRoom is MaintenanceRoom && ShouldRouteToTram())
            {
                phase = LateJoinRoutePhase.InMaintenance;
            }
            else if (phase == LateJoinRoutePhase.None && vPlayer?.VRoom is MaintenanceRoom)
            {
                phase = LateJoinRoutePhase.WaitingHostPhase;
            }
            else if (phase == LateJoinRoutePhase.None && vPlayer?.VRoom is VWaitingRoom)
            {
                phase = LateJoinRoutePhase.InWaitingRoom;
            }

            dto.LateJoinPhase = phase.ToString();
            dto.LateJoinLabel = GetDisplayLabelForPhase(phase);
            float stuck = GetStuckSeconds(dto.PlayerUid);
            dto.LateJoinStuckSeconds = stuck > 0f ? stuck : null;
            dto.LateJoinAttemptCount = GetAttemptCount(dto.PlayerUid);
        }

        private static string GetDisplayLabelForPhase(LateJoinRoutePhase phase) =>
            phase switch
            {
                LateJoinRoutePhase.WaitingHostPhase => ModL10n.Get("joinanytime.late_join_waiting_host"),
                LateJoinRoutePhase.InMaintenance => ModL10n.Get("joinanytime.late_join_routing_tram"),
                LateJoinRoutePhase.AwaitingClient => ModL10n.Get("joinanytime.late_join_awaiting_tram"),
                LateJoinRoutePhase.InWaitingRoom => ModL10n.Get("joinanytime.late_join_in_tram"),
                _ => "",
            };

        private static RouteState GetOrCreate(long uid)
        {
            if (!StatesByUid.TryGetValue(uid, out RouteState? state))
            {
                state = new RouteState(uid);
                StatesByUid[uid] = state;
            }

            return state;
        }

        private static bool ShouldRouteToTram()
        {
            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
            return pdata?.ClientMode == NetworkClientMode.Host
                && pdata.main is InTramWaitingScene or GamePlayScene;
        }

        private static void NotifyDashboardIfChanged()
        {
            if (ModConfig.EnableWebDashboard.Value)
            {
                WebDashboardSnapshotCache.MarkDirty();
            }
        }

        private sealed class RouteState
        {
            internal RouteState(long uid)
            {
                Uid = uid;
            }

            internal long Uid { get; }

            internal LateJoinRoutePhase Phase { get; private set; }

            internal float FirstAttemptTime { get; private set; }

            internal float LastAttemptTime { get; private set; }

            internal int AttemptCount { get; private set; }

            internal void SetPhase(LateJoinRoutePhase phase)
            {
                Phase = phase;
                if (phase == LateJoinRoutePhase.InWaitingRoom)
                {
                    FirstAttemptTime = 0f;
                    LastAttemptTime = 0f;
                    AttemptCount = 0;
                }
            }

            internal void RecordAttempt()
            {
                float now = Time.time;
                if (FirstAttemptTime <= 0f)
                {
                    FirstAttemptTime = now;
                }

                LastAttemptTime = now;
                AttemptCount++;
            }
        }
    }
}
