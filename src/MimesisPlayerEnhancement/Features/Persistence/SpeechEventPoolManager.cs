using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MimesisPlayerEnhancement.Features.Persistence.Patches;
using MimesisPlayerEnhancement.Features.Statistics;
using MimesisPlayerEnhancement.Util;
using Mimic.Voice.SpeechSystem;

namespace MimesisPlayerEnhancement.Features.Persistence
{
    /// <summary>
    /// Manages a 2-state pool of SpeechEvents loaded from disk.
    /// Pending  -> loaded, waiting for a matching SpeechEventArchive
    /// Injected -> matched and added to the correct player's archive
    /// </summary>
    public static class SpeechEventPoolManager
    {
        private const string Feature = "Persistence";
        public enum EventState { Pending, Injected }

        private static readonly Dictionary<long, (SpeechEvent ev, EventState state, string originalPlayerName)> _pool
            = [];

        private static readonly Dictionary<string, List<long>> _byPlayerName = [];

        private static readonly Dictionary<ulong, string> _steamToDissonance = [];

        private static int _loadedSlotId = -1;

        private static SpeechEventArchive? _localArchive;

        private static readonly List<(SpeechEventArchive archive, List<SpeechEvent> events)> _deferredNameUpdates
            = [];

        private static readonly HashSet<SpeechEventArchive> _deferredInjectionArchives = [];

        private static readonly Dictionary<long, SpeechEvent> _disconnectedCache = [];

        private static readonly Dictionary<ulong, string> _disconnectedPlayerMappings = [];

        private static readonly HashSet<SpeechEventArchive> _awaitingVoiceUuid = [];

        private static readonly FieldInfo? RecordedTimeField =
            typeof(SpeechEvent).GetField("RecordedTime", BindingFlags.Public | BindingFlags.Instance);

        private static readonly FieldInfo? LastPlayedTimeField =
            typeof(SpeechEvent).GetField("LastPlayedTime", BindingFlags.Public | BindingFlags.Instance);

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

            LoadPlayerMapping(slotId);

            ModLog.Debug(Feature, $"Loaded {_pool.Count} events for slot {slotId} ({_steamToDissonance.Count} SteamID mappings)");
        }

        public static bool AwaitingVoiceUuid(SpeechEventArchive archive) =>
            archive != null && _awaitingVoiceUuid.Contains(archive);

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

