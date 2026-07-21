namespace MimesisPlayerEnhancement.Features.Persistence.Patches
{
    [HarmonyPatch(typeof(SpeechEventArchive), "OnStartClient")]
    internal static class SpeechEventArchivePatches
    {
        private const string Feature = "Persistence";

        private static int _poolLoadedForSlot = -999;

        [HarmonyPostfix]
        public static void Postfix(SpeechEventArchive __instance)
        {
            try
            {
                SpeechEventArchiveRegistry.Register(__instance);

                if (!MimesisSaveManager.IsHost())
                {
                    ModLog.Debug(Feature, $"Archive started (non-host) — {VoiceEventStats.DescribePlayerBrief(__instance)}");
                    return;
                }

                int slotId = MimesisSaveManager.GetCurrentSaveSlotId();
                if (!MimesisSaveManager.IsValidSaveSlotId(slotId))
                {
                    ModLog.Debug(Feature, $"Archive started outside save slot — {VoiceEventStats.DescribePlayerBrief(__instance)}");
                    return;
                }

                EnsurePoolLoaded(slotId);

                if (__instance.IsLocal)
                {
                    SpeechEventPoolManager.SetLocalArchive(__instance);
                }

                if (ModConfig.EnablePersistence.Value)
                {
                    PersistenceRuntime.TryRestoreArchive(__instance, slotId);
                }

                if (ModConfig.EnableStatistics.Value
                    && !JoinAnytime.JoinAnytimePlayerRegistration.ShouldDeferRegistration(__instance.PlayerUID))
                {
                    PlayerPresenceEvents.OnArchiveStarted(__instance, slotId);
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"SpeechEventArchive inject failed: {ex.Message}");
            }
            finally
            {
                PlayerLifecycleCoordinator.FinishArchiveConnect(__instance);
            }
        }

        internal static PersistenceConnectOutcome BuildConnectOutcome(
            SpeechEventInjector.RestoreResult result,
            SpeechEventArchive? archive = null)
        {
            return BuildConnectOutcome(
                result,
                SpeechEventPoolManager.HasPending(),
                SpeechEventPoolManager.DisconnectedCacheCount,
                archive);
        }

        internal static PersistenceConnectOutcome BuildConnectOutcome(
            SpeechEventInjector.RestoreResult result,
            bool hasPending,
            int disconnectedCacheCount,
            SpeechEventArchive? archive = null)
        {
            if (result.TotalAdded > 0)
            {
                return new PersistenceConnectOutcome(PersistenceConnectPhase.Connected, null);
            }

            if (hasPending || disconnectedCacheCount > 0)
            {
                MaybeWarnNoMatchingVoices(archive);
                return new PersistenceConnectOutcome(PersistenceConnectPhase.Connected, "unmatched saved voices");
            }

            return new PersistenceConnectOutcome(PersistenceConnectPhase.Connected, null);
        }

        private static void MaybeWarnNoMatchingVoices(SpeechEventArchive? archive)
        {
            if (archive == null)
            {
                return;
            }

            if (!VoiceEventStats.TryCaptureArchiveIdentity(archive, out _, out _, out ulong steamId)
                || steamId == 0)
            {
                return;
            }

            if (!SpeechEventPoolManager.HasDisconnectedCacheForSteam(steamId))
            {
                return;
            }

            ModLog.Warn(
                Feature,
                $"No matching saved voices while disconnect cache holds entries — {VoiceEventStats.DescribePlayerBrief(archive)}");
        }

        internal static void InvalidatePoolLoaded()
        {
            _poolLoadedForSlot = -999;
        }

        internal static void EnsurePoolLoaded(int slotId)
        {
            if (!ModConfig.EnablePersistence.Value)
            {
                return;
            }

            if (slotId == _poolLoadedForSlot)
            {
                // Pool may have been reset after save (e.g. returning to menu) while disk now has data.
                if (SpeechEventPoolManager.TotalCount > 0 || !MimesisSaveManager.HasMimesisData(slotId))
                {
                    return;
                }
            }

            _poolLoadedForSlot = slotId;
            SpeechEventPoolManager.Reset();

            if (MimesisSaveManager.HasMimesisData(slotId))
            {
                SpeechEventPoolManager.LoadForSlot(slotId);
                ModLog.Info(Feature, $"Loaded persisted voice pool for save slot {slotId} ({SpeechEventPoolManager.TotalCount} events).");
            }
            else
            {
                ModLog.Debug(Feature, $"No persisted voice data for save slot {slotId}.");
            }
        }
    }
}
