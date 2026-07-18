using MimesisPlayerEnhancement.Features.Persistence.Patches;

namespace MimesisPlayerEnhancement.Features.Persistence
{
    internal static class PersistenceRuntime
    {
        private const string Feature = "Persistence";

        private static bool _wasEnabled;

        internal static void RefreshFromConfig()
        {
            bool enabled = ModConfig.EnablePersistence.Value;
            if (_wasEnabled && !enabled)
            {
                SpeechEventPoolManager.OnSessionEnded();
                SpeechEventArchivePatches.InvalidatePoolLoaded();
                ModLog.Debug(Feature, "Persistence disabled — cleared voice pool runtime state.");
            }

            _wasEnabled = enabled;

            if (!enabled || !MimesisSaveManager.IsHost())
            {
                return;
            }

            int slotId = MimesisSaveManager.GetCurrentSaveSlotId();
            if (!MimesisSaveManager.IsValidSaveSlotId(slotId))
            {
                return;
            }

            SpeechEventArchivePatches.EnsurePoolLoaded(slotId);
            SpeechEventPoolManager.ProcessDeferredUpdates();
            RestoreActiveArchives(slotId);
            ModLog.Debug(Feature, $"Persistence enabled — restored voice pool for slot {slotId}");
        }

        private static void RestoreActiveArchives(int slotId)
        {
            foreach (SpeechEventArchive archive in SpeechEventArchiveRegistry.EnumerateActive())
            {
                if (archive == null || archive.IsLocal)
                {
                    continue;
                }

                TryRestoreArchive(archive, slotId);
            }
        }

        internal static void TryRestoreArchive(SpeechEventArchive archive, int slotId)
        {
            if (archive.events == null)
            {
                ModLog.Warn(Feature, $"Player archive has no event list — {VoiceEventStats.DescribePlayer(archive)}");
                return;
            }

            SpeechEventPoolManager.ArchiveRestoreOutcome outcome = SpeechEventPoolManager.TryRestoreToArchive(archive);
            SpeechEventPoolManager.ApplyRestoreOutcome(archive, outcome, flush: true);

            (int pendingCount, int injectedCount) = SpeechEventPoolManager.GetCounts();
            ModLog.Debug(
                Feature,
                $"Archive detail — slot={slotId} poolState={pendingCount}P/{injectedCount}I disconnectedCache={SpeechEventPoolManager.DisconnectedCacheCount}");
        }
    }
}
