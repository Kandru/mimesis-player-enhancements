using MimesisPlayerEnhancement.Features.WebDashboard;

namespace MimesisPlayerEnhancement
{
    /// <summary>
    /// Coordinates per-save sidecar load (once at save load), in-memory session state,
    /// flush on vanilla save, and sync flush on mod unload.
    /// </summary>
    internal static class SaveSlotSidecarPersistence
    {
        private const string Feature = "SaveSlotSidecar";

        internal static void OnSaveSlotLoaded(int slotId)
        {
            if (!MimesisSaveManager.IsHost() || !MimesisSaveManager.IsValidSaveSlotId(slotId))
            {
                return;
            }

            StatisticsTracker.LoadForSlot(slotId);
            SaveSlotConfigStore.LoadForSlot(slotId);
            WebDashboardPlayerNameStore.LoadForSlot(slotId);
            JoinAnytimeLobbyStore.LoadForSlot(slotId);
            JoinAnytimeLobbyController.OnSaveSlotSidecarLoaded(slotId);
            ModLog.Debug(Feature, $"Loaded slot sidecars for save slot {slotId}.");
        }

        /// <summary>
        /// Loads sidecars when the host session is active but ApplyLoadedGameData ran before host was ready.
        /// </summary>
        internal static void EnsureSaveSlotLoaded(int slotId)
        {
            if (!MimesisSaveManager.IsHost() || !MimesisSaveManager.IsValidSaveSlotId(slotId))
            {
                return;
            }

            if (SaveSlotConfigStore.ActiveSlotId == slotId)
            {
                JoinAnytimeLobbyStore.EnsureSlotBound(slotId);
                JoinAnytimeLobbyController.OnSaveSlotSidecarLoaded(slotId);
                return;
            }

            OnSaveSlotLoaded(slotId);
        }

        internal static void OnGameSaved(int slotId)
        {
            if (!MimesisSaveManager.IsHost() || !MimesisSaveManager.IsValidSaveSlotId(slotId))
            {
                return;
            }

            StatisticsTracker.OnGameSaved(slotId);
            SaveSlotConfigStore.FlushToDisk(slotId, waitForCompletion: false);
            WebDashboardPlayerNameStore.FlushToDisk(slotId, waitForCompletion: false);
            JoinAnytimeLobbyStore.CaptureFromController(slotId);
            JoinAnytimeLobbyStore.FlushToDisk(slotId, waitForCompletion: false);
            ModLog.Debug(Feature, $"Queued slot sidecar flush for save slot {slotId}.");
        }

        internal static void OnSessionEnded()
        {
            StatisticsTracker.OnSessionEnded();
            SaveSlotConfigStore.ClearRuntimeToGlobal();
            WebDashboardPlayerNameStore.Clear();
            JoinAnytimeLobbyStore.Clear();
            JoinAnytimeLobbyController.OnSessionEnded();
        }

        internal static void FlushAllSync()
        {
            int activeSlotId = SaveSlotConfigStore.ActiveSlotId;
            if (activeSlotId >= 0)
            {
                StatisticsTracker.PersistLoadedSlot(waitForCompletion: true);
                SaveSlotConfigStore.FlushToDisk(activeSlotId, waitForCompletion: true);
                WebDashboardPlayerNameStore.FlushToDisk(activeSlotId, waitForCompletion: true);
                JoinAnytimeLobbyStore.CaptureFromController(activeSlotId);
                JoinAnytimeLobbyStore.FlushToDisk(activeSlotId, waitForCompletion: true);
            }

            StatisticsStore.FlushAllSync();
            BackgroundFileWriteQueue.FlushAllSync();
        }
    }
}
