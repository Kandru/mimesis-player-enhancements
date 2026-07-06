using System;
using Mimic.Voice.SpeechSystem;

namespace MimesisPlayerEnhancement.Features.Persistence.Patches
{
    [HarmonyPatch(typeof(SpeechEventArchive), "OnStartClient")]
    public static class SpeechEventArchivePatches
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
                    if (ModConfig.EnablePersistence.Value)
                    {
                        ModLog.Debug(Feature, $"Archive started (non-host) — {VoiceEventStats.DescribePlayerBrief(__instance)}");
                    }

                    return;
                }

                int slotId = MimesisSaveManager.GetCurrentSaveSlotId();
                if (!MimesisSaveManager.IsValidSaveSlotId(slotId))
                {
                    if (ModConfig.EnablePersistence.Value)
                    {
                        ModLog.Debug(Feature, $"Archive started outside save slot — {VoiceEventStats.DescribePlayerBrief(__instance)}");
                    }

                    return;
                }

                if (ModConfig.EnablePersistence.Value)
                {
                    HandlePersistence(__instance, slotId);
                }

                if (ModConfig.EnableStatistics.Value
                    && !JoinAnytime.JoinAnytimePlayerRegistration.ShouldDeferRegistration(__instance.PlayerUID))
                {
                    StatisticsTracker.HandleArchiveStarted(__instance, slotId);
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

        private static void HandlePersistence(SpeechEventArchive __instance, int slotId)
        {
            EnsurePoolLoaded(slotId);

            if (__instance.IsLocal)
            {
                SpeechEventPoolManager.SetLocalArchive(__instance);
            }

            if (__instance.events == null)
            {
                ModLog.Warn(Feature, $"Player archive has no event list — {VoiceEventStats.DescribePlayer(__instance)}");
                return;
            }

            SpeechEventPoolManager.ArchiveRestoreOutcome outcome = SpeechEventPoolManager.TryRestoreToArchive(__instance);
            SpeechEventPoolManager.ApplyRestoreOutcome(__instance, outcome);

            (int pendingCount, int injectedCount) = SpeechEventPoolManager.GetCounts();
            ModLog.Debug(Feature, $"Archive detail — slot={slotId} poolState={pendingCount}P/{injectedCount}I disconnectedCache={SpeechEventPoolManager.DisconnectedCacheCount}");
        }

        internal static PersistenceConnectOutcome BuildConnectOutcome(
            SpeechEventInjector.RestoreResult result,
            int eventsBefore,
            int eventsAfter,
            SpeechEventArchive? archive = null)
        {
            if (result.TotalAdded > 0)
            {
                return new PersistenceConnectOutcome(
                    PersistenceConnectPhase.Connected,
                    $"restored {result.TotalAdded} voice events (pool={result.FromPool}, reconnect={result.FromReconnect}, before={eventsBefore}, after={eventsAfter})");
            }

            if (SpeechEventPoolManager.HasPending() || SpeechEventPoolManager.DisconnectedCacheCount > 0)
            {
                MaybeWarnNoMatchingVoices(archive);
                return new PersistenceConnectOutcome(PersistenceConnectPhase.Connected, "no matching saved voices");
            }

            return new PersistenceConnectOutcome(PersistenceConnectPhase.Connected, "no persistence data");
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

        internal static void EnsurePoolLoaded(int slotId)
        {
            if (slotId == _poolLoadedForSlot)
            {
                return;
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
