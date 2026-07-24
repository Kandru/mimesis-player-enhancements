using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace MimesisPlayerEnhancement.Features.Persistence
{
    internal sealed class SpeechEventSaveSnapshot
    {
        internal string SpeechPath = string.Empty;
        internal byte[]? SpeechBytes;
        internal int SerializedCount;
    }

    internal static class PersistenceWriteQueue
    {
        private const string Feature = "Persistence";

        private static readonly ConcurrentDictionary<int, PendingSlotSave> InFlightBySlot = new();

        private sealed class PendingSlotSave
        {
            internal List<SpeechEventCapturedRecord>? LatestRecords;
            internal string SpeechPath = string.Empty;
            internal Task? PrepareTask;
        }

        /// <summary>
        /// Captures speech event bytes on the game thread, then packs and writes on a worker.
        /// </summary>
        internal static void EnqueueSave(int slotId, List<SpeechEvent> speechEvents)
        {
            List<SpeechEventCapturedRecord> records = SpeechEventFileStore.CaptureRecords(speechEvents);
            string speechPath = SaveSidecarPaths.GetSpeechPath(slotId) ?? string.Empty;
            ModLog.Debug(Feature, $"Captured {records.Count} SpeechEvents for slot {slotId}");

            PendingSlotSave pending = InFlightBySlot.GetOrAdd(slotId, static _ => new PendingSlotSave());
            lock (pending)
            {
                pending.LatestRecords = records;
                pending.SpeechPath = speechPath;

                if (pending.PrepareTask is { IsCompleted: false })
                {
                    ModLog.Debug(Feature, $"Coalescing slot {slotId} save — pack/write already in flight.");
                    return;
                }

                pending.PrepareTask = Task.Run(() => PrepareAndWrite(slotId, pending));
            }
        }

        internal static void FlushAllSync()
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

            BackgroundFileWriteQueue.FlushAllSync();
        }

        private static void PrepareAndWrite(int slotId, PendingSlotSave pending)
        {
            List<SpeechEventCapturedRecord>? records;
            string speechPath;
            lock (pending)
            {
                records = pending.LatestRecords;
                speechPath = pending.SpeechPath;
                pending.LatestRecords = null;
            }

            try
            {
                if (records == null)
                {
                    return;
                }

                SpeechEventSaveSnapshot snapshot = SpeechEventFileStore.BuildSnapshot(speechPath, records);
                WriteSnapshot(snapshot);
            }
            catch (Exception ex)
            {
                ModLog.Error(Feature, $"Background slot {slotId} save failed: {ex}");
            }

            ScheduleRetryIfNeeded(slotId, pending);
        }

        private static void ScheduleRetryIfNeeded(int slotId, PendingSlotSave pending)
        {
            lock (pending)
            {
                if (pending.LatestRecords == null)
                {
                    pending.PrepareTask = null;
                    _ = InFlightBySlot.TryRemove(slotId, out _);
                    return;
                }

                pending.PrepareTask = Task.Run(() => PrepareAndWrite(slotId, pending));
            }
        }

        private static void WriteSnapshot(SpeechEventSaveSnapshot snapshot)
        {
            if (snapshot.SpeechBytes != null && snapshot.SpeechBytes.Length > 0)
            {
                BackgroundFileWriteQueue.EnqueueBytes(snapshot.SpeechPath, snapshot.SpeechBytes, Feature);
            }
            else
            {
                BackgroundFileWriteQueue.EnqueueDelete(snapshot.SpeechPath, Feature);
            }

            ModLog.Info(Feature, $"Queued save — speechEvents={snapshot.SerializedCount}");
        }

        private static void WaitForTask(Task? task)
        {
            TaskWaitHelper.WaitSync(task, Feature, "Background save");
        }
    }
}