                    LogVoiceUuidRemap(brief, claimed.Count, savedVoiceId, playerId);
                }
                else if (archive != null)
                {
                    _deferredNameUpdates.Add((archive, new List<SpeechEvent>(claimed)));
                    _ = _awaitingVoiceUuid.Add(archive);
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

            if (_deferredInjectionArchives.Add(archive))
            {
                ModLog.Debug(Feature, $"Deferred injection registered — {VoiceEventStats.DescribePlayerBrief(archive)} " +
                                $"(waiting for SyncVars, {_deferredInjectionArchives.Count} pending)");
            }
        }

        public static void ProcessDeferredUpdates()
        {
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

                    string playerId;
                    long playerUID;
                    bool isLocal;
                    try
                    {
                        playerId = archive.PlayerId;
                        playerUID = archive.PlayerUID;
                        isLocal = archive.IsLocal;
                    }
                    catch
                    {
                        _ = _deferredInjectionArchives.Add(archive);
                        continue;
                    }

                    if (string.IsNullOrEmpty(playerId) && playerUID == 0)
                    {
                        _ = _deferredInjectionArchives.Add(archive);
                        continue;
                    }

                    ModLog.Debug(Feature, $"Deferred injection ready — {VoiceEventStats.DescribePlayerBrief(archive)}");

                    int eventsBefore = VoiceEventStats.GetVoiceLineCount(archive);
                    SpeechEventInjector.RestoreResult result = SpeechEventInjector.RestoreIntoArchive(
                        archive, playerId, playerUID, isLocal);
                    int eventsAfter = VoiceEventStats.GetVoiceLineCount(archive);

                    StatisticsTracker.SyncVoiceBaseline(archive);

                    PersistenceConnectOutcome outcome = SpeechEventArchivePatches.BuildConnectOutcome(
                        result, eventsBefore, eventsAfter);
                    PlayerLifecycleCoordinator.SetPersistenceOutcome(archive, outcome, flush: true);
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

                LogVoiceUuidRemap(VoiceEventStats.DescribePlayerBrief(archive), events.Count, oldVoiceId, newPlayerId);
                _ = _awaitingVoiceUuid.Remove(archive);
                _deferredNameUpdates.RemoveAt(i);

                PlayerLifecycleCoordinator.TryFlushConnect(archive);
            }
        }

        private static string DescribeBrief(SpeechEventArchive? archive, long playerUID, bool isLocal)
        {
            return archive != null
                ? VoiceEventStats.DescribePlayerBrief(archive)
                : $"steamId={GameSessionAccess.ResolveSteamId(playerUID, isLocal)}";
        }

        private static void LogVoiceUuidRemap(string brief, int count, string oldVoiceId, string newVoiceId)
        {
            ModLog.Debug(Feature, $"Voice UUID remapped — {brief} — {count} events: '{oldVoiceId}' -> '{newVoiceId}'");
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

        public static bool IsLoaded => _loadedSlotId >= 0 && _pool.Count > 0;

        public static void FixEventTiming(SpeechEvent ev, float currentTime)
        {
            RecordedTimeField?.SetValue(ev, currentTime);
            LastPlayedTimeField?.SetValue(ev, currentTime);
        }

        public static int CacheEventsFromArchive(SpeechEventArchive archive)
        {
            if (archive == null)
            {
                return 0;
            }

            try
            {
                string? playerId = null;
                long playerUID = 0;
                bool isLocal = false;

                try
                {
                    playerId = archive.PlayerId;
                    playerUID = archive.PlayerUID;
                    isLocal = archive.IsLocal;
                }
                catch { /* Player may already be partially destroyed */ }

                if (isLocal)
                {
                    return 0;
                }

                List<SpeechEvent> collectedEvents = [];
                HashSet<long> seenIds = [];
                _ = SpeechEventInjector.CollectFromArchive(archive, seenIds, collectedEvents);

                int cached = 0;
                foreach (SpeechEvent ev in collectedEvents)
                {
                    if (_disconnectedCache.ContainsKey(ev.Id))
                    {
                        continue;
                    }

                    _disconnectedCache[ev.Id] = ev;
                    cached++;
                }

                ulong steamId = GameSessionAccess.ResolveSteamId(playerUID, false);
                if (steamId != 0 && !string.IsNullOrEmpty(playerId))
                {
                    _disconnectedPlayerMappings[steamId] = playerId;
                }

                ModLog.Debug(Feature, $"Disconnect cache — {VoiceEventStats.DescribePlayerBrief(archive)} — cached {cached} events (totalCache={_disconnectedCache.Count})");
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
                _ = _disconnectedCache.Remove(id);
            }

            ulong steamId = GameSessionAccess.ResolveSteamId(playerUID, isLocal);
            if (steamId != 0 && claimed.Count > 0)
            {
                _ = _disconnectedPlayerMappings.Remove(steamId);
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

        public static Dictionary<ulong, string> GetDisconnectedPlayerMappings()
        {
            return new Dictionary<ulong, string>(_disconnectedPlayerMappings);
        }

        public static int DisconnectedCacheCount => _disconnectedCache.Count;

        public static void Reset()
        {
            _pool.Clear();
            _byPlayerName.Clear();
            _steamToDissonance.Clear();
            _deferredNameUpdates.Clear();
            _deferredInjectionArchives.Clear();
            _awaitingVoiceUuid.Clear();
            _disconnectedCache.Clear();
            _disconnectedPlayerMappings.Clear();
            _loadedSlotId = -1;
            _localArchive = null;
        }

        public static bool TryBuildPlayerMappingJson(
            int slotId,
            out string filePath,
            out string json)
        {
            filePath = string.Empty;
            json = string.Empty;

            string? mappingPath = SaveSidecarPaths.GetSpeechMappingPath(slotId);
            if (string.IsNullOrEmpty(mappingPath))
            {
                ModLog.Warn(Feature, "TryBuildPlayerMappingJson: sidecar path is null/empty!");
                return false;
            }

            try
            {
                Dictionary<ulong, string> mapping = BuildPlayerMapping();
                filePath = mappingPath;
                json = ModJson.Serialize(mapping);
                ModLog.Debug(Feature, $"Built player mapping: {mapping.Count} entries -> {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                ModLog.Error(Feature, $"TryBuildPlayerMappingJson FAILED: {ex}");
                return false;
            }
        }

        private static Dictionary<ulong, string> BuildPlayerMapping()
        {
            Dictionary<ulong, string> mapping = [];

            foreach (SpeechEventArchive archive in SpeechEventArchiveRegistry.EnumerateActive())
            {
                try
                {
                    string pid = archive.PlayerId;
                    long uid = archive.PlayerUID;
                    bool isLocal = archive.IsLocal;

                    if (string.IsNullOrEmpty(pid))
                    {
                        continue;
                    }

                    ulong steamId = GameSessionAccess.ResolveSteamId(uid, isLocal);
                    if (steamId != 0)
                    {
                        mapping[steamId] = pid;
                    }
                }
                catch (Exception archEx)
                {
                    ModLog.Warn(Feature, $"Archive error during mapping build: {archEx.Message}");
                }
            }

            foreach (KeyValuePair<ulong, string> kvp in GetDisconnectedPlayerMappings())
            {
                mapping.TryAdd(kvp.Key, kvp.Value);
            }

            return mapping;
        }

        private static void LoadPlayerMapping(int slotId)
        {
            _steamToDissonance.Clear();

            string? filePath = SaveSidecarPaths.GetSpeechMappingPath(slotId);
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            string? json = AtomicFileIO.ReadText(filePath, Feature);
            if (string.IsNullOrEmpty(json))
            {
                ModLog.Debug(Feature, $"No player mapping sidecar at {filePath}");
                return;
            }

            try
            {
                Dictionary<ulong, string>? mapping = ModJson.Deserialize<Dictionary<ulong, string>>(json);
                if (mapping == null)
                {
                    return;
                }

                foreach (KeyValuePair<ulong, string> kvp in mapping)
                {
                    _steamToDissonance[kvp.Key] = kvp.Value;
                }

                ModLog.Debug(Feature, $"Loaded player mapping: {_steamToDissonance.Count} entries");
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"LoadPlayerMapping: {ex.Message}");
            }
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
            if (steamId == 0)
            {
                return matchedPlayerNames;
            }

            Dictionary<ulong, string> mappingSource = useDisconnectedMapping
                ? _disconnectedPlayerMappings
                : _steamToDissonance;

            if (mappingSource.TryGetValue(steamId, out string oldDissonanceId)
                && _byPlayerName.ContainsKey(oldDissonanceId))
            {
                _ = matchedPlayerNames.Add(oldDissonanceId);
            }
            else if (useDisconnectedMapping && mappingSource.TryGetValue(steamId, out oldDissonanceId))
            {
                _ = matchedPlayerNames.Add(oldDissonanceId);
            }

            return matchedPlayerNames;
        }
    }
}
