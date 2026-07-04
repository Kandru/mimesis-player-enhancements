using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    /// <summary>
    /// Pre-serializes SSE snapshot payloads on a background thread so the game thread only builds DTOs.
    /// </summary>
    internal static class WebDashboardSnapshotEventCache
    {
        private const string Feature = "WebDashboard";

        private static readonly object PendingGate = new();

        private static string _payload = "";
        private static int _payloadVersion = -1;
        private static int _serializeInFlight;
        private static PendingBuild? _pendingBuild;

        private sealed class PendingBuild
        {
            internal WebDashboardSnapshot Snapshot = new();
            internal int SnapshotVersion;
            internal bool LivePlayersOnly;
            internal List<WebDashboardPlayerDto> LivePlayers = [];
        }

        internal static string? TryGetPayload(int snapshotVersion)
        {
            return snapshotVersion == _payloadVersion && _payload.Length > 0 ? _payload : null;
        }

        internal static void ScheduleBuild(
            WebDashboardSnapshot snapshot,
            int snapshotVersion,
            bool livePlayersOnly,
            IReadOnlyList<WebDashboardPlayerDto>? livePlayers)
        {
            if (snapshotVersion == _payloadVersion)
            {
                return;
            }

            lock (PendingGate)
            {
                _pendingBuild = new PendingBuild
                {
                    Snapshot = snapshot,
                    SnapshotVersion = snapshotVersion,
                    LivePlayersOnly = livePlayersOnly,
                    LivePlayers = livePlayersOnly && livePlayers != null && livePlayers.Count > 0
                        ? [.. livePlayers]
                        : [],
                };
            }

            if (Interlocked.CompareExchange(ref _serializeInFlight, 1, 0) != 0)
            {
                return;
            }

            _ = Task.Run(ProcessQueue);
        }

        internal static void Clear()
        {
            lock (PendingGate)
            {
                _pendingBuild = null;
            }

            _payload = "";
            _payloadVersion = -1;
        }

        private static void ProcessQueue()
        {
            try
            {
                while (TryDequeue(out PendingBuild build))
                {
                    try
                    {
                        string payload = WebDashboardJson.SerializeSnapshotEvent(
                            build.Snapshot,
                            build.LivePlayersOnly,
                            build.LivePlayers);
                        _payload = payload;
                        _payloadVersion = build.SnapshotVersion;
                        WebDashboardSseHub.NotifySnapshotChanged();
                    }
                    catch (System.Exception ex)
                    {
                        ModLog.Warn(Feature, $"Snapshot event serialization failed — {ex.Message}");
                    }
                }
            }
            finally
            {
                _ = Interlocked.Exchange(ref _serializeInFlight, 0);
                if (HasPending())
                {
                    if (Interlocked.CompareExchange(ref _serializeInFlight, 1, 0) == 0)
                    {
                        _ = Task.Run(ProcessQueue);
                    }
                }
            }
        }

        private static bool TryDequeue(out PendingBuild build)
        {
            lock (PendingGate)
            {
                if (_pendingBuild == null)
                {
                    build = null!;
                    return false;
                }

                build = _pendingBuild;
                _pendingBuild = null;
                return true;
            }
        }

        private static bool HasPending()
        {
            lock (PendingGate)
            {
                return _pendingBuild != null;
            }
        }
    }
}
