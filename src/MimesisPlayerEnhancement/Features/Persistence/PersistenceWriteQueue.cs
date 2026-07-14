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
            internal List<SpeechEvent>? LatestEvents;
            internal Task? PrepareTask;
        }

        /// <summary>
        /// Captures speech events on the game thread, then serializes and writes on a worker.
        /// </summary>
        internal static void EnqueueSave(int slotId, List<SpeechEvent> speechEvents)
        {
            PendingSlotSave pending = InFlightBySlot.GetOrAdd(slotId, static _ => new PendingSlotSave());
            lock (pending)
            {
                pending.LatestEvents = speechEvents;

                if (pending.PrepareTask is { IsCompleted: false })
                {
                    ModLog.Debug(Feature, $"Coalescing slot {slotId} save — serialize already in flight.");
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
            List<SpeechEvent>? events;
            lock (pending)
            {
                events = pending.LatestEvents;
                pending.LatestEvents = null;
            }

            try
            {
                if (events == null)
                {
                    return;
                }

                SpeechEventSaveSnapshot snapshot = SpeechEventFileStore.Serialize(slotId, events);
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
                if (pending.LatestEvents == null)
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
