namespace MimesisPlayerEnhancement.Features.Persistence.Patches
{
    // game@0.3.1 Assembly-CSharp/Mimic.Voice.SpeechSystem/SpeechEventArchive.cs:L111-126
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

    // game@0.3.1 Assembly-CSharp/Mimic.Voice.SpeechSystem/SpeechEventArchive.cs:L128-140
    [HarmonyPatch(typeof(SpeechEventArchive), nameof(SpeechEventArchive.OnStopClient))]
    internal static class SpeechEventArchiveDisconnectPatches
    {
        private const string Feature = "Persistence";

        [HarmonyPrefix]
        public static void Prefix(SpeechEventArchive __instance)
        {
            SpeechEventArchiveRegistry.Unregister(__instance);
            PlayerLifecycleCoordinator.ClearConnectState(__instance);

            try
            {
                bool isLocal = false;
                try
                {
                    isLocal = __instance.IsLocal;
                }
                catch
                {
                    /* Player ref may be gone */
                }

                if (!SpeechEventMatchResolver.ShouldCacheDisconnectEvents(
                        ModConfig.EnablePersistence.Value,
                        MimesisSaveManager.IsHost(),
                        isLocal))
                {
                    return;
                }

                ulong steamId = 0;
                long playerUID = 0;
                _ = VoiceEventStats.TryCaptureArchiveIdentity(__instance, out playerUID, out _, out steamId);

                if (JoinAnytime.JoinAnytimePlayerRegistration.ShouldDeferRegistration(playerUID))
                {
                    return;
                }

                int cached = SpeechEventPoolManager.CacheEventsFromArchive(__instance, steamId, playerUID);
                PlayerLifecycleCoordinator.OnArchiveDisconnecting(
                    __instance,
                    new PlayerLifecycleContribution(Feature, $"cached {cached} voice events"),
                    steamId,
                    playerUID);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Disconnect cache error: {ex.Message}");
            }
        }
    }
}
