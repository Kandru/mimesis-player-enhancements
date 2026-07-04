using System.Threading;
using System.Threading.Tasks;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    /// <summary>
    /// Offline/historical player rows for the dashboard — rebuilt on a background thread when stats revision changes.
    /// </summary>
    internal static class WebDashboardOfflinePlayerCache
    {
        private const string Feature = "WebDashboard";

        private static List<WebDashboardPlayerDto> _cached = [];
        private static int _cachedRevision = -1;
        private static int _buildInFlight;
        private static int _pendingRevision;

        internal static int CachedRevision => Volatile.Read(ref _cachedRevision);

        internal static IReadOnlyList<WebDashboardPlayerDto> GetCached()
        {
            return _cached;
        }

        internal static void EnsureFresh(int revision)
        {
            if (revision == _cachedRevision)
            {
                return;
            }

            if (revision > _pendingRevision)
            {
                _pendingRevision = revision;
            }

            if (Interlocked.CompareExchange(ref _buildInFlight, 1, 0) != 0)
            {
                return;
            }

            int rebuildRevision = revision;
            _ = Task.Run(() => RebuildBackground(rebuildRevision));
        }

        internal static void Clear()
        {
            _cached = [];
            _cachedRevision = -1;
            _pendingRevision = 0;
        }

        private static void RebuildBackground(int revision)
        {
            try
            {
                List<WebDashboardPlayerDto> built = WebDashboardPlayerService.BuildOfflineStatisticsPlayers();
                _cached = built;
                _cachedRevision = revision;
                WebDashboardSnapshotCache.MarkDirty();
                WebDashboardSnapshotCache.RequestFullPublish();
            }
            catch (System.Exception ex)
            {
                ModLog.Warn(Feature, $"Offline player cache rebuild failed — {ex.Message}");
            }
            finally
            {
                _ = Interlocked.Exchange(ref _buildInFlight, 0);
                int pending = Volatile.Read(ref _pendingRevision);
                if (pending != 0 && pending != _cachedRevision)
                {
                    WebDashboardSnapshotCache.MarkDirty();
                    WebDashboardSnapshotCache.RequestFullPublish();
                }
            }
        }
    }
}
