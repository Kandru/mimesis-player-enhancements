using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using MimesisPlayerEnhancement.Features.Statistics.Models;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    public static class StatisticsStore
    {
        private const string Feature = "Statistics";

        private static int _cachedLoadSlotId = -999;
        private static SlotStatisticsDocument? _cachedLoadSlot;

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

        internal static void SaveSlot(int slotId, SlotStatisticsDocument document, bool waitForCompletion = false)
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
                InvalidateLoadCache(slotId);
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
            if (_cachedLoadSlotId == slotId && _cachedLoadSlot != null)
            {
                return StatisticsHistory.CloneSlot(_cachedLoadSlot);
            }

            string? path = SaveSidecarPaths.GetStatisticsPath(slotId);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            string? json = AtomicFileIO.ReadText(path, Feature);
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            SlotStatisticsDocument? slot = StatisticsJson.DeserializeSlot(json);
            if (slot == null)
            {
                ModLog.Warn(Feature, $"Corrupt statistics file — ignoring: {path}");
                return null;
            }

            if (slot.Version != SlotStatisticsDocument.CurrentVersion)
            {
                TryBackupLegacyFile(path, slot.Version);
                ModLog.Info(Feature, $"Legacy statistics file discarded — schema v{slot.Version} → v{SlotStatisticsDocument.CurrentVersion}.");
                return null;
            }

            _cachedLoadSlotId = slotId;
            _cachedLoadSlot = StatisticsHistory.CloneSlot(slot);
            return slot;
        }

        private static void TryBackupLegacyFile(string path, int version)
        {
            try
            {
                string backup = $"{path}.legacy-v{version}.bak";
                if (!File.Exists(backup) && File.Exists(path))
                {
                    File.Move(path, backup);
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Failed to backup legacy statistics file — {ex.Message}");
            }
        }

        private static void InvalidateLoadCache(int slotId)
        {
            if (_cachedLoadSlotId == slotId)
            {
                _cachedLoadSlotId = -999;
                _cachedLoadSlot = null;
            }
        }

        private static void WaitForTask(Task? task)
        {
            TaskWaitHelper.WaitSync(task, Feature, "Statistics save");
        }
    }
}
