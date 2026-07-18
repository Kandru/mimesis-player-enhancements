using System.Linq;
using FishNet.Object.Synchronizing;
using MimesisPlayerEnhancement.Features.Persistence.Patches;
using MimesisPlayerEnhancement.Features.Statistics.Models;

namespace MimesisPlayerEnhancement.Features.Persistence
{
    /// <summary>
    /// Manages a 2-state pool of SpeechEvents loaded from disk.
    /// Pending  -> loaded, waiting for a matching SpeechEventArchive
    /// Injected -> matched and added to the correct player's archive
    /// </summary>
    internal static class SpeechEventPoolManager
    {
        private const string Feature = "Persistence";
        private const float DeferredRetryTimeoutSec = 30f;

        public enum EventState { Pending, Injected }

        internal readonly struct ArchiveRestoreOutcome
        {
            internal ArchiveRestoreOutcome(
                SpeechEventInjector.RestoreResult restoreResult,
                bool shouldDeferRetry,
                bool identityPending)
            {
                RestoreResult = restoreResult;
                ShouldDeferRetry = shouldDeferRetry;
                IdentityPending = identityPending;
            }

            internal SpeechEventInjector.RestoreResult RestoreResult { get; }
            internal bool ShouldDeferRetry { get; }
            internal bool IdentityPending { get; }
        }

        private static readonly Dictionary<long, (SpeechEvent ev, EventState state, string originalPlayerName)> _pool
            = [];

        private static readonly Dictionary<string, List<long>> _byPlayerName = [];

        private static int _loadedSlotId = -1;

        private static SpeechEventArchive? _localArchive;

        private static readonly List<(SpeechEventArchive archive, List<SpeechEvent> events)> _deferredNameUpdates
            = [];

        private static readonly HashSet<SpeechEventArchive> _deferredInjectionArchives = [];

        private static readonly Dictionary<long, SpeechEvent> _disconnectedCache = [];

        private static readonly Dictionary<ulong, HashSet<long>> _disconnectedCacheBySteam = [];

        private static readonly Dictionary<ulong, string> _disconnectedPlayerMappings = [];

        private static readonly Dictionary<SpeechEventArchive, float> _deferredRetryStartTime = [];

        private static readonly HashSet<ulong> _restoreGiveUpWarnedSteamIds = [];

        private static readonly System.Reflection.FieldInfo? RecordedTimeField =
            AccessTools.Field(typeof(SpeechEvent), "RecordedTime");

        private static readonly System.Reflection.FieldInfo? LastPlayedTimeField =
            AccessTools.Field(typeof(SpeechEvent), "LastPlayedTime");

        internal static int LoadedSlotId => _loadedSlotId;

        public static void LoadForSlot(int slotId)
        {
            if (slotId == _loadedSlotId && _pool.Count > 0)
            {
                return;
            }

            Reset();
            _loadedSlotId = slotId;

            List<SpeechEvent>? events = MimesisSaveManager.LoadSpeechEvents(slotId);
            if (events == null || events.Count == 0)
            {
                ModLog.Debug(Feature, "No events to load for slot " + slotId);
                return;
            }

            foreach (SpeechEvent ev in events)
            {
                if (ev == null || _pool.ContainsKey(ev.Id))
                {
                    continue;
                }

                string playerName = ev.PlayerName ?? "";
                _pool[ev.Id] = (ev, EventState.Pending, playerName);

                if (!_byPlayerName.TryGetValue(playerName, out List<long>? list))
                {
                    list = [];
                    _byPlayerName[playerName] = list;
                }

                list.Add(ev.Id);
            }

            LoadVoiceMappingsFromDocument(slotId);

            IReadOnlyDictionary<ulong, string> voiceMappings = PlayerRegistry.GetVoiceMappings();
            if (_pool.Count > 0 && voiceMappings.Count == 0)
            {
                ModLog.Debug(
                    Feature,
                    $"No persisted Steam→VoiceId mappings for slot {slotId} ({_pool.Count} pooled events) — unmapped pool inference or live voice UUID sync will be used");
            }

            ModLog.Debug(Feature, $"Loaded {_pool.Count} events for slot {slotId} ({voiceMappings.Count} SteamID mappings)");
        }

        public static void SyncVoiceMappingsToDocument()
        {
            PushLiveVoiceIdsToRegistry();

            IReadOnlyDictionary<ulong, string> mapping = PlayerRegistry.GetVoiceMappings();
            if (mapping.Count == 0 && _pool.Count > 0)
            {
                ModLog.Warn(
                    Feature,
                    $"No Steam→VoiceId mappings to persist despite {_pool.Count} pooled voice events — restore will fail on next load");
            }

            PlayerRegistry.SyncRosterToDocument();
        }

        internal static void OnSessionEnded()
        {
            if (!ModConfig.EnablePersistence.Value)
            {
                Reset();
                return;
            }

            if (!MimesisSaveManager.IsHost())
            {
                Reset();
                return;
            }

            try
            {
                ProcessDeferredUpdates();
                SyncVoiceMappingsToDocument();
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"OnSessionEnded flush failed — {ex.Message}");
            }

            Reset();
            SpeechEventArchivePatches.InvalidatePoolLoaded();
        }

        internal static bool TryResolveVoiceIdForSteam(ulong steamId, out string? voiceId)
        {
            voiceId = ResolveSaveVoiceIdForSteam(steamId);
            return !string.IsNullOrEmpty(voiceId);
        }

