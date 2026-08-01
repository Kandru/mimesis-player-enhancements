using System.Collections.Concurrent;
using System.Threading.Tasks;
using MimesisPlayerEnhancement.Features.Statistics.Models;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    public static class StatisticsStore
    {
        private const string Feature = "Statistics";

        private static readonly Dictionary<int, SlotStatisticsDocument?> LoadCache = new();

        private static readonly ConcurrentDictionary<int, PendingSlotSave> InFlightBySlot = new();

        private sealed class PendingSlotSave
        {
            internal SlotStatisticsDocument? LatestDocument;
            internal bool WaitForCompletion;
            internal Task? PrepareTask;
        }

        public static SlotStatisticsDocument? TryLoadSlotDocument(int slotId)
        {
            return TryLoadSlot(slotId);
        }

        public static SlotStatisticsDocument LoadSlot(int slotId)
        {
            SlotStatisticsDocument? loaded = TryLoadSlot(slotId);
            SlotStatisticsDocument document = loaded ?? new SlotStatisticsDocument();
            StatisticsHistory.Load(document);
            return document;
        }

        internal static void SaveSlot(int slotId, bool waitForCompletion = false)
        {
            string? path = SaveSidecarPaths.GetStatisticsPath(slotId);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            PendingSlotSave pending = InFlightBySlot.GetOrAdd(slotId, static _ => new PendingSlotSave());
            lock (pending)
            {
                pending.LatestDocument = StatisticsHistory.CloneDocument();
                pending.WaitForCompletion = waitForCompletion;

                if (pending.PrepareTask is { IsCompleted: false })
                {
                    if (!waitForCompletion)
                    {
                        ModLog.Debug(Feature, $"Coalescing slot {slotId} statistics save — serialize already in flight.");
                        return;
                    }
                }
                else
                {
                    pending.PrepareTask = Task.Run(() => PrepareAndWrite(slotId, path, pending));
                    if (!waitForCompletion)
                    {
                        return;
                    }
                }
            }

            if (waitForCompletion)
            {
                WaitForTask(pending.PrepareTask);
            }
        }

        internal static void FlushAllSync()
        {
            for (int pass = 0; pass < 8 && !InFlightBySlot.IsEmpty; pass++)
            {
                foreach (KeyValuePair<int, PendingSlotSave> kvp in InFlightBySlot)
                {
                    Task? task;
                    lock (kvp.Value)
                    {
                        task = kvp.Value.PrepareTask;
                    }

                    WaitForTask(task);
                }
            }
        }

        internal static void InvalidateSlot(int slotId)
        {
            lock (LoadCache)
            {
                LoadCache.Remove(slotId);
            }
        }

        private static void PrepareAndWrite(int slotId, string path, PendingSlotSave pending)
        {
            SlotStatisticsDocument? document;
            bool waitForCompletion;
            lock (pending)
            {
                document = pending.LatestDocument;
                waitForCompletion = pending.WaitForCompletion;
                pending.LatestDocument = null;
                pending.WaitForCompletion = false;
            }

            try
            {
                if (document == null)
                {
                    return;
                }

                document.Version = SlotStatisticsDocument.CurrentVersion;
                document.UpdatedAtUtc = DateTime.UtcNow;
                string json = StatisticsJson.SerializeSlot(document);
                BackgroundFileWriteQueue.EnqueueText(path, json, Feature, waitForCompletion);
                InvalidateSlot(slotId);
                ModLog.Debug(Feature, $"Saved slot {slotId} statistics ({document.Globals.Count} players) -> {path}");
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Background slot {slotId} statistics save failed: {ex.Message}");
            }

            ScheduleRetryIfNeeded(slotId, path, pending);
        }

        private static void ScheduleRetryIfNeeded(int slotId, string path, PendingSlotSave pending)
        {
            lock (pending)
            {
                if (pending.LatestDocument == null)
                {
                    pending.PrepareTask = null;
                    _ = InFlightBySlot.TryRemove(slotId, out _);
                    return;
                }

                pending.PrepareTask = Task.Run(() => PrepareAndWrite(slotId, path, pending));
            }
        }

        private static SlotStatisticsDocument? TryLoadSlot(int slotId)
        {
            lock (LoadCache)
            {
                if (LoadCache.TryGetValue(slotId, out SlotStatisticsDocument? cached))
                {
                    return cached == null ? null : StatisticsHistory.CloneSlot(cached);
                }
            }

            string? path = SaveSidecarPaths.GetStatisticsPath(slotId);
            if (string.IsNullOrEmpty(path))
            {
                CacheMiss(slotId, null);
                return null;
            }

            string? json = AtomicFileIO.ReadText(path, Feature);
            if (string.IsNullOrEmpty(json))
            {
                CacheMiss(slotId, null);
                return null;
            }

            SlotStatisticsDocument? slot = StatisticsJson.DeserializeSlot(json);
            if (slot == null)
            {
                ModLog.Warn(Feature, $"Corrupt statistics file — ignoring: {path}");
                CacheMiss(slotId, null);
                return null;
            }

            if (slot.Version != SlotStatisticsDocument.CurrentVersion)
            {
                StatisticsLegacyFileCleanup.Retire(path, slot.Version, Feature);
                ModLog.Info(Feature, $"Legacy statistics file discarded — schema v{slot.Version} → v{SlotStatisticsDocument.CurrentVersion}.");
                CacheMiss(slotId, null);
                return null;
            }

            CacheMiss(slotId, StatisticsHistory.CloneSlot(slot));
            return slot;
        }

        private static void CacheMiss(int slotId, SlotStatisticsDocument? document)
        {
            lock (LoadCache)
            {
                LoadCache[slotId] = document;
            }
        }

        private static void WaitForTask(Task? task)
        {
            TaskWaitHelper.WaitSync(task, Feature, "Statistics save");
        }
    }
}
