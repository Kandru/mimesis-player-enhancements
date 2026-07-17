using System.Threading;
using System.Threading.Tasks;
using MimesisPlayerEnhancement.Features.Statistics;
using MimesisPlayerEnhancement.Features.Statistics.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardLeaderboardCache
    {
        private const string Feature = "WebDashboard";

        private static readonly object SerializeLock = new();

        private static string? _cachedJson;
        private static int _cachedSlotId = -1;
        private static int _cachedRevision = -1;
        private static List<ulong> _cachedLeaderboardSteamIds = [];
        private static int _serializeInFlight;
        private static int _pendingRevision;

        internal static string? UpdateAndGetCached(
            int saveSlotId,
            IReadOnlyList<ulong> connectedSteamIds)
        {
            if (saveSlotId < 0 || !ModConfig.EnableStatistics.Value || !WebDashboardGameState.IsHost())
            {
                return _cachedSlotId == saveSlotId ? _cachedJson : null;
            }

            int revision = PlayerRegistry.Revision;
            if (_cachedSlotId == saveSlotId
                && revision == _cachedRevision
                && !string.IsNullOrEmpty(_cachedJson))
            {
                return _cachedJson;
            }

            ScheduleBackgroundRebuild(saveSlotId, connectedSteamIds, revision);
            return _cachedSlotId == saveSlotId ? _cachedJson : null;
        }

        internal static IReadOnlyList<ulong> GetCachedLeaderboardSteamIds()
        {
            lock (SerializeLock)
            {
                return _cachedLeaderboardSteamIds;
            }
        }

        internal static void Clear()
        {
            lock (SerializeLock)
            {
                _cachedJson = null;
                _cachedSlotId = -1;
                _cachedRevision = -1;
                _cachedLeaderboardSteamIds = [];
            }

            _pendingRevision = 0;
        }

        private static void ScheduleBackgroundRebuild(
            int saveSlotId,
            IReadOnlyList<ulong> connectedSteamIds,
            int revision)
        {
            if (revision > _pendingRevision)
            {
                _pendingRevision = revision;
            }

            if (Interlocked.CompareExchange(ref _serializeInFlight, 1, 0) != 0)
            {
                return;
            }

            LeaderboardRebuildSnapshot snapshot = WebDashboardBackgroundSnapshots.CaptureLeaderboardRebuild(saveSlotId);
            List<ulong> connectedIds = [.. connectedSteamIds];
            int rebuildRevision = revision;
            _ = Task.Run(() => BuildAndSerializeBackground(saveSlotId, connectedIds, rebuildRevision, snapshot));
        }

        private static void BuildAndSerializeBackground(
            int saveSlotId,
            List<ulong> connectedSteamIds,
            int revision,
            LeaderboardRebuildSnapshot snapshot)
        {
            try
            {
                LeaderboardDocument doc = snapshot.Players.Count == 0
                    ? new LeaderboardDocument { SaveSlotId = saveSlotId, UpdatedAtUtc = DateTime.UtcNow }
                    : LeaderboardBuilder.BuildFromSnapshot(
                        snapshot.SaveSlotId,
                        snapshot.CurrentZone,
                        snapshot.Players,
                        snapshot.DisplayNames);

                string json = WebDashboardJson.SerializeLeaderboardResponse(doc, connectedSteamIds);
                List<ulong> leaderboardSteamIds = [];
                foreach (LeaderboardEntry entry in doc.Entries)
                {
                    if (entry.SteamId != 0)
                    {
                        leaderboardSteamIds.Add(entry.SteamId);
                    }
                }

                lock (SerializeLock)
                {
                    _cachedJson = json;
                    _cachedSlotId = saveSlotId;
                    _cachedRevision = revision;
                    _cachedLeaderboardSteamIds = leaderboardSteamIds;
                }

                WebDashboardSnapshotCache.MarkDirty();
                WebDashboardSnapshotCache.RequestFullPublish();
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Background leaderboard build failed — {ex.Message}");
            }
            finally
            {
                _ = Interlocked.Exchange(ref _serializeInFlight, 0);
                int cachedRevision;
                int pending;
                lock (SerializeLock)
                {
                    cachedRevision = _cachedRevision;
                }

                pending = Volatile.Read(ref _pendingRevision);
                if (pending != 0 && pending != cachedRevision)
                {
                    WebDashboardSnapshotCache.MarkDirty();
                    WebDashboardSnapshotCache.RequestFullPublish();
                }
            }
        }
    }
}