        internal static ArchiveRestoreOutcome TryRestoreToArchive(SpeechEventArchive archive)
        {
            if (archive == null || archive.events == null)
            {
                return default;
            }

            string? playerId = null;
            long playerUID = 0;
            bool isLocal = false;

            try
            {
                playerId = archive.PlayerId;
                playerUID = archive.PlayerUID;
                isLocal = archive.IsLocal;
            }
            catch
            {
                RegisterDeferredInjection(archive);
                return new ArchiveRestoreOutcome(default, shouldDeferRetry: true, identityPending: true);
            }

            if (string.IsNullOrEmpty(playerId) && playerUID == 0)
            {
                if (HasPending() || DisconnectedCacheCount > 0)
                {
                    RegisterDeferredInjection(archive);
                    return new ArchiveRestoreOutcome(default, shouldDeferRetry: true, identityPending: true);
                }
            }

            SpeechEventInjector.RestoreResult result = SpeechEventInjector.RestoreIntoArchive(
                archive, playerId, playerUID, isLocal);

            StatisticsTracker.SyncVoiceBaseline(archive);

            bool shouldDefer = false;
            if (result.TotalAdded > 0)
            {
                ClearDeferredRetry(archive);
                SpeechEventArchiveInitialSync.RequestAfterRestore(archive, result.TotalAdded);
            }
            else
            {
                shouldDefer = ShouldDeferRestoreRetry(archive, playerId, playerUID, isLocal);
                if (shouldDefer)
                {
                    RegisterDeferredInjection(archive);
                }
                else
                {
                    MaybeWarnRestoreGiveUp(archive, playerId, playerUID, isLocal);
                    ClearDeferredRetry(archive);
                }
            }

            return new ArchiveRestoreOutcome(
                result,
                shouldDefer,
                identityPending: false);
        }

        internal static void ApplyRestoreOutcome(SpeechEventArchive archive, ArchiveRestoreOutcome outcome, bool flush = false)
        {
            if (archive == null)
            {
                return;
            }

            if (outcome.IdentityPending)
            {
                string detail = HasPending() || DisconnectedCacheCount > 0
                    ? "restoring voices"
                    : "awaiting identity";
                PlayerLifecycleCoordinator.SetPersistenceOutcome(
                    archive,
                    new PersistenceConnectOutcome(PersistenceConnectPhase.Connecting, detail),
                    flush);
                return;
            }

            if (outcome.ShouldDeferRetry)
            {
                PlayerLifecycleCoordinator.SetPersistenceOutcome(
                    archive,
                    new PersistenceConnectOutcome(PersistenceConnectPhase.Connected, "restoring voices"),
                    flush);
                return;
            }

            PersistenceConnectOutcome connectOutcome = SpeechEventArchivePatches.BuildConnectOutcome(
                outcome.RestoreResult, archive);
            PlayerLifecycleCoordinator.SetPersistenceOutcome(archive, connectOutcome, flush);
        }

        private static string ResolvePersistedVoiceId(IReadOnlyList<SpeechEvent> events)
        {
            foreach (SpeechEvent ev in events)
            {
                if (ev != null && !string.IsNullOrEmpty(ev.PlayerName))
                {
                    return ev.PlayerName;
                }
            }

            return "(unknown)";
        }

        public static List<SpeechEvent> ClaimEventsForArchive(
            string? playerId,
            long playerUID,
            bool isLocal = false,
            SpeechEventArchive? archive = null)
        {
            List<SpeechEvent> claimed = [];
            if (_pool.Count == 0)
            {
                return claimed;
            }

            HashSet<string> matchedPlayerNames = ResolveMatchedPlayerNames(playerId, playerUID, isLocal);

            foreach (string playerName in matchedPlayerNames)
            {
                if (!_byPlayerName.TryGetValue(playerName, out List<long>? eventIds))
                {
                    continue;
                }

                foreach (long id in eventIds)
                {
                    if (!_pool.TryGetValue(id, out (SpeechEvent ev, EventState state, string originalPlayerName) entry))
                    {
                        continue;
                    }

                    if (entry.state != EventState.Pending)
                    {
                        continue;
                    }

                    _pool[id] = (entry.ev, EventState.Injected, entry.originalPlayerName);
                    claimed.Add(entry.ev);
                }
            }

            if (claimed.Count > 0)
            {
                string brief = DescribeBrief(archive, playerUID, isLocal);
                string savedVoiceId = ResolvePersistedVoiceId(claimed);

                if (!string.IsNullOrEmpty(playerId))
                {
                    foreach (SpeechEvent ev in claimed)
                    {
                        ev.PlayerName = playerId;
                    }

                    ulong steamId = GameSessionAccess.ResolveSteamId(playerUID, isLocal);
                    if (steamId != 0)
                    {
                        PlayerRegistry.UpdateVoiceId(steamId, playerId);
                    }

                    LogVoiceUuidRemap(archive, claimed.Count, savedVoiceId, playerId);
                }
                else if (archive != null)
                {
                    ulong steamId = GameSessionAccess.ResolveSteamId(playerUID, isLocal);
                    if (steamId != 0 && !string.IsNullOrEmpty(savedVoiceId))
                    {
                        PlayerRegistry.UpdateVoiceId(steamId, savedVoiceId);
                    }

                    _deferredNameUpdates.Add((archive, new List<SpeechEvent>(claimed)));
                    ModLog.Debug(Feature, $"Voice UUID remap deferred — {brief} — {claimed.Count} events, saved voiceId='{savedVoiceId}'");
                }
            }
            else
            {
                (int pending, int injected) = GetCounts();
                string brief = DescribeBrief(archive, playerUID, isLocal);
                ModLog.Debug(Feature, $"No events claimed — {brief} " +
                                $"(matched names: [{string.Join(", ", matchedPlayerNames)}], pool: {pending}P/{injected}I)");
            }

            return claimed;
        }

