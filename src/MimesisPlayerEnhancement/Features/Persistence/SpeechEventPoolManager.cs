using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mimic.Voice.SpeechSystem;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.Persistence
{
    /// <summary>
    /// Manages a 3-state pool of SpeechEvents loaded from disk.
    /// PENDING  -> loaded, waiting for a matching SpeechEventArchive
    /// INJECTED -> matched and added to the correct player's archive
    ///
    /// Player mapping saved as SteamID -> old DissonanceID.
    /// On load, when a new archive's SteamID matches a saved SteamID,
    /// we find events by the old DissonanceID and update PlayerName to the new one.
    /// </summary>
    public static class SpeechEventPoolManager
    {
        public enum EventState { Pending, Injected }

        private static readonly Dictionary<long, (SpeechEvent ev, EventState state, string originalPlayerName)> _pool
            = new Dictionary<long, (SpeechEvent, EventState, string)>();

        // Index: old PlayerName (DissonanceID) -> list of event IDs for fast lookup
        private static readonly Dictionary<string, List<long>> _byPlayerName
            = new Dictionary<string, List<long>>();

        // Saved mapping from previous session: SteamID -> old DissonanceID
        // Allows cross-session matching even when DissonanceID changes
        private static readonly Dictionary<ulong, string> _steamToDissonance
            = new Dictionary<ulong, string>();

        private static int _loadedSlotId = -1;

        // Reference to the local (host) archive
        private static SpeechEventArchive? _localArchive;

        // Deferred PlayerName updates: events injected before PlayerId was available
        // Key = archive reference, Value = list of events needing PlayerName update
        private static readonly List<(SpeechEventArchive archive, List<SpeechEvent> events)> _deferredNameUpdates
            = new List<(SpeechEventArchive, List<SpeechEvent>)>();

        // Deferred injection: archives whose PlayerId/PlayerUID weren't available at OnStartClient.
        // Happens for remote players because FishNet SyncVars haven't synced yet.
        // ProcessDeferredUpdates will retry when the data becomes available.
        private static readonly List<SpeechEventArchive> _deferredInjectionArchives
            = new List<SpeechEventArchive>();

        // ===================== Disconnected Player Cache =====================
        // Caches SpeechEvents from players who disconnect mid-session (before save).
        // Without this, their SpeechEventArchive (NetworkObject) gets destroyed
        // and all their voice events from the current session are lost.
        // Keyed by event ID to avoid duplicates.
        private static readonly Dictionary<long, SpeechEvent> _disconnectedCache
            = new Dictionary<long, SpeechEvent>();

        // Cached SteamID -> DissonanceID for disconnected players.
        // SavePlayerMapping uses FindObjectsOfType which misses destroyed archives.
        private static readonly Dictionary<ulong, string> _disconnectedPlayerMappings
            = new Dictionary<ulong, string>();

        // RecordedTime and LastPlayedTime are readonly fields, need reflection
        private static readonly FieldInfo RecordedTimeField =
            typeof(SpeechEvent).GetField("RecordedTime", BindingFlags.Public | BindingFlags.Instance);
        private static readonly FieldInfo LastPlayedTimeField =
            typeof(SpeechEvent).GetField("LastPlayedTime", BindingFlags.Public | BindingFlags.Instance);

        private const string PlayerMappingFile = "player_mapping.json";

        /// <summary>
        /// Load events from disk for the given slot. Idempotent - only loads once per slot.
        /// </summary>
        public static void LoadForSlot(int slotId)
        {
            if (slotId == _loadedSlotId && _pool.Count > 0) return;

            Reset();
            _loadedSlotId = slotId;

            // Load speech events
            List<SpeechEvent>? events = MimesisSaveManager.LoadSpeechEvents(slotId);
            if (events == null || events.Count == 0)
            {
                ModLog.Debug("Persistence", "No events to load for slot " + slotId);
                return;
            }

            // Populate pool
            foreach (var ev in events)
            {
                if (ev == null || _pool.ContainsKey(ev.Id)) continue;

                string playerName = ev.PlayerName ?? "";
                _pool[ev.Id] = (ev, EventState.Pending, playerName);

                if (!_byPlayerName.TryGetValue(playerName, out var list))
                {
                    list = new List<long>();
                    _byPlayerName[playerName] = list;
                }
                list.Add(ev.Id);
            }

            // Load player mapping for cross-session UID matching
            LoadPlayerMapping(slotId);

            ModLog.Info("Persistence", $"Loaded {_pool.Count} events for slot {slotId} " +
                            $"({_steamToDissonance.Count} SteamID mappings)");
        }

        /// <summary>
        /// Try to claim PENDING events for the given archive using multi-level matching:
        /// 1. Direct DissonanceID match (PlayerName == playerId) - works if ID persists
        /// 2. SteamID match: get current SteamID -> find old DissonanceID in mapping
        ///    -> claim events with that old DissonanceID
        /// After claiming, updates ev.PlayerName to the new DissonanceID.
        /// </summary>
        public static List<SpeechEvent> ClaimEventsForArchive(string? playerId, long playerUID, bool isLocal = false, SpeechEventArchive? archive = null)
        {
            var claimed = new List<SpeechEvent>();
            if (_pool.Count == 0) return claimed;

            ModLog.Debug("Persistence", $"ClaimEventsForArchive: PlayerId='{playerId}', " +
                            $"PlayerUID={playerUID}, isLocal={isLocal}, " +
                            $"pool has {_pool.Count} events, playerNames in pool: [{string.Join(", ", _byPlayerName.Keys)}]");

            var matchedPlayerNames = new HashSet<string>();

            // Level 1: Direct DissonanceID match (fast path, same ID across sessions)
            if (!string.IsNullOrEmpty(playerId) && _byPlayerName.ContainsKey(playerId))
            {
                matchedPlayerNames.Add(playerId);
                ModLog.Debug("Persistence", $"Level 1 match: DissonanceID '{playerId}' found directly");
            }

            // Level 2: SteamID-based match
            // Get current SteamID for this archive's PlayerUID
            ulong archiveSteamId = GetSteamIdForPlayerUID(playerUID, isLocal);
            if (archiveSteamId != 0)
            {
                // Look up old DissonanceID from saved mapping
                if (_steamToDissonance.TryGetValue(archiveSteamId, out string oldDissonanceId))
                {
                    if (_byPlayerName.ContainsKey(oldDissonanceId))
                    {
                        matchedPlayerNames.Add(oldDissonanceId);
                        ModLog.Debug("Persistence", $"Level 2 match: SteamID {archiveSteamId} " +
                                        $"-> old DissonanceID '{oldDissonanceId}' (new: '{playerId}')");
                    }
                }
                else
                {
                    ModLog.Debug("Persistence", $"Level 2: SteamID {archiveSteamId} not in mapping " +
                                    $"({_steamToDissonance.Count} entries)");
                }
            }
            else
            {
                ModLog.Debug("Persistence", $"Level 2: Could not resolve SteamID for " +
                                $"PlayerUID={playerUID}");
            }

            // Claim all PENDING events for matched player names
            foreach (var playerName in matchedPlayerNames)
            {
                if (!_byPlayerName.TryGetValue(playerName, out var eventIds)) continue;

                foreach (long id in eventIds)
                {
                    if (!_pool.TryGetValue(id, out var entry)) continue;
                    if (entry.state != EventState.Pending) continue;

                    _pool[id] = (entry.ev, EventState.Injected, entry.originalPlayerName);
                    claimed.Add(entry.ev);
                }
            }

            // Update PlayerName to the NEW DissonanceID so the game sees them as
            // belonging to this archive's player (important for mimic matching)
            if (claimed.Count > 0)
            {
                if (!string.IsNullOrEmpty(playerId))
                {
                    foreach (var ev in claimed)
                        ev.PlayerName = playerId;

                    ModLog.Debug("Persistence", $"Claimed {claimed.Count} events, " +
                                    $"PlayerName updated to '{playerId}'");
                }
                else if (archive != null)
                {
                    // PlayerId not available yet (OnStartClient fires before Player init)
                    // Register deferred update - will be applied when PlayerId becomes available
                    _deferredNameUpdates.Add((archive, new List<SpeechEvent>(claimed)));
                    ModLog.Debug("Persistence", $"Claimed {claimed.Count} events, " +
                                    $"PlayerId empty -> deferred PlayerName update registered");
                }
            }
            else
            {
                var (p, i, f) = GetCounts();
                ModLog.Debug("Persistence", $"No events claimed for PlayerId='{playerId}' " +
                                $"(matched names: [{string.Join(", ", matchedPlayerNames)}], " +
                                $"pool: {p}P/{i}I/{f}F)");
            }

            return claimed;
        }

        /// <summary>
        /// Get SteamID for a PlayerUID.
        /// 1. Try Hub.s.pdata.actorUIDToSteamID (populated for remote players)
        /// 2. Fallback (only if isLocal): PlatformMgr._uniqueUserPath (host's own SteamID)
        /// </summary>
        private static ulong GetSteamIdForPlayerUID(long playerUID, bool isLocal = false)
        {
            // Try actorUIDToSteamID first (works for remote players, needs playerUID > 0)
            if (playerUID != 0)
            {
                try
                {
                    object? pdata = MimesisSaveManager.GetHubMember("pdata");
                    if (pdata != null)
                    {
                        var field = pdata.GetType().GetField("actorUIDToSteamID",
                            BindingFlags.Public | BindingFlags.Instance);
                        if (field != null)
                        {
                            var dict = field.GetValue(pdata) as Dictionary<long, ulong>;
                            if (dict != null && dict.TryGetValue(playerUID, out ulong steamId))
                            {
                                ModLog.Debug("Persistence", $"Resolved PlayerUID {playerUID} -> SteamID {steamId} (from actorUIDToSteamID)");
                                return steamId;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModLog.Warn("Persistence", $"actorUIDToSteamID lookup error: {ex.Message}");
                }
            }

            // Fallback for the host: PlatformMgr._uniqueUserPath
            // actorUIDToSteamID does NOT contain the host's own entry,
            // AND PlayerUID can be 0 at OnStartClient time before Player is initialized
            if (isLocal)
            {
                ulong hostSteamId = GetLocalSteamId();
                if (hostSteamId != 0)
                {
                    ModLog.Debug("Persistence", $"Resolved host SteamID={hostSteamId} (from PlatformMgr, PlayerUID was {playerUID})");
                    return hostSteamId;
                }
            }

            ModLog.Warn("Persistence", $"Could not resolve SteamID for PlayerUID={playerUID} (isLocal={isLocal})");
            return 0;
        }

        /// <summary>
        /// Get the local (host) player's SteamID from PlatformMgr._uniqueUserPath.
        /// </summary>
        private static ulong GetLocalSteamId()
        {
            try
            {
                var platformMgr = MonoSingleton<PlatformMgr>.Instance;
                if (platformMgr == null) return 0;

                var field = typeof(PlatformMgr).GetField("_uniqueUserPath",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null) return 0;

                string? userPath = field.GetValue(platformMgr) as string;
                if (!string.IsNullOrEmpty(userPath) && ulong.TryParse(userPath, out ulong steamId))
                    return steamId;
            }
            catch (Exception ex)
            {
                ModLog.Warn("Persistence", $"GetLocalSteamId error: {ex.Message}");
            }
            return 0;
        }


        /// <summary>
        /// Register an archive for deferred injection. Called when OnStartClient fires
        /// but PlayerId/PlayerUID aren't available yet (remote player SyncVars not synced).
        /// </summary>
        public static void RegisterDeferredInjection(SpeechEventArchive archive)
        {
            if (archive == null) return;
            // Avoid duplicates
            foreach (var existing in _deferredInjectionArchives)
                if (existing == archive) return;

            _deferredInjectionArchives.Add(archive);
            ModLog.Debug("Persistence", $"Registered archive for deferred injection " +
                            $"(waiting for SyncVars, {_deferredInjectionArchives.Count} pending)");
        }

        /// <summary>
        /// Process all deferred operations. Called from Mod.OnUpdate() every frame.
        /// 1. Deferred injections: archives waiting for PlayerId/PlayerUID to sync
        /// 2. Deferred name updates: events injected before PlayerId was set
        /// </summary>
        public static void ProcessDeferredUpdates()
        {
            // === Deferred injections (remote players whose SyncVars weren't ready) ===
            if (_deferredInjectionArchives.Count > 0)
            {
                for (int i = _deferredInjectionArchives.Count - 1; i >= 0; i--)
                {
                    var archive = _deferredInjectionArchives[i];

                    // Archive destroyed?
                    if (archive == null)
                    {
                        _deferredInjectionArchives.RemoveAt(i);
                        continue;
                    }

                    // Check if SyncVars have synced
                    string playerId;
                    long playerUID;
                    bool isLocal;
                    try
                    {
                        playerId = archive.PlayerId;
                        playerUID = archive.PlayerUID;
                        isLocal = archive.IsLocal;
                    }
                    catch { continue; } // Player not ready yet

                    if (string.IsNullOrEmpty(playerId) && playerUID == 0)
                        continue; // Still not synced, try next frame

                    // SyncVars are ready! Do the full claim+inject
                    _deferredInjectionArchives.RemoveAt(i);
                    ModLog.Debug("Persistence", $"Deferred injection: SyncVars ready for " +
                                    $"PlayerId='{playerId}', PlayerUID={playerUID}");

                    var eventsList = archive.events;
                    if (eventsList == null) continue;

                    float currentTime = GetCurrentSessionTime();
                    var seenIds = new HashSet<long>();
                    for (int j = 0; j < eventsList.Count; j++)
                        seenIds.Add(eventsList[j].Id);

                    int totalAdded = 0;

                    // Source 1: Pool from disk
                    if (HasPending())
                    {
                        var claimed = ClaimEventsForArchive(playerId, playerUID, isLocal, archive);
                        if (claimed != null)
                        {
                            foreach (var ev in claimed)
                            {
                                if (ev == null || seenIds.Contains(ev.Id)) continue;
                                FixEventTiming(ev, currentTime);
                                eventsList.Add(ev);
                                seenIds.Add(ev.Id);
                                totalAdded++;
                            }
                        }
                    }

                    // Source 2: Disconnected cache
                    if (_disconnectedCache.Count > 0)
                    {
                        var reclaimed = ClaimDisconnectedEventsForArchive(playerId, playerUID, isLocal);
                        if (reclaimed != null)
                        {
                            foreach (var ev in reclaimed)
                            {
                                if (ev == null || seenIds.Contains(ev.Id)) continue;
                                FixEventTiming(ev, currentTime);
                                eventsList.Add(ev);
                                seenIds.Add(ev.Id);
                                totalAdded++;
                            }
                        }
                    }

                    if (totalAdded > 0)
                    {
                        ModLog.Info(
                            "Persistence",
                            $"Player connected — {VoiceEventStats.DescribePlayer(archive)} — " +
                            $"restored {totalAdded} voice events (deferred injection)");
                    }
                    else
                    {
                        ModLog.Info(
                            "Persistence",
                            $"Player connected — {VoiceEventStats.DescribePlayer(archive)} — no matching saved voices (deferred injection)");
                    }
                }
            }

            // === Deferred PlayerName updates (events claimed before PlayerId was set) ===
            if (_deferredNameUpdates.Count == 0) return;

            for (int i = _deferredNameUpdates.Count - 1; i >= 0; i--)
            {
                var (archive, events) = _deferredNameUpdates[i];

                // Archive destroyed?
                if (archive == null)
                {
                    _deferredNameUpdates.RemoveAt(i);
                    continue;
                }

                string newPlayerId;
                try { newPlayerId = archive.PlayerId; }
                catch { continue; } // Player not ready yet

                if (string.IsNullOrEmpty(newPlayerId))
                    continue; // Still not available, try next frame

                // PlayerId is now available! Update all events
                foreach (var ev in events)
                {
                    if (ev != null)
                        ev.PlayerName = newPlayerId;
                }

                ModLog.Debug("Persistence", $"Deferred update: {events.Count} events " +
                                $"PlayerName updated to '{newPlayerId}'");
                _deferredNameUpdates.RemoveAt(i);
            }
        }

        /// <summary>
        /// Get the local archive reference.
        /// </summary>
        public static SpeechEventArchive? GetLocalArchive()
        {
            return _localArchive;
        }

        /// <summary>
        /// Store a reference to the local archive.
        /// </summary>
        public static void SetLocalArchive(SpeechEventArchive archive)
        {
            _localArchive = archive;
        }

        /// <summary>
        /// Get current counts for each state.
        /// </summary>
        public static (int pending, int injected, int fallback) GetCounts()
        {
            int pending = 0, injected = 0;
            foreach (var entry in _pool.Values)
            {
                switch (entry.state)
                {
                    case EventState.Pending: pending++; break;
                    case EventState.Injected: injected++; break;
                }
            }
            return (pending, injected, 0);
        }

        /// <summary>
        /// Get all PENDING events (loaded from disk but not matched to any archive this session).
        /// Used during save to preserve events for players who didn't join this session.
        /// </summary>
        public static List<SpeechEvent> GetPendingEvents()
        {
            var pending = new List<SpeechEvent>();
            foreach (var entry in _pool.Values)
            {
                if (entry.state == EventState.Pending)
                    pending.Add(entry.ev);
            }
            return pending;
        }

        /// <summary>
        /// Whether there are any PENDING events.
        /// </summary>
        public static bool HasPending()
        {
            foreach (var entry in _pool.Values)
                if (entry.state == EventState.Pending) return true;
            return false;
        }

        /// <summary>
        /// Total loaded event count.
        /// </summary>
        public static int TotalCount => _pool.Count;

        /// <summary>
        /// Whether the pool has been loaded for any slot.
        /// </summary>
        public static bool IsLoaded => _loadedSlotId >= 0 && _pool.Count > 0;

        /// <summary>
        /// Fix RecordedTime and LastPlayedTime on an event so the warmup filter works.
        /// </summary>
        public static void FixEventTiming(SpeechEvent ev, float currentTime)
        {
            if (RecordedTimeField != null)
                RecordedTimeField.SetValue(ev, currentTime);
            if (LastPlayedTimeField != null)
                LastPlayedTimeField.SetValue(ev, currentTime);
        }

        /// <summary>
        /// Get the current session time via reflection.
        /// </summary>
        public static float GetCurrentSessionTime()
        {
            try
            {
                object? timeutil = MimesisSaveManager.GetHubMember("timeutil");
                if (timeutil != null)
                {
                    var getTickMethod = timeutil.GetType().GetMethod("GetCurrentTickSec",
                        BindingFlags.Public | BindingFlags.Instance);
                    if (getTickMethod != null)
                        return (float)(int)getTickMethod.Invoke(timeutil, null);
                }
            }
            catch { }
            return 0f;
        }

        // ===================== Disconnected Cache Methods =====================

        /// <summary>
        /// Cache all SpeechEvents from an archive that is about to be destroyed
        /// (player disconnecting). Also caches the player's SteamID -> DissonanceID mapping.
        /// Called from SpeechEventArchiveDisconnectPatches.Prefix (before OnStopClient).
        /// </summary>
        public static int CacheEventsFromArchive(SpeechEventArchive archive)
        {
            if (archive == null) return 0;

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

                // Don't cache the host's own archive (host doesn't "disconnect")
                if (isLocal) return 0;

                // Extract events from the SyncList via reflection
                var eventsField = typeof(SpeechEventArchive).GetField("events", BindingFlags.Public | BindingFlags.Instance);
                if (eventsField == null) return 0;

                var syncList = eventsField.GetValue(archive);
                if (syncList == null) return 0;

                var countProp = syncList.GetType().GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);
                var indexer = syncList.GetType().GetProperty("Item", new[] { typeof(int) });
                if (countProp == null || indexer == null) return 0;

                int count = (int)countProp.GetValue(syncList);
                int cached = 0;

                for (int i = 0; i < count; i++)
                {
                    var ev = indexer.GetValue(syncList, new object[] { i }) as SpeechEvent;
                    if (ev != null && !_disconnectedCache.ContainsKey(ev.Id))
                    {
                        _disconnectedCache[ev.Id] = ev;
                        cached++;
                    }
                }

                // Cache player mapping (SteamID -> DissonanceID)
                ulong steamId = GetSteamIdForPlayerUID(playerUID, false);
                if (steamId != 0 && !string.IsNullOrEmpty(playerId))
                {
                    _disconnectedPlayerMappings[steamId] = playerId;
                    ModLog.Debug("Persistence", $"Cached player mapping: SteamID {steamId} -> '{playerId}'");
                }

                ModLog.Debug("Persistence", $"Disconnect cache — {VoiceEventStats.DescribePlayerVerbose(archive)} — cached {cached} events (totalCache={_disconnectedCache.Count})");
                return cached;
            }
            catch (Exception ex)
            {
                ModLog.Warn("Persistence", $"CacheEventsFromArchive error: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Try to reclaim cached events for a player who reconnected mid-session.
        /// Matches by SteamID: resolves the archive's SteamID, finds the old DissonanceID
        /// in _disconnectedPlayerMappings, then returns all cached events with that PlayerName.
        /// Claimed events are removed from the disconnected cache.
        /// </summary>
        public static List<SpeechEvent> ClaimDisconnectedEventsForArchive(string? playerId, long playerUID, bool isLocal)
        {
            var claimed = new List<SpeechEvent>();
            if (_disconnectedCache.Count == 0) return claimed;

            // Collect all DissonanceIDs that belong to this player
            var matchedPlayerNames = new HashSet<string>();

            // Direct match: same DissonanceID as before
            if (!string.IsNullOrEmpty(playerId))
                matchedPlayerNames.Add(playerId);

            // SteamID match: resolve SteamID -> look up old DissonanceID in disconnected mappings
            ulong steamId = GetSteamIdForPlayerUID(playerUID, isLocal);
            if (steamId != 0 && _disconnectedPlayerMappings.TryGetValue(steamId, out string oldDissonanceId))
            {
                matchedPlayerNames.Add(oldDissonanceId);
                ModLog.Debug("Persistence", $"Disconnected cache match: SteamID {steamId} -> old DissonanceID '{oldDissonanceId}'");
            }

            if (matchedPlayerNames.Count == 0) return claimed;

            // Find all cached events whose PlayerName matches
            var idsToRemove = new List<long>();
            foreach (var kvp in _disconnectedCache)
            {
                string evPlayerName = kvp.Value.PlayerName ?? "";
                if (matchedPlayerNames.Contains(evPlayerName))
                {
                    // Update PlayerName to the current DissonanceID
                    if (!string.IsNullOrEmpty(playerId))
                        kvp.Value.PlayerName = playerId;

                    claimed.Add(kvp.Value);
                    idsToRemove.Add(kvp.Key);
                }
            }

            // Remove claimed events from cache
            foreach (long id in idsToRemove)
                _disconnectedCache.Remove(id);

            // Clean up the player mapping if all events for this player were claimed
            if (steamId != 0 && claimed.Count > 0)
                _disconnectedPlayerMappings.Remove(steamId);

            if (claimed.Count > 0)
            {
                ModLog.Debug("Persistence", $"Reclaimed {claimed.Count} events from disconnected cache " +
                                $"for PlayerId='{playerId}' (remaining cache: {_disconnectedCache.Count})");
            }

            return claimed;
        }

        /// <summary>
        /// Get all cached events from disconnected players.
        /// </summary>
        public static List<SpeechEvent> GetDisconnectedEvents()
        {
            return new List<SpeechEvent>(_disconnectedCache.Values);
        }

        /// <summary>
        /// Get cached player mappings from disconnected players.
        /// </summary>
        public static Dictionary<ulong, string> GetDisconnectedPlayerMappings()
        {
            return new Dictionary<ulong, string>(_disconnectedPlayerMappings);
        }

        /// <summary>
        /// Number of events in the disconnected cache.
        /// </summary>
        public static int DisconnectedCacheCount => _disconnectedCache.Count;

        /// <summary>
        /// Reset all state. Called when switching slots or cleaning up.
        /// </summary>
        public static void Reset()
        {
            _pool.Clear();
            _byPlayerName.Clear();
            _steamToDissonance.Clear();
            _deferredNameUpdates.Clear();
            _deferredInjectionArchives.Clear();
            _disconnectedCache.Clear();
            _disconnectedPlayerMappings.Clear();
            _loadedSlotId = -1;
            _localArchive = null;
        }

        // ===================== Player Mapping =====================

        /// <summary>
        /// Save the SteamID -> DissonanceID mapping for all current archives.
        /// Uses Hub.s.pdata.actorUIDToSteamID to resolve PlayerUID -> SteamID.
        /// Called during SaveMimesisData.
        /// </summary>
        public static void SavePlayerMapping(int slotId)
        {
            string? slotPath = MimesisSaveManager.GetMimesisSlotPath(slotId);
            if (string.IsNullOrEmpty(slotPath))
            {
                ModLog.Warn("Persistence", "SavePlayerMapping: slot path is null/empty!");
                return;
            }

            try
            {
                var archives = UnityEngine.Object.FindObjectsByType<SpeechEventArchive>(FindObjectsSortMode.None) ?? Array.Empty<SpeechEventArchive>();

                ModLog.Debug("Persistence", $"SavePlayerMapping: found {archives.Length} live archives");

                var mapping = new Dictionary<ulong, string>();
                foreach (var archive in archives)
                {
                    try
                    {
                        string pid = archive.PlayerId;
                        long uid = archive.PlayerUID;
                        bool isLocal = archive.IsLocal;

                        if (string.IsNullOrEmpty(pid))
                        {
                            ModLog.Debug("Persistence", $"  Archive skipped: empty PlayerId (UID={uid})");
                            continue;
                        }

                        // Use isLocal fallback for host (actorUIDToSteamID doesn't have host entry)
                        ulong steamId = GetSteamIdForPlayerUID(uid, isLocal);
                        ModLog.Debug("Persistence", $"  Archive: PlayerId='{pid}', PlayerUID={uid}, IsLocal={isLocal}, SteamID={steamId}");

                        if (steamId != 0)
                        {
                            mapping[steamId] = pid;
                        }
                        else
                        {
                            ModLog.Warn("Persistence", $"  Could not resolve SteamID for PlayerUID={uid}");
                        }
                    }
                    catch (Exception archEx)
                    {
                        ModLog.Warn("Persistence", $"  Archive error: {archEx.Message}");
                    }
                }

                // Merge with cached mappings from disconnected players
                // (disconnected mappings are added first, live ones overwrite if duplicate)
                var disconnectedMappings = GetDisconnectedPlayerMappings();
                int disconnectedCount = 0;
                foreach (var kvp in disconnectedMappings)
                {
                    if (!mapping.ContainsKey(kvp.Key))
                    {
                        mapping[kvp.Key] = kvp.Value;
                        disconnectedCount++;
                    }
                }
                if (disconnectedCount > 0)
                    ModLog.Debug("Persistence", $"Added {disconnectedCount} disconnected player mappings");

                // Always create the file, even if empty (so we know save was attempted)
                // JSON: {"steamId":"dissonanceId", ...}
                var entries = new List<string>();
                foreach (var kvp in mapping)
                {
                    string val = EscapeJsonString(kvp.Value);
                    entries.Add($"\"{kvp.Key}\":\"{val}\"");
                }
                string json = "{" + string.Join(",", entries) + "}";

                Directory.CreateDirectory(slotPath);
                string filePath = Path.Combine(slotPath, PlayerMappingFile);
                MimesisSaveManager.SafeWritePlayerMapping(filePath, json);
                ModLog.Debug("Persistence", $"Saved player mapping: {mapping.Count} entries ({disconnectedCount} from cache) -> {filePath}");
            }
            catch (Exception ex)
            {
                ModLog.Error("Persistence", $"SavePlayerMapping FAILED: {ex}");
            }
        }

        /// <summary>
        /// Load the SteamID -> old DissonanceID mapping from disk.
        /// </summary>
        private static void LoadPlayerMapping(int slotId)
        {
            _steamToDissonance.Clear();

            string? slotPath = MimesisSaveManager.GetMimesisSlotPath(slotId);
            if (string.IsNullOrEmpty(slotPath)) return;

            string filePath = Path.Combine(slotPath, PlayerMappingFile);
            string? json = MimesisSaveManager.SafeReadPlayerMapping(filePath);
            if (string.IsNullOrEmpty(json))
            {
                ModLog.Debug("Persistence", $"No player_mapping.json at {filePath}");
                return;
            }

            try
            {
                // Parse {"steamId":"dissonanceId", ...}
                var mapping = ParseSteamToDissonanceJson(json);

                foreach (var kvp in mapping)
                {
                    _steamToDissonance[kvp.Key] = kvp.Value;
                }

                ModLog.Debug("Persistence", $"Loaded player mapping: {_steamToDissonance.Count} entries");
                foreach (var kvp in _steamToDissonance)
                    ModLog.Debug("Persistence", $"  SteamID {kvp.Key} -> DissonanceID '{kvp.Value}'");
            }
            catch (Exception ex)
            {
                ModLog.Warn("Persistence", $"LoadPlayerMapping: {ex.Message}");
            }
        }

        /// <summary>
        /// Parse JSON {"ulongKey":"stringValue", ...} -> Dictionary&lt;ulong, string&gt;.
        /// </summary>
        private static Dictionary<ulong, string> ParseSteamToDissonanceJson(string json)
        {
            var result = new Dictionary<ulong, string>();
            if (string.IsNullOrEmpty(json)) return result;

            // Strip outer braces
            json = json.Trim();
            if (json.StartsWith("{")) json = json.Substring(1);
            if (json.EndsWith("}")) json = json.Substring(0, json.Length - 1);
            json = json.Trim();
            if (string.IsNullOrEmpty(json)) return result;

            // Parse "key":"value" pairs
            int pos = 0;
            while (pos < json.Length)
            {
                // Find key (quoted ulong)
                int keyStart = json.IndexOf('"', pos);
                if (keyStart < 0) break;
                int keyEnd = json.IndexOf('"', keyStart + 1);
                if (keyEnd < 0) break;
                string keyStr = json.Substring(keyStart + 1, keyEnd - keyStart - 1);

                // Find colon
                int colon = json.IndexOf(':', keyEnd + 1);
                if (colon < 0) break;

                // Find value (quoted string)
                int valStart = json.IndexOf('"', colon + 1);
                if (valStart < 0) break;
                int valEnd = json.IndexOf('"', valStart + 1);
                if (valEnd < 0) break;
                string valStr = json.Substring(valStart + 1, valEnd - valStart - 1);

                if (ulong.TryParse(keyStr, out ulong steamId))
                    result[steamId] = valStr;

                // Move past the value, look for comma or end
                pos = valEnd + 1;
                int comma = json.IndexOf(',', pos);
                pos = (comma >= 0) ? comma + 1 : json.Length;
            }

            return result;
        }

        private static string EscapeJsonString(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
