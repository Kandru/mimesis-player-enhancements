using System;
using System.Collections.Generic;
using HarmonyLib;
using Mimic.Voice.SpeechSystem;
using ReluProtocol;

namespace MimesisPlayerEnhancement.Features.Persistence.Patches
{
    /// <summary>
    /// Runs for ALL SpeechEventArchive instances (not just local).
    /// Uses SpeechEventPoolManager for 3-state event distribution.
    /// </summary>
    [HarmonyPatch(typeof(SpeechEventArchive), "OnStartClient")]
    public static class SpeechEventArchivePatches
    {
        private const string Feature = "Persistence";

        private static int _poolLoadedForSlot = -999;

        [HarmonyPostfix]
        public static void Postfix(SpeechEventArchive __instance)
        {
            if (!ModConfig.EnablePersistence.Value)
                return;

            try
            {
                if (!MimesisSaveManager.IsHost())
                {
                    ModLog.Debug(Feature, $"Archive started (non-host) — {VoiceEventStats.DescribePlayer(__instance)}");
                    return;
                }

                int slotId = MimesisSaveManager.GetCurrentSaveSlotId();
                if (!MMSaveGameData.CheckSaveSlotID(slotId, true))
                {
                    ModLog.Debug(Feature, $"Archive started outside save slot — {VoiceEventStats.DescribePlayer(__instance)}");
                    return;
                }

                if (slotId != _poolLoadedForSlot)
                {
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

                if (__instance.IsLocal)
                    SpeechEventPoolManager.SetLocalArchive(__instance);

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

                int eventsBefore = VoiceEventStats.GetEventCount(__instance);

                if (!isLocal && string.IsNullOrEmpty(playerId) && playerUID == 0)
                {
                    bool hasDataToInject = SpeechEventPoolManager.HasPending()
                                           || SpeechEventPoolManager.DisconnectedCacheCount > 0;
                    if (hasDataToInject)
                    {
                        SpeechEventPoolManager.RegisterDeferredInjection(__instance);
                        ModLog.Info(
                            Feature,
                            $"Player connecting — {VoiceEventStats.DescribePlayer(__instance)} — " +
                            $"voice injection deferred (pendingPool={SpeechEventPoolManager.GetCounts().pending}, " +
                            $"disconnectedCache={SpeechEventPoolManager.DisconnectedCacheCount})");
                    }
                    else
                    {
                        ModLog.Info(
                            Feature,
                            $"Player connecting — {VoiceEventStats.DescribePlayer(__instance)} — awaiting identity sync");
                    }

                    return;
                }

                var eventsList = __instance.events;
                if (eventsList == null)
                {
                    ModLog.Warn(Feature, $"Player archive has no event list — {VoiceEventStats.DescribePlayer(__instance)}");
                    return;
                }

                float currentTime = SpeechEventPoolManager.GetCurrentSessionTime();
                var seenIds = new HashSet<long>();
                for (int i = 0; i < eventsList.Count; i++)
                    seenIds.Add(eventsList[i].Id);

                int fromPool = 0;
                int fromReconnect = 0;

                if (SpeechEventPoolManager.HasPending())
                {
                    List<SpeechEvent> claimed = SpeechEventPoolManager.ClaimEventsForArchive(
                        playerId, playerUID, isLocal, __instance);
                    if (claimed != null && claimed.Count > 0)
                    {
                        foreach (SpeechEvent ev in claimed)
                        {
                            if (ev == null || seenIds.Contains(ev.Id))
                                continue;
                            SpeechEventPoolManager.FixEventTiming(ev, currentTime);
                            eventsList.Add(ev);
                            seenIds.Add(ev.Id);
                            fromPool++;
                        }
                    }
                }

                if (SpeechEventPoolManager.DisconnectedCacheCount > 0)
                {
                    List<SpeechEvent> reclaimed = SpeechEventPoolManager.ClaimDisconnectedEventsForArchive(
                        playerId, playerUID, isLocal);
                    if (reclaimed != null && reclaimed.Count > 0)
                    {
                        foreach (SpeechEvent ev in reclaimed)
                        {
                            if (ev == null || seenIds.Contains(ev.Id))
                                continue;
                            SpeechEventPoolManager.FixEventTiming(ev, currentTime);
                            eventsList.Add(ev);
                            seenIds.Add(ev.Id);
                            fromReconnect++;
                        }
                    }
                }

                int totalAdded = fromPool + fromReconnect;
                int eventsAfter = VoiceEventStats.GetEventCount(__instance);

                if (totalAdded > 0)
                {
                    ModLog.Info(
                        Feature,
                        $"Player connected — {VoiceEventStats.DescribePlayer(__instance)} — " +
                        $"restored {totalAdded} voice events (pool={fromPool}, reconnect={fromReconnect}, " +
                        $"before={eventsBefore}, after={eventsAfter})");
                }
                else if (SpeechEventPoolManager.HasPending() || SpeechEventPoolManager.DisconnectedCacheCount > 0)
                {
                    ModLog.Info(
                        Feature,
                        $"Player connected — {VoiceEventStats.DescribePlayer(__instance)} — no matching saved voices");
                }
                else
                {
                    ModLog.Info(
                        Feature,
                        $"Player connected — {VoiceEventStats.DescribePlayer(__instance)} — no persistence data");
                }

                var counts = SpeechEventPoolManager.GetCounts();
                ModLog.Debug(
                    Feature,
                    $"Archive detail — slot={slotId} time={currentTime:F1} poolState={counts.pending}P/{counts.injected}I/0F " +
                    $"disconnectedCache={SpeechEventPoolManager.DisconnectedCacheCount}");
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"SpeechEventArchive inject failed: {ex.Message}");
            }
        }

    }
}