        public static void RegisterDeferredInjection(SpeechEventArchive archive)
        {
            if (archive == null)
            {
                return;
            }

            if (!_deferredRetryStartTime.ContainsKey(archive))
            {
                _deferredRetryStartTime[archive] = GameSessionAccess.GetCurrentTickSec();
            }

            if (_deferredInjectionArchives.Add(archive))
            {
                ModLog.Debug(Feature, () =>
                    $"Deferred injection registered — {VoiceEventStats.DescribePlayerBrief(archive)} " +
                    $"(waiting for SyncVars, {_deferredInjectionArchives.Count} pending)");
            }
        }

        public static void ProcessDeferredUpdates()
        {
            if (!ModConfig.EnablePersistence.Value)
            {
                return;
            }

            if (_deferredInjectionArchives.Count == 0 && _deferredNameUpdates.Count == 0)
            {
                return;
            }

            if (_deferredInjectionArchives.Count > 0)
            {
                List<SpeechEventArchive> toProcess = [.. _deferredInjectionArchives];
                _deferredInjectionArchives.Clear();

                foreach (SpeechEventArchive archive in toProcess)
                {
                    if (archive == null)
                    {
                        continue;
                    }

                    ModLog.Debug(Feature, () =>
                        $"Deferred injection ready — {VoiceEventStats.DescribePlayerBrief(archive)}");

                    ArchiveRestoreOutcome outcome = TryRestoreToArchive(archive);
                    ApplyRestoreOutcome(archive, outcome, flush: true);
                }
            }

            if (_deferredNameUpdates.Count == 0)
            {
                return;
            }

            for (int i = _deferredNameUpdates.Count - 1; i >= 0; i--)
            {
                (SpeechEventArchive? archive, List<SpeechEvent>? events) = _deferredNameUpdates[i];

                if (archive == null)
                {
                    _deferredNameUpdates.RemoveAt(i);
                    continue;
                }

                string newPlayerId;
                try { newPlayerId = archive.PlayerId; }
                catch { continue; }

                if (string.IsNullOrEmpty(newPlayerId))
                {
                    continue;
                }

                string oldVoiceId = ResolvePersistedVoiceId(events);

                foreach (SpeechEvent ev in events)
                {
                    _ = (ev?.PlayerName = newPlayerId);
                }

                LogVoiceUuidRemap(archive, events.Count, oldVoiceId, newPlayerId);
                _deferredNameUpdates.RemoveAt(i);

                try
                {
                    ulong steamId = GameSessionAccess.ResolveSteamId(archive.PlayerUID, archive.IsLocal);
                    if (steamId != 0)
                    {
                        PlayerRegistry.UpdateVoiceId(steamId, newPlayerId);
                        string displayName = StatisticsDisplayNameResolver.Resolve(steamId, string.Empty);
                        _ = SaveSlotDocumentStore.UpsertPlayer(steamId, displayName, newPlayerId);
                    }
                }
                catch
                {
                    /* archive may be tearing down */
                }

                PlayerLifecycleCoordinator.TryFlushConnect(archive);
            }
        }

        private static string DescribeBrief(SpeechEventArchive? archive, long playerUID, bool isLocal)
        {
            return archive != null
                ? VoiceEventStats.DescribePlayerBrief(archive)
                : $"steamId={GameSessionAccess.ResolveSteamId(playerUID, isLocal)}";
        }

        private static void LogVoiceUuidRemap(SpeechEventArchive? archive, int count, string oldVoiceId, string newVoiceId)
        {
            ModLog.Debug(Feature, () =>
                $"Voice UUID remapped — {VoiceEventStats.DescribePlayerBrief(archive)} — {count} events: '{oldVoiceId}' -> '{newVoiceId}'");
        }

        public static SpeechEventArchive? GetLocalArchive()
        {
            return _localArchive;
        }

        public static void SetLocalArchive(SpeechEventArchive archive)
        {
            _localArchive = archive;
        }

        public static (int pending, int injected) GetCounts()
        {
            int pending = 0;
            int injected = 0;
            foreach ((_, EventState state, _) in _pool.Values)
            {
                switch (state)
                {
                    case EventState.Pending: pending++; break;
                    case EventState.Injected: injected++; break;
                }
            }

            return (pending, injected);
        }

        public static List<SpeechEvent> GetPendingEvents()
        {
            List<SpeechEvent> pending = [];
            foreach ((SpeechEvent ev, EventState state, _) in _pool.Values)
            {
                if (state == EventState.Pending)
                {
                    pending.Add(ev);
                }
            }

            return pending;
        }

        public static bool HasPending()
        {
            foreach ((_, EventState state, _) in _pool.Values)
            {
                if (state == EventState.Pending)
                {
                    return true;
                }
            }

            return false;
        }

        public static int TotalCount => _pool.Count;

