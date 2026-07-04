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

        private static string _payload = "";
        private static int _payloadVersion = -1;
        private static int _serializeInFlight;

        internal static string? TryGetPayload(int snapshotVersion)
        {
            return snapshotVersion == _payloadVersion && _payload.Length > 0 ? _payload : null;
        }

        internal static void ScheduleBuild(WebDashboardSnapshot snapshot, int snapshotVersion)
        {
            if (snapshotVersion == _payloadVersion)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _serializeInFlight, 1, 0) != 0)
            {
                return;
            }

            WebDashboardSnapshot copy = CloneSnapshot(snapshot);
            _ = Task.Run(() => BuildBackground(copy, snapshotVersion));
        }

        internal static void Clear()
        {
            _payload = "";
            _payloadVersion = -1;
        }

        private static void BuildBackground(WebDashboardSnapshot snapshot, int snapshotVersion)
        {
            try
            {
                string payload = WebDashboardJson.SerializeSnapshotEvent(snapshot);
                _payload = payload;
                _payloadVersion = snapshotVersion;
                WebDashboardSseHub.NotifySnapshotChanged();
            }
            catch (System.Exception ex)
            {
                ModLog.Warn(Feature, $"Snapshot event serialization failed — {ex.Message}");
            }
            finally
            {
                _ = Interlocked.Exchange(ref _serializeInFlight, 0);
            }
        }

        private static WebDashboardSnapshot CloneSnapshot(WebDashboardSnapshot source)
        {
            return new WebDashboardSnapshot
            {
                Status = source.Status,
                Players = source.Players.Count == 0 ? [] : [.. source.Players],
                LeaderboardJson = source.LeaderboardJson,
                MinimapLayout = source.MinimapLayout,
                MinimapMarkers = source.MinimapMarkers.Count == 0 ? [] : [.. source.MinimapMarkers],
                MinimapTrain = source.MinimapTrain,
            };
        }
    }
}
