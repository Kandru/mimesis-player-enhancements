using MimesisPlayerEnhancement.Util;

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

        internal static void EnqueueSave(
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

        internal static void FlushAllSync()
        {
            BackgroundFileWriteQueue.FlushAllSync();
        }
    }
}
