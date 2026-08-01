namespace MimesisPlayerEnhancement.Features.Statistics
{
    internal static class StatisticsWriteQueue
    {
        private static int _loadedSlotId = -999;

        internal static void Configure(int slotId)
        {
            _loadedSlotId = slotId;
        }

        internal static void Clear()
        {
            _loadedSlotId = -999;
        }

        internal static void SaveLoadedSlot(bool waitForCompletion)
        {
            if (_loadedSlotId < 0)
            {
                return;
            }

            StatisticsStore.SaveSlot(_loadedSlotId, waitForCompletion);
        }

        internal static void FlushAllSync()
        {
            StatisticsTracker.PersistLoadedSlot(waitForCompletion: true);
            StatisticsStore.FlushAllSync();
            BackgroundFileWriteQueue.FlushAllSync();
        }
    }
}
