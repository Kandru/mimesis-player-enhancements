using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Mimic.Voice.SpeechSystem;

namespace MimesisPlayerEnhancement.Features.Persistence
{
    internal sealed class SpeechEventSaveSnapshot
    {
        internal string SpeechPath = string.Empty;
        internal byte[]? SpeechBytes;
        internal string MetadataPath = string.Empty;
        internal string MetadataJson = string.Empty;
        internal int SerializedCount;
    }

    internal static class PersistenceWriteQueue
    {
        private const string Feature = "Persistence";

        private static readonly ConcurrentDictionary<int, PendingSlotSave> InFlightBySlot = new();

        private sealed class PendingSlotSave
        {
            internal List<SpeechEvent>? LatestEvents;
            internal string PlayerMappingPath = string.Empty;
            internal string PlayerMappingJson = string.Empty;
            internal Task? PrepareTask;
        }

        /// <summary>
        /// Captures speech events on the game thread, then serializes and writes on a worker.
        /// </summary>
        internal static void EnqueueSave(
            int slotId,
            List<SpeechEvent> speechEvents,
            string playerMappingPath,
            string playerMappingJson)
        {
            PendingSlotSave pending = InFlightBySlot.GetOrAdd(slotId, static _ => new PendingSlotSave());
            lock (pending)
            {
                pending.LatestEvents = speechEvents;
                pending.PlayerMappingPath = playerMappingPath;
                pending.PlayerMappingJson = playerMappingJson;

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
            string playerMappingPath;
            string playerMappingJson;
            lock (pending)
            {
                events = pending.LatestEvents;
                playerMappingPath = pending.PlayerMappingPath;
                playerMappingJson = pending.PlayerMappingJson;
                pending.LatestEvents = null;
            }

            try
            {
                if (events == null)
                {
                    return;
                }

                SpeechEventSaveSnapshot snapshot = SpeechEventFileStore.Serialize(slotId, events);
                WriteSnapshot(slotId, snapshot, playerMappingPath, playerMappingJson);
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

        private static void WriteSnapshot(
            int slotId,
            SpeechEventSaveSnapshot snapshot,
            string playerMappingPath,
            string playerMappingJson)
        {
            if (snapshot.SpeechBytes != null && snapshot.SpeechBytes.Length > 0)
            {
                BackgroundFileWriteQueue.EnqueueBytes(snapshot.SpeechPath, snapshot.SpeechBytes, Feature);
            }
            else
            {
                BackgroundFileWriteQueue.EnqueueDelete(snapshot.SpeechPath, Feature);
            }

            BackgroundFileWriteQueue.EnqueueText(snapshot.MetadataPath, snapshot.MetadataJson, Feature);
            BackgroundFileWriteQueue.EnqueueText(playerMappingPath, playerMappingJson, Feature);

            ModLog.Info(Feature, $"Queued slot {slotId} save — speechEvents={snapshot.SerializedCount}");
        }

        private static void WaitForTask(Task? task)
        {
            if (task == null || task.IsCompleted)
            {
                return;
            }

            try
            {
                _ = task.Wait(TimeSpan.FromSeconds(30));
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Background save wait failed: {ex.Message}");
            }
        }
    }
}
