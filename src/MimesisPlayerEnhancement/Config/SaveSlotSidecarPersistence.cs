using System.Diagnostics;
using MimesisPlayerEnhancement.Features.Persistence.Patches;

namespace MimesisPlayerEnhancement
{
    /// <summary>
    /// Coordinates per-save sidecar load (once at save load), in-memory session state,
    /// flush on vanilla save, and sync flush on mod unload.
    /// </summary>
    internal static class SaveSlotSidecarPersistence
    {
        private const string Feature = "SaveSlotSidecar";

        private static int _loadingSidecarSlot = -1;

        internal static void OnSaveSlotLoaded(int slotId)
        {
            if (!MimesisSaveManager.IsHost() || !MimesisSaveManager.IsValidSaveSlotId(slotId))
            {
                return;
            }

            if (_loadingSidecarSlot == slotId)
            {
                ModLog.Debug(Feature, $"Skipping reentrant sidecar load for save slot {slotId}.");
                return;
            }

            _loadingSidecarSlot = slotId;
            try
            {
                SaveSlotDocumentStore.LoadForSlot(slotId);
                PlayerRegistry.LoadForSlot(slotId, forceReload: true);
                SaveSlotConfigStore.LoadForSlot(slotId);
                SpeechEventArchivePatches.EnsurePoolLoaded(slotId);

                Features.JoinAnytime.JoinAnytimeLobbyController.OnSaveSlotSidecarLoaded(slotId);

                int statsPlayers = PlayerRegistry.GetAllStatistics().Count;
                int rosterPlayers = SaveSlotDocumentStore.LoadedPlayerCount;
                ModLog.Info(
                    Feature,
                    $"Loaded slot sidecars for save slot {slotId} — stats={statsPlayers}, roster={rosterPlayers}.");

                WebDashboardOfflinePlayerCache.RebuildSync(PlayerRegistry.Revision);
                WebDashboardSnapshotCache.MarkDirty();
                WebDashboardSnapshotCache.RequestFullPublish();
            }
            finally
            {
                if (_loadingSidecarSlot == slotId)
                {
                    _loadingSidecarSlot = -1;
                }
            }
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

        internal static void OnGameSaved(int slotId, IReadOnlyList<string>? playerNames, bool isAutoSave)
        {
            if (!MimesisSaveManager.IsHost() || !MimesisSaveManager.IsValidSaveSlotId(slotId))
            {
                return;
            }

            EnsureSlotBoundForSave(slotId);

            bool waitForCompletion = !isAutoSave;
            Stopwatch? saveFlushTimer = waitForCompletion && ModConfig.EnableDebugLogging.Value
                ? Stopwatch.StartNew()
                : null;

            SpeechEventPoolManager.ProcessDeferredUpdates();

            PlayerRegistry.MergeLiveSessionRoster();
            SpeechEventPoolManager.SyncVoiceMappingsToDocument();
            PlayerRegistry.SyncRosterToDocument();
            MimesisSaveManager.SaveMimesisData(slotId);

            PlayerRegistry.PersistStatistics(waitForCompletion);
            SaveSlotConfigStore.ForceFlushToDisk(slotId, waitForCompletion);
            SaveSlotDocumentStore.CaptureLobbyFromController(slotId);
            SaveSlotDocumentStore.ForceFlushToDisk(slotId, waitForCompletion);

            if (waitForCompletion)
            {
                PersistenceWriteQueue.FlushAllSync();
                BackgroundFileWriteQueue.FlushAllSync();
            }

            if (saveFlushTimer != null)
            {
                saveFlushTimer.Stop();
                ModLog.Debug(
                    Feature,
                    $"Manual save sidecar flush completed in {saveFlushTimer.ElapsedMilliseconds} ms — slot={slotId}.");
            }

            ModLog.Info(Feature, $"Persisted slot sidecars for save slot {slotId} (auto={isAutoSave}).");
        }

        private static void EnsureSlotBoundForSave(int slotId)
        {
            if (SaveSlotDocumentStore.LoadedSlotId == slotId && SaveSlotConfigStore.ActiveSlotId == slotId)
            {
                return;
            }

            ModLog.Info(Feature, $"Late-binding sidecar stores for save slot {slotId} at save time.");
            OnSaveSlotLoaded(slotId);
        }

        internal static void OnSessionEnded()
        {
            _loadingSidecarSlot = -1;
            PlayerRegistry.ResetSessionRuntimeState();
            SpeechEventArchivePatches.InvalidatePoolLoaded();
            SaveSlotConfigStore.ClearRuntimeToGlobal();
            SaveSlotDocumentStore.Clear();
        }

        internal static void FlushAllSync()
        {
            int activeSlotId = SaveSlotConfigStore.ActiveSlotId;
            if (activeSlotId >= 0)
            {
                SpeechEventPoolManager.ProcessDeferredUpdates();

                SpeechEventPoolManager.SyncVoiceMappingsToDocument();

                PlayerRegistry.PersistStatistics(waitForCompletion: true);
                SaveSlotConfigStore.ForceFlushToDisk(activeSlotId, waitForCompletion: true);
                SaveSlotDocumentStore.CaptureLobbyFromController(activeSlotId);
                SaveSlotDocumentStore.ForceFlushToDisk(activeSlotId, waitForCompletion: true);
            }

            PersistenceWriteQueue.FlushAllSync();
            StatisticsStore.FlushAllSync();
            BackgroundFileWriteQueue.FlushAllSync();
        }
    }
}