        public static void FixEventTiming(SpeechEvent ev, float currentTime)
        {
            RecordedTimeField?.SetValue(ev, currentTime);
            LastPlayedTimeField?.SetValue(ev, currentTime);
        }

        public static int CacheEventsFromArchive(SpeechEventArchive archive, ulong steamIdHint = 0, long playerUidHint = 0)
        {
            if (archive == null)
            {
                return 0;
            }

            try
            {
                string? playerId = null;
                long playerUID = playerUidHint;
                bool isLocal = false;

                try
                {
                    playerId = archive.PlayerId;
                    if (playerUID == 0)
                    {
                        playerUID = archive.PlayerUID;
                    }

                    isLocal = archive.IsLocal;
                }
                catch { /* Player may already be partially destroyed */ }

                if (isLocal)
                {
                    return 0;
                }

                int eventsBefore = VoiceEventStats.GetVoiceLineCount(archive);

                List<SpeechEvent> collectedEvents = [];
                HashSet<long> seenIds = [];
                _ = SpeechEventInjector.CollectFromArchive(archive, seenIds, collectedEvents);

                ulong steamId = steamIdHint;
                if (steamId == 0)
                {
                    steamId = GameSessionAccess.ResolveSteamId(playerUID, false);
                }

                int cached = 0;
                foreach (SpeechEvent ev in collectedEvents)
                {
                    if (_disconnectedCache.ContainsKey(ev.Id))
                    {
                        continue;
                    }

                    _disconnectedCache[ev.Id] = ev;
                    IndexDisconnectedEvent(steamId, ev.Id);
                    cached++;
                }

                if (string.IsNullOrEmpty(playerId) && collectedEvents.Count > 0)
                {
                    playerId = ResolveDominantPlayerName(collectedEvents);
                }

                if (steamId != 0 && !string.IsNullOrEmpty(playerId))
                {
                    _disconnectedPlayerMappings[steamId] = playerId;
                    PlayerRegistry.UpdateVoiceId(steamId, playerId);
                }

                string brief = steamId != 0
                    ? VoiceEventStats.DescribeSteamPlayer(steamId, playerUID)
                    : VoiceEventStats.DescribePlayerBrief(archive);
                ModLog.Debug(Feature, $"Disconnect cache — {brief} — cached {cached} events (totalCache={_disconnectedCache.Count})");

                if (cached == 0 && eventsBefore > 0)
                {
                    ModLog.Warn(Feature, $"Disconnect cache captured 0 events but archive had {eventsBefore} — {brief}");
                }

                TryRestoreToLiveArchiveAfterCache(archive, steamId, playerUID);

                return cached;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"CacheEventsFromArchive error: {ex.Message}");
                return 0;
            }
        }

        public static List<SpeechEvent> ClaimDisconnectedEventsForArchive(string? playerId, long playerUID, bool isLocal)
        {
            List<SpeechEvent> claimed = [];
            if (_disconnectedCache.Count == 0)
            {
                return claimed;
            }

            HashSet<string> matchedPlayerNames = ResolveMatchedPlayerNames(playerId, playerUID, isLocal, useDisconnectedMapping: true);
            ulong steamId = GameSessionAccess.ResolveSteamId(playerUID, isLocal);

            if (matchedPlayerNames.Count == 0 && steamId != 0
                && _disconnectedCacheBySteam.TryGetValue(steamId, out HashSet<long>? steamEventIds))
            {
                foreach (long id in steamEventIds)
                {
                    if (_disconnectedCache.TryGetValue(id, out SpeechEvent? ev) && ev != null
                        && !string.IsNullOrEmpty(ev.PlayerName))
                    {
                        _ = matchedPlayerNames.Add(ev.PlayerName);
                    }
                }
            }

            if (matchedPlayerNames.Count == 0)
            {
                return claimed;
            }

            List<long> idsToRemove = [];
            foreach (KeyValuePair<long, SpeechEvent> kvp in _disconnectedCache)
            {
                string evPlayerName = kvp.Value.PlayerName ?? "";
                if (!matchedPlayerNames.Contains(evPlayerName))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(playerId))
                {
                    kvp.Value.PlayerName = playerId;
                }

                claimed.Add(kvp.Value);
                idsToRemove.Add(kvp.Key);
            }

            foreach (long id in idsToRemove)
            {
                RemoveDisconnectedEvent(id, steamId);
            }

            if (steamId != 0 && claimed.Count > 0)
            {
                _ = _disconnectedPlayerMappings.Remove(steamId);
                if (!string.IsNullOrEmpty(playerId))
                {
                    PlayerRegistry.UpdateVoiceId(steamId, playerId);
                }
            }

            if (claimed.Count > 0)
            {
                ModLog.Debug(Feature, $"Reclaimed {claimed.Count} events from disconnected cache for PlayerId='{playerId}', uid={playerUID} (remaining cache: {_disconnectedCache.Count})");
            }

            return claimed;
        }

        public static List<SpeechEvent> GetDisconnectedEvents()
        {
            return [.. _disconnectedCache.Values];
        }

        public static int DisconnectedCacheCount => _disconnectedCache.Count;

        public static bool HasDisconnectedCacheForSteam(ulong steamId) =>
            steamId != 0 && _disconnectedCacheBySteam.ContainsKey(steamId);

        public static void Reset()
        {
            _pool.Clear();
            _byPlayerName.Clear();
            _deferredNameUpdates.Clear();
            _deferredInjectionArchives.Clear();
            _disconnectedCache.Clear();
            _disconnectedCacheBySteam.Clear();
            _disconnectedPlayerMappings.Clear();
            _deferredRetryStartTime.Clear();
            _restoreGiveUpWarnedSteamIds.Clear();
            _loadedSlotId = -1;
            _localArchive = null;
        }

        private static void LoadVoiceMappingsFromDocument(int slotId)
        {
            SaveSlotDocumentStore.ApplyVoiceMappingsToRuntime((steamId, voiceId) =>
            {
                PlayerRegistry.UpdateVoiceId(steamId, voiceId);
            });

            ModLog.Debug(
                Feature,
                $"Loaded player voice mappings from slot document: {PlayerRegistry.GetVoiceMappings().Count} entries (slot={slotId})");
        }

        /// <summary>
        /// Removes all voice pool, disconnected-cache, and mapping entries for one Steam ID.
        /// </summary>
        internal static int RemovePlayer(ulong steamId)
        {
            if (steamId == 0)
            {
                return 0;
            }

            HashSet<string> playerNames = ResolvePlayerNamesForSteam(steamId);
            int removed = RemoveEventsFromArchives(playerNames);

            foreach (string playerName in playerNames)
            {
                removed += RemovePoolEventsForPlayerName(playerName);
            }

            if (_disconnectedCacheBySteam.TryGetValue(steamId, out HashSet<long>? disconnectedIds))
            {
                foreach (long eventId in disconnectedIds.ToList())
                {
                    RemoveDisconnectedEvent(eventId, steamId);
                    removed++;
                }
            }

            _ = _disconnectedPlayerMappings.Remove(steamId);
            _ = _restoreGiveUpWarnedSteamIds.Remove(steamId);

            if (removed > 0)
            {
                ModLog.Info(Feature, $"Removed voice data — steamId={steamId}, events={removed}");
            }

            return removed;
        }

        private static int RemoveEventsFromArchives(HashSet<string> playerNames)
        {
            if (playerNames.Count == 0)
            {
                return 0;
            }

            int removed = 0;
            foreach (SpeechEventArchive archive in SpeechEventArchiveRegistry.EnumerateActive())
            {
                try
                {
                    SyncList<SpeechEvent>? events = archive.events;
                    if (events == null)
                    {
                        continue;
                    }

                    for (int i = events.Count - 1; i >= 0; i--)
                    {
                        SpeechEvent? ev = events[i];
                        string playerName = ev?.PlayerName ?? "";
                        if (string.IsNullOrEmpty(playerName) || !playerNames.Contains(playerName))
                        {
                            continue;
                        }

                        events.RemoveAt(i);
                        if (ev != null)
                        {
                            _ = RemovePoolEvent(ev.Id);
                        }

                        removed++;
                    }
                }
                catch
                {
                    /* archive may be partially destroyed */
                }
            }

            return removed;
        }

        private static bool RemovePoolEvent(long eventId)
        {
            if (!_pool.TryGetValue(eventId, out (SpeechEvent ev, EventState state, string originalPlayerName) entry))
            {
                return false;
            }

            _ = _pool.Remove(eventId);

            string playerName = entry.originalPlayerName;
            if (string.IsNullOrEmpty(playerName))
            {
                playerName = entry.ev.PlayerName ?? "";
            }

            if (!string.IsNullOrEmpty(playerName)
                && _byPlayerName.TryGetValue(playerName, out List<long>? ids))
            {
                _ = ids.Remove(eventId);
                if (ids.Count == 0)
                {
                    _ = _byPlayerName.Remove(playerName);
                }
            }

            _ = _disconnectedCache.Remove(eventId);
            foreach (KeyValuePair<ulong, HashSet<long>> kvp in _disconnectedCacheBySteam.ToList())
            {
                if (!kvp.Value.Remove(eventId))
                {
                    continue;
                }

                if (kvp.Value.Count == 0)
                {
                    _ = _disconnectedCacheBySteam.Remove(kvp.Key);
                }
            }

            return true;
        }

        private static HashSet<string> ResolvePlayerNamesForSteam(ulong steamId)
        {
            HashSet<string> playerNames = [];

            if (_loadedSlotId >= 0
                && SaveSlotDocumentStore.TryGetVoiceId(_loadedSlotId, steamId, out string? docVoiceId)
                && !string.IsNullOrEmpty(docVoiceId))
            {
                _ = playerNames.Add(docVoiceId);
            }

            if (PlayerRegistry.TryGetVoiceId(steamId, out string mapped) && !string.IsNullOrEmpty(mapped))
            {
                _ = playerNames.Add(mapped);
            }

            if (_disconnectedPlayerMappings.TryGetValue(steamId, out string? disconnectedMapped)
                && !string.IsNullOrEmpty(disconnectedMapped))
            {
                _ = playerNames.Add(disconnectedMapped);
            }

            foreach (SpeechEventArchive archive in SpeechEventArchiveRegistry.EnumerateActive())
            {
                try
                {
                    if (archive.IsLocal)
                    {
                        continue;
                    }

                    ulong archiveSteamId = GameSessionAccess.ResolveSteamId(archive.PlayerUID, false);
                    if (archiveSteamId == steamId && !string.IsNullOrEmpty(archive.PlayerId))
                    {
                        _ = playerNames.Add(archive.PlayerId);
                    }
                }
                catch
                {
                    /* archive may be partially destroyed */
                }
            }

            return playerNames;
        }

        private static int RemovePoolEventsForPlayerName(string playerName)
        {
            if (string.IsNullOrEmpty(playerName) || !_byPlayerName.TryGetValue(playerName, out List<long>? ids))
            {
                return 0;
            }

            int removed = 0;
            foreach (long id in ids.ToList())
            {
                if (_pool.Remove(id))
                {
                    removed++;
                }

                _ = _disconnectedCache.Remove(id);
            }

            _ = _byPlayerName.Remove(playerName);
            return removed;
        }

        private static void TryRestoreToLiveArchiveAfterCache(
            SpeechEventArchive disconnectingArchive,
            ulong steamId,
            long playerUID)
        {
            foreach (SpeechEventArchive archive in SpeechEventArchiveRegistry.EnumerateActive())
            {
                if (archive == null || ReferenceEquals(archive, disconnectingArchive))
                {
                    continue;
                }

                try
                {
                    if (archive.IsLocal)
                    {
                        continue;
                    }
                }
                catch
                {
                    continue;
                }

                if (!ArchiveMatchesIdentity(archive, steamId, playerUID))
                {
                    continue;
                }

                ModLog.Debug(Feature, $"Disconnect cache restore to live archive — {VoiceEventStats.DescribePlayerBrief(archive)}");
                ArchiveRestoreOutcome outcome = TryRestoreToArchive(archive);
                ApplyRestoreOutcome(archive, outcome, flush: true);
                return;
            }
        }

        private static bool ArchiveMatchesIdentity(SpeechEventArchive archive, ulong steamId, long playerUID)
        {
            if (steamId == 0 && playerUID == 0)
            {
                return false;
            }

            if (VoiceEventStats.TryCaptureArchiveIdentity(archive, out long liveUid, out _, out ulong liveSteam))
            {
                if (steamId != 0 && liveSteam == steamId)
                {
                    return true;
                }

                if (playerUID != 0 && liveUid == playerUID)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ShouldDeferRestoreRetry(
            SpeechEventArchive archive,
            string? playerId,
            long playerUID,
            bool isLocal)
        {
            if (!HasPending() && DisconnectedCacheCount == 0)
            {
                return false;
            }

            if (!CouldStillMatchArchive(playerId, playerUID, isLocal))
            {
                return false;
            }

            if (!_deferredRetryStartTime.TryGetValue(archive, out float startedAt))
            {
                return true;
            }

            float elapsed = GameSessionAccess.GetCurrentTickSec() - startedAt;
            return elapsed < DeferredRetryTimeoutSec;
        }

        private static bool CouldStillMatchArchive(string? playerId, long playerUID, bool isLocal)
        {
            if ((HasPending() || DisconnectedCacheCount > 0) && string.IsNullOrEmpty(playerId))
            {
                return true;
            }

            ulong steamId = GameSessionAccess.ResolveSteamId(playerUID, isLocal);
            if (steamId == 0 && string.IsNullOrEmpty(playerId) && playerUID == 0)
            {
                return HasPending() || DisconnectedCacheCount > 0;
            }

            if (HasPending())
            {
                if (ResolveMatchedPlayerNames(playerId, playerUID, isLocal).Count > 0)
                {
                    return true;
                }

                // Identity still settling — SyncVars (steamId / voice UUID) not fully arrived.
                if (playerUID != 0 && (steamId == 0 || string.IsNullOrEmpty(playerId)))
                {
                    return true;
                }

                // Known save player whose live voice UUID isn't in the pool yet — inference may still match.
                ulong knownSteamId = steamId != 0 ? steamId : (isLocal ? GameSessionAccess.GetLocalSteamId() : 0);
                if (knownSteamId != 0 && IsKnownSavePlayer(knownSteamId))
                {
                    return true;
                }
            }

            if (DisconnectedCacheCount > 0)
            {
                HashSet<string> cacheNames = ResolveMatchedPlayerNames(
                    playerId, playerUID, isLocal, useDisconnectedMapping: true);
                if (cacheNames.Count > 0)
                {
                    return true;
                }

                if (steamId != 0 && _disconnectedCacheBySteam.ContainsKey(steamId))
                {
                    return true;
                }
            }

            return steamId == 0 && playerUID != 0 && DisconnectedCacheCount > 0;
        }

        private static void MaybeWarnRestoreGiveUp(
            SpeechEventArchive archive,
            string? playerId,
            long playerUID,
            bool isLocal)
        {
            if (DisconnectedCacheCount == 0 && !HasPending())
            {
                return;
            }

            ulong steamId = GameSessionAccess.ResolveSteamId(playerUID, isLocal);
            if (steamId != 0 && !_restoreGiveUpWarnedSteamIds.Add(steamId))
            {
                return;
            }

            string brief = DescribeBrief(archive, playerUID, isLocal);
            ModLog.Warn(Feature, $"Voice restore gave up — {brief} — disconnectedCache={DisconnectedCacheCount}, pendingPool={GetCounts().pending}");
        }

        private static void ClearDeferredRetry(SpeechEventArchive archive)
        {
            _ = _deferredRetryStartTime.Remove(archive);
        }

        private static void IndexDisconnectedEvent(ulong steamId, long eventId)
        {
            if (steamId == 0)
            {
                return;
            }

            if (!_disconnectedCacheBySteam.TryGetValue(steamId, out HashSet<long>? ids))
            {
                ids = [];
                _disconnectedCacheBySteam[steamId] = ids;
            }

            _ = ids.Add(eventId);
        }

        private static void RemoveDisconnectedEvent(long eventId, ulong steamIdHint)
        {
            _ = _disconnectedCache.Remove(eventId);

            if (steamIdHint != 0
                && _disconnectedCacheBySteam.TryGetValue(steamIdHint, out HashSet<long>? ids))
            {
                _ = ids.Remove(eventId);
                if (ids.Count == 0)
                {
                    _ = _disconnectedCacheBySteam.Remove(steamIdHint);
                }
            }
            else
            {
                foreach (KeyValuePair<ulong, HashSet<long>> kvp in _disconnectedCacheBySteam)
                {
                    if (kvp.Value.Remove(eventId) && kvp.Value.Count == 0)
                    {
                        _ = _disconnectedCacheBySteam.Remove(kvp.Key);
                    }
                }
            }
        }

        private static string? ResolveDominantPlayerName(IReadOnlyList<SpeechEvent> events)
        {
            Dictionary<string, int> counts = [];
            string? dominant = null;
            int max = 0;

            foreach (SpeechEvent ev in events)
            {
                if (ev == null || string.IsNullOrEmpty(ev.PlayerName))
                {
                    continue;
                }

                counts.TryGetValue(ev.PlayerName, out int count);
                count++;
                counts[ev.PlayerName] = count;
                if (count > max)
                {
                    max = count;
                    dominant = ev.PlayerName;
                }
            }

            return dominant;
        }

        private static void PushLiveVoiceIdsToRegistry()
        {
            HashSet<ulong> steamIds = [];

            foreach (SpeechEventArchive archive in SpeechEventArchiveRegistry.EnumerateActive())
            {
                try
                {
                    ulong steamId = GameSessionAccess.ResolveSteamId(archive.PlayerUID, archive.IsLocal);
                    if (steamId != 0)
                    {
                        _ = steamIds.Add(steamId);
                    }
                }
                catch (Exception archEx)
                {
                    ModLog.Warn(Feature, $"Archive error during voice push: {archEx.Message}");
                }
            }

            foreach (ulong steamId in PlayerRegistry.GetVoiceMappings().Keys)
            {
                _ = steamIds.Add(steamId);
            }

            foreach (ulong steamId in _disconnectedPlayerMappings.Keys)
            {
                _ = steamIds.Add(steamId);
            }

            GameSessionInfo? sessionInfo = GameSessionAccess.TryGetGameSessionInfo();
            if (sessionInfo?.TotalPlayerSteamIDs != null)
            {
                foreach (ulong steamId in sessionInfo.TotalPlayerSteamIDs.Keys)
                {
                    if (steamId != 0)
                    {
                        _ = steamIds.Add(steamId);
                    }
                }
            }

            foreach (ulong steamId in steamIds)
            {
                string? voiceId = ResolveSaveVoiceIdForSteam(steamId);
                if (!string.IsNullOrEmpty(voiceId))
                {
                    PlayerRegistry.UpdateVoiceId(steamId, voiceId);
                }
            }
        }

        private static string? ResolveSaveVoiceIdForSteam(ulong steamId)
        {
            if (steamId == 0)
            {
                return null;
            }

            string? liveArchiveVoiceId = null;
            foreach (SpeechEventArchive archive in SpeechEventArchiveRegistry.EnumerateActive())
            {
                try
                {
                    ulong archiveSteamId = GameSessionAccess.ResolveSteamId(archive.PlayerUID, archive.IsLocal);
                    if (archiveSteamId != steamId)
                    {
                        continue;
                    }

                    string pid = archive.PlayerId;
                    if (!string.IsNullOrEmpty(pid))
                    {
                        liveArchiveVoiceId = pid;
                        if (_byPlayerName.ContainsKey(pid))
                        {
                            return pid;
                        }
                    }

                    List<SpeechEvent> archiveEvents = [];
                    HashSet<long> seenIds = [];
                    if (SpeechEventInjector.CollectFromArchive(archive, seenIds, archiveEvents) > 0)
                    {
                        string? dominantVoiceId = ResolveDominantPlayerName(archiveEvents);
                        if (!string.IsNullOrEmpty(dominantVoiceId))
                        {
                            return dominantVoiceId;
                        }
                    }
                }
                catch
                {
                    /* archive may be tearing down */
                }
            }

            foreach (string playerName in ResolvePlayerNamesForSteam(steamId))
            {
                if (_byPlayerName.ContainsKey(playerName))
                {
                    return playerName;
                }
            }

            if (!string.IsNullOrEmpty(liveArchiveVoiceId))
            {
                return liveArchiveVoiceId;
            }

            if (PlayerRegistry.TryGetVoiceId(steamId, out string mapped) && !string.IsNullOrEmpty(mapped))
            {
                return mapped;
            }

            if (_disconnectedPlayerMappings.TryGetValue(steamId, out string? disconnected)
                && !string.IsNullOrEmpty(disconnected))
            {
                return disconnected;
            }

            return null;
        }

        private static HashSet<string> ResolveMatchedPlayerNames(
            string? playerId,
            long playerUID,
            bool isLocal,
            bool useDisconnectedMapping = false)
        {
            HashSet<string> matchedPlayerNames = [];

            if (!string.IsNullOrEmpty(playerId))
            {
                if (_byPlayerName.ContainsKey(playerId))
                {
                    _ = matchedPlayerNames.Add(playerId);
                }
                else if (useDisconnectedMapping)
                {
                    _ = matchedPlayerNames.Add(playerId);
                }
            }

            ulong steamId = GameSessionAccess.ResolveSteamId(playerUID, isLocal);

            AddMappedVoiceId(matchedPlayerNames, steamId, _disconnectedPlayerMappings, useDisconnectedMapping);

            IReadOnlyDictionary<ulong, string> registryMappings = PlayerRegistry.GetVoiceMappings();
            if (useDisconnectedMapping)
            {
                AddMappedVoiceId(matchedPlayerNames, steamId, registryMappings, requirePoolMatch: false);
            }
            else
            {
                AddMappedVoiceId(matchedPlayerNames, steamId, registryMappings, requirePoolMatch: true);
            }

            if (matchedPlayerNames.Count == 0)
            {
                AddUnmappedPoolInference(matchedPlayerNames, steamId, isLocal);
            }

            return matchedPlayerNames;
        }

        /// <summary>
        /// When no Steam→VoiceId mapping was persisted, assign unmapped pool voice IDs
        /// to a connecting player known on this save.
        /// </summary>
        private static void AddUnmappedPoolInference(
            HashSet<string> matchedPlayerNames,
            ulong steamId,
            bool isLocal)
        {
            if (_byPlayerName.Count == 0)
            {
                return;
            }

            if (steamId == 0 && isLocal)
            {
                steamId = GameSessionAccess.GetLocalSteamId();
            }

            if (!IsKnownSavePlayer(steamId))
            {
                return;
            }

            HashSet<string> mappedToOthers = CollectPoolVoiceIdsMappedToOtherSteamIds(steamId);
            List<string> unmapped = [];
            foreach (string poolName in _byPlayerName.Keys)
            {
                if (!string.IsNullOrEmpty(poolName) && !mappedToOthers.Contains(poolName))
                {
                    unmapped.Add(poolName);
                }
            }

            if (unmapped.Count == 0)
            {
                return;
            }

            IReadOnlyList<PlayerStatisticsDocument> stats = PlayerRegistry.GetAllStatistics();
            if (stats.Count == 1 && stats[0].SteamId == steamId)
            {
                foreach (string poolName in unmapped)
                {
                    _ = matchedPlayerNames.Add(poolName);
                }

                ModLog.Debug(
                    Feature,
                    $"Solo-save unmapped pool inference — steamId={steamId}, pooledVoiceIds={unmapped.Count}");
                return;
            }

            string? dominant = ResolveDominantPoolVoiceId(unmapped);
            if (string.IsNullOrEmpty(dominant))
            {
                return;
            }

            _ = matchedPlayerNames.Add(dominant);
            ModLog.Debug(
                Feature,
                $"Unmapped pool voice inference — steamId={steamId}, pooledVoiceId='{dominant}'");
        }

        private static string? ResolveDominantPoolVoiceId(IReadOnlyList<string> poolVoiceIds)
        {
            string? dominant = null;
            int dominantCount = 0;

            foreach (string poolVoiceId in poolVoiceIds)
            {
                if (!_byPlayerName.TryGetValue(poolVoiceId, out List<long>? eventIds))
                {
                    continue;
                }

                if (eventIds.Count > dominantCount)
                {
                    dominantCount = eventIds.Count;
                    dominant = poolVoiceId;
                }
            }

            return dominant;
        }

        private static bool IsKnownSavePlayer(ulong steamId) =>
            SpeechEventVoiceOwnership.IsKnownSavePlayer(_loadedSlotId, steamId);

        private static HashSet<string> CollectPoolVoiceIdsMappedToOtherSteamIds(ulong connectingSteamId)
        {
            HashSet<string> mapped = new(StringComparer.Ordinal);

            foreach (KeyValuePair<ulong, string> kvp in PlayerRegistry.GetVoiceMappings())
            {
                if (kvp.Key == 0 || kvp.Key == connectingSteamId || string.IsNullOrEmpty(kvp.Value))
                {
                    continue;
                }

                if (_byPlayerName.ContainsKey(kvp.Value))
                {
                    _ = mapped.Add(kvp.Value);
                }
            }

            if (MimesisSaveManager.IsValidSaveSlotId(_loadedSlotId))
            {
                SaveSlotDocumentStore.ApplyPlayerEntries((steamId, entry) =>
                {
                    if (steamId == 0
                        || steamId == connectingSteamId
                        || string.IsNullOrWhiteSpace(entry.VoiceId)
                        || !_byPlayerName.ContainsKey(entry.VoiceId))
                    {
                        return;
                    }

                    _ = mapped.Add(entry.VoiceId);
                });
            }

            return mapped;
        }

        private static void AddMappedVoiceId(
            HashSet<string> matchedPlayerNames,
            ulong steamId,
            IReadOnlyDictionary<ulong, string> mappingSource,
            bool requirePoolMatch)
        {
            if (steamId == 0 || !mappingSource.TryGetValue(steamId, out string oldDissonanceId))
            {
                return;
            }

            if (!requirePoolMatch || _byPlayerName.ContainsKey(oldDissonanceId))
            {
                _ = matchedPlayerNames.Add(oldDissonanceId);
            }
        }
    }
}
