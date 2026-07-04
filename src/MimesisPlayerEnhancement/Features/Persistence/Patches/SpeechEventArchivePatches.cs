using System;
using System.Collections.Generic;
using HarmonyLib;
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
                        ModLog.Debug(Feature, $"Archive started (non-host) — {VoiceEventStats.DescribePlayer(__instance)}");
                    }

                    return;
                }

                int slotId = MimesisSaveManager.GetCurrentSaveSlotId();
                if (!MimesisSaveManager.IsValidSaveSlotId(slotId))
                {
                    if (ModConfig.EnablePersistence.Value)
                    {
                        ModLog.Debug(Feature, $"Archive started outside save slot — {VoiceEventStats.DescribePlayer(__instance)}");
                    }

                    return;
                }

                if (ModConfig.EnablePersistence.Value)
                {
                    HandlePersistence(__instance, slotId);
                }

                if (ModConfig.EnableStatistics.Value)
                {
                    StatisticsTracker.HandleArchiveStarted(__instance, slotId);
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"SpeechEventArchive inject failed: {ex.Message}");
            }
        }

        private static void HandlePersistence(SpeechEventArchive __instance, int slotId)
        {
            EnsurePoolLoaded(slotId);

            if (__instance.IsLocal)
            {
                SpeechEventPoolManager.SetLocalArchive(__instance);
            }

            string? playerId = null;
            long playerUID = 0;
            bool isLocal = false;

            try
            {
                playerId = __instance.PlayerId;
                playerUID = __instance.PlayerUID;
                isLocal = __instance.IsLocal;
            }
            catch
            {
                /* Player component may not be ready yet */
            }

            int eventsBefore = VoiceEventStats.GetVoiceLineCount(__instance);

            if (!isLocal && string.IsNullOrEmpty(playerId) && playerUID == 0)
            {
                bool hasDataToInject = SpeechEventPoolManager.HasPending()
                                       || SpeechEventPoolManager.DisconnectedCacheCount > 0;
                if (hasDataToInject)
                {
                    SpeechEventPoolManager.RegisterDeferredInjection(__instance);
                    (int pending, _) = SpeechEventPoolManager.GetCounts();
                    ModLog.Info(Feature, $"Player connecting — {VoiceEventStats.DescribePlayer(__instance)} — " +
                        $"voice injection deferred (pendingPool={pending}, " +
                        $"disconnectedCache={SpeechEventPoolManager.DisconnectedCacheCount})");
                }
                else
                {
                    ModLog.Info(Feature, $"Player connecting — {VoiceEventStats.DescribePlayer(__instance)} — awaiting identity sync");
                }

                return;
            }

            if (__instance.events == null)
            {
                ModLog.Warn(Feature, $"Player archive has no event list — {VoiceEventStats.DescribePlayer(__instance)}");
                return;
            }

            SpeechEventInjector.RestoreResult result = SpeechEventInjector.RestoreIntoArchive(
                __instance, playerId, playerUID, isLocal);

            int eventsAfter = VoiceEventStats.GetVoiceLineCount(__instance);

            if (result.TotalAdded > 0)
            {
                ModLog.Info(Feature, $"Player connected — {VoiceEventStats.DescribePlayer(__instance)} — " +
                    $"restored {result.TotalAdded} voice events (pool={result.FromPool}, reconnect={result.FromReconnect}, " +
                    $"before={eventsBefore}, after={eventsAfter})");
            }
            else if (SpeechEventPoolManager.HasPending() || SpeechEventPoolManager.DisconnectedCacheCount > 0)
            {
                ModLog.Info(Feature, $"Player connected — {VoiceEventStats.DescribePlayer(__instance)} — no matching saved voices");
            }
            else
            {
                ModLog.Info(Feature, $"Player connected — {VoiceEventStats.DescribePlayer(__instance)} — no persistence data");
            }

            StatisticsTracker.SyncVoiceBaseline(__instance);

            (int pendingCount, int injectedCount) = SpeechEventPoolManager.GetCounts();
            ModLog.Debug(Feature, $"Archive detail — slot={slotId} poolState={pendingCount}P/{injectedCount}I " +
                $"disconnectedCache={SpeechEventPoolManager.DisconnectedCacheCount}");
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
