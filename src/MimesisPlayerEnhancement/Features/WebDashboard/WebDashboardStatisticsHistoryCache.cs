using System.Threading;
using System.Threading.Tasks;
using MimesisPlayerEnhancement.Features.Statistics.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardStatisticsHistoryCache
    {
        private const string Feature = "WebDashboard";

        private static readonly object SerializeLock = new();

        private static string? _cachedJson;
        private static int _cachedSlotId = -1;
        private static int _cachedRevision = -1;
        private static int _serializeInFlight;
        private static int _pendingRevision;
        private static int _generation;

        internal static string? UpdateAndGetCached(int saveSlotId)
        {
            if (saveSlotId < 0 || !ModConfig.EnableStatistics.Value || !WebDashboardGameState.IsHost())
            {
                return _cachedSlotId == saveSlotId ? _cachedJson : null;
            }

            int revision = StatisticsHistory.Revision;
            if (_cachedSlotId == saveSlotId
                && revision == _cachedRevision
                && !string.IsNullOrEmpty(_cachedJson))
            {
                return _cachedJson;
            }

            ScheduleBackgroundRebuild(saveSlotId, revision);
            return _cachedSlotId == saveSlotId ? _cachedJson : null;
        }

        internal static void Clear()
        {
            _generation++;
            lock (SerializeLock)
            {
                _cachedJson = null;
                _cachedSlotId = -1;
                _cachedRevision = -1;
            }

            _pendingRevision = 0;
        }

        private static void ScheduleBackgroundRebuild(int saveSlotId, int revision)
        {
            if (revision > _pendingRevision)
            {
                _pendingRevision = revision;
            }

            if (Interlocked.CompareExchange(ref _serializeInFlight, 1, 0) != 0)
            {
                return;
            }

            HistoryRebuildSnapshot snapshot = WebDashboardBackgroundSnapshots.CaptureHistoryRebuild(saveSlotId);
            int rebuildRevision = revision;
            int generationAtStart = Volatile.Read(ref _generation);
            _ = Task.Run(() => BuildAndSerializeBackground(saveSlotId, rebuildRevision, snapshot, generationAtStart));
        }

        private static void BuildAndSerializeBackground(
            int saveSlotId,
            int revision,
            HistoryRebuildSnapshot snapshot,
            int generationAtStart)
        {
            try
            {
                StatisticsHistoryDocument history = StatisticsHistoryBuilder.Build(
                    snapshot.SaveSlotId,
                    snapshot.Document,
                    (steamId, fallback) => snapshot.DisplayNames.TryGetValue(steamId, out string? name) && !string.IsNullOrWhiteSpace(name)
                        ? name
                        : fallback ?? steamId.ToString());

                string json = WebDashboardJson.SerializeStatisticsHistory(history);

                if (generationAtStart != Volatile.Read(ref _generation))
                {
                    return;
                }

                lock (SerializeLock)
                {
                    _cachedJson = json;
                    _cachedSlotId = saveSlotId;
                    _cachedRevision = revision;
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Background statistics history build failed — {ex.Message}");
            }
            finally
            {
                _ = Interlocked.Exchange(ref _serializeInFlight, 0);
            }
        }
    }
}
