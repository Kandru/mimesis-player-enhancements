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
            SaveSlotDocumentStore.LoadForSlot(slotId);
            SaveSlotConfigStore.LoadForSlot(slotId);
            Features.JoinAnytime.JoinAnytimeLobbyController.OnSaveSlotSidecarLoaded(slotId);

            int statsPlayers = StatisticsTracker.GetCachedPlayerDocumentsView().Count;
            int rosterPlayers = SaveSlotDocumentStore.LoadedPlayerCount;
            ModLog.Info(
                Feature,
                $"Loaded slot sidecars for save slot {slotId} — stats={statsPlayers}, roster={rosterPlayers}.");

            WebDashboardOfflinePlayerCache.RebuildSync(StatisticsTracker.Revision);
            WebDashboardSnapshotCache.MarkDirty();
            WebDashboardSnapshotCache.RequestFullPublish();
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

            int activeSlotId = SaveSlotConfigStore.ActiveSlotId;
            if (activeSlotId >= 0)
            {
                if (activeSlotId == slotId)
                {
                    return;
                }

                ModLog.Debug(
                    Feature,
                    $"Ignoring sidecar reload — active slot {activeSlotId}, requested {slotId}.");
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
            SaveSlotDocumentStore.CaptureLobbyFromController(slotId);
            SaveSlotDocumentStore.FlushToDisk(slotId, waitForCompletion: false);
            ModLog.Debug(Feature, $"Queued slot sidecar flush for save slot {slotId}.");
        }

        internal static void OnSessionEnded()
        {
            StatisticsTracker.OnSessionEnded();
            SaveSlotConfigStore.ClearRuntimeToGlobal();
            SaveSlotDocumentStore.Clear();
            Features.JoinAnytime.JoinAnytimeLobbyController.OnSessionEnded();
        }

        internal static void FlushAllSync()
        {
            int activeSlotId = SaveSlotConfigStore.ActiveSlotId;
            if (activeSlotId >= 0)
            {
                StatisticsTracker.PersistLoadedSlot(waitForCompletion: true);
                SaveSlotConfigStore.FlushToDisk(activeSlotId, waitForCompletion: true);
                SaveSlotDocumentStore.CaptureLobbyFromController(activeSlotId);
                SaveSlotDocumentStore.FlushToDisk(activeSlotId, waitForCompletion: true);
            }

            StatisticsStore.FlushAllSync();
            BackgroundFileWriteQueue.FlushAllSync();
        }
    }
}
