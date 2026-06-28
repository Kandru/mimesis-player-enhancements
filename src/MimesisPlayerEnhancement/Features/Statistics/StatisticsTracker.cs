using System;
using System.Collections.Generic;
using System.Reflection;
using Mimic.Actors;
using Mimic.Voice.SpeechSystem;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using MimesisPlayerEnhancement.Features.WebDashboard;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    public static class StatisticsTracker
    {
        private const string Feature = "Statistics";
        private const BindingFlags InstanceMemberFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo? HubVworldField =
            typeof(Hub).GetField("vworld", InstanceMemberFlags);

        private static readonly PropertyInfo? HubVworldProperty =
            typeof(Hub).GetProperty("vworld", InstanceMemberFlags);

        private static FieldInfo? _vRoomManagerField;
        private static FieldInfo? _gameSessionInfoField;
        private static PropertyInfo? _playReportManagerProperty;

        private static readonly Dictionary<ulong, PlayerStatisticsDocument> _players = [];
        private static readonly Dictionary<ulong, DateTime> _connectedSince = [];
        private static readonly Dictionary<ulong, PlayReportSnapshot> _cycleBaselines = [];
        private static readonly Dictionary<ulong, int> _voiceEventBaselines = [];
        private static readonly Dictionary<ulong, int> _lastKillCounts = [];
        private static readonly Dictionary<int, ulong> _actorToSteam = [];

        private static int _loadedSlotId = -999;
        private static long _lastCurrencyBaseline;
        private static int _lastCycleCount;
        private static Dictionary<ulong, int>? _voiceCountCache;

        private sealed class PlayReportSnapshot
        {
            public long ItemCarryCount;
            public long DamageToAlly;
            public long MimicEncounterCount;
            public long TimeInStartingVolumeMs;
        }

        internal static void ClearRuntimeState()
        {
            _players.Clear();
            _connectedSince.Clear();
            _cycleBaselines.Clear();
            _voiceEventBaselines.Clear();
            _lastKillCounts.Clear();
            _actorToSteam.Clear();
            _loadedSlotId = -999;
            _lastCurrencyBaseline = 0;
            _lastCycleCount = 0;
            StatisticsWriteQueue.Clear();
        }

        public static void LoadForSlot(int slotId)
        {
            if (!MimesisSaveManager.IsValidSaveSlotId(slotId))
            {
                return;
            }

            if (slotId == _loadedSlotId)
            {
                return;
            }

            _loadedSlotId = slotId;
            _players.Clear();
            _connectedSince.Clear();
            _cycleBaselines.Clear();
            _voiceEventBaselines.Clear();
            _lastKillCounts.Clear();
            _actorToSteam.Clear();
            StatisticsStore.LoadAllPlayersForSlot(slotId, _players);
            ResetCycleBaselines();
            StatisticsWriteQueue.Configure(
                slotId,
                steamId => _players.TryGetValue(steamId, out PlayerStatisticsDocument? doc) ? doc : null,
                PersistLeaderboardDocument);
            ModLog.Info(Feature, $"Loaded statistics for save slot {slotId} ({_players.Count} players).");
        }

        internal static void HandleArchiveStarted(SpeechEventArchive archive, int slotId)
        {
            if (!ModConfig.EnableStatistics.Value)
            {
                return;
            }

            if (!MimesisSaveManager.IsValidSaveSlotId(slotId))
            {
                return;
            }

            if (slotId != _loadedSlotId)
            {
                LoadForSlot(slotId);
            }

            if (!IsArchiveIdentityReady(archive))
            {
                return;
            }

            ulong steamId = ResolveSteamIdFromArchive(archive);
            if (steamId == 0)
            {
                return;
            }

            OnPlayerRegistered(steamId, slotId);
        }

        public static void OnPlayerRegistered(ulong steamId, int slotId)
        {
            if (!ModConfig.EnableStatistics.Value)
            {
                return;
            }

            if (steamId == 0)
            {
                return;
            }

            if (!MimesisSaveManager.IsValidSaveSlotId(slotId))
            {
                return;
            }

            if (_connectedSince.ContainsKey(steamId))
            {
                return;
            }

            LoadForSlot(slotId);

            PlayerStatisticsDocument doc = GetOrCreatePlayer(steamId);
            doc.DisplayName = ResolveDisplayName(steamId, doc.DisplayName);
            DateTime now = DateTime.UtcNow;
            int graceMinutes = ModConfig.SessionReconnectGraceMinutes.Value;

            bool resumeSession = doc.CurrentSession != null
                                 && doc.CurrentSession.IsOpen
                                 && doc.CurrentSession.LastDisconnectedAtUtc.HasValue
                                 && now - doc.CurrentSession.LastDisconnectedAtUtc.Value <= TimeSpan.FromMinutes(graceMinutes);

            if (resumeSession && doc.CurrentSession != null)
            {
                doc.CurrentSession.ReconnectCount++;
                doc.CurrentSession.LastConnectedAtUtc = now;
                doc.CurrentSession.LastDisconnectedAtUtc = null;
                ModLog.Info(Feature, $"Player resumed session — steamId={steamId} session={doc.CurrentSession.SessionId} reconnects={doc.CurrentSession.ReconnectCount}");
            }
            else
            {
                FinalizeOpenSession(doc, countAsCompleted: true);
                doc.CurrentSession = NewSession(now);
                ModLog.Info(Feature, $"Player started session — steamId={steamId} session={doc.CurrentSession.SessionId}");
            }

            _connectedSince[steamId] = now;
            EnsureCycleBaseline(steamId);
            EnsureVoiceBaseline(steamId);
            StatisticsWriteQueue.SavePlayerImmediate(slotId, doc);
            PersistLeaderboardImmediate(slotId);

            bool isNewSession = !resumeSession;
            int reconnectCount = doc.CurrentSession?.ReconnectCount ?? 0;
            StatisticsMessages.OnPlayerJoinedSession(steamId, doc.DisplayName, doc, isNewSession, reconnectCount);
            WebDashboardSnapshotCache.MarkDirty();
        }

        public static void OnPlayerUnregistered(ulong steamId)
        {
            if (!CanTrack())
            {
                return;
            }

            if (steamId == 0)
            {
                return;
            }

            if (!_players.TryGetValue(steamId, out PlayerStatisticsDocument? doc))
            {
                doc = GetOrCreatePlayer(steamId);
            }

            FlushConnectedTime(steamId, doc);
            _ = _connectedSince.Remove(steamId);
            if (doc.CurrentSession != null)
            {
                doc.CurrentSession.LastDisconnectedAtUtc = DateTime.UtcNow;
                doc.CurrentSession.IsOpen = true;
            }

            ModLog.Info(Feature, $"Player disconnected — steamId={steamId} displayName={doc.DisplayName}");
            StatisticsWriteQueue.FlushAllSync();
            StatisticsWriteQueue.SavePlayerImmediate(_loadedSlotId, doc);
            PersistLeaderboardImmediate(_loadedSlotId);
            StatisticsMessages.OnPlayerLeftSession(steamId, doc.DisplayName, doc);
            WebDashboardSnapshotCache.MarkDirty();
        }

        public static void ProcessDeferred()
        {
            if (!CanTrack())
            {
                return;
            }

            if (_connectedSince.Count == 0 && !HasOpenDisconnectedSessions())
            {
                return;
            }

            int graceMinutes = ModConfig.SessionReconnectGraceMinutes.Value;
            DateTime now = DateTime.UtcNow;
            int slotId = _loadedSlotId;
            bool changed = false;

            foreach (KeyValuePair<ulong, PlayerStatisticsDocument> kvp in _players)
            {
                PlayerStatisticsDocument doc = kvp.Value;
                SessionStats? session = doc.CurrentSession;
                if (session == null || !session.IsOpen || !session.LastDisconnectedAtUtc.HasValue)
                {
                    continue;
                }

                if (now - session.LastDisconnectedAtUtc.Value <= TimeSpan.FromMinutes(graceMinutes))
                {
                    continue;
                }

                ModLog.Info(Feature, $"Session finalized — steamId={kvp.Key} session={session.SessionId} after grace period");
                FinalizeOpenSession(doc, countAsCompleted: true);
                StatisticsWriteQueue.SavePlayerImmediate(slotId, doc);
                changed = true;
            }

            foreach (ulong steamId in _connectedSince.Keys)
            {
                if (!_players.TryGetValue(steamId, out PlayerStatisticsDocument? doc))
                {
                    continue;
                }

                if (doc.CurrentSession?.LastDisconnectedAtUtc.HasValue == true)
                {
                    continue;
                }

                FlushConnectedTime(steamId, doc);
            }

            if (changed)
            {
                PersistLeaderboardImmediate(slotId);
            }
        }

        private static bool HasOpenDisconnectedSessions()
        {
            foreach (PlayerStatisticsDocument doc in _players.Values)
            {
                SessionStats? session = doc.CurrentSession;
                if (session != null && session.IsOpen && session.LastDisconnectedAtUtc.HasValue)
                {
                    return true;
                }
            }

            return false;
        }

        public static void OnCycleCompleted(PlayReportManager? manager)
        {
            if (!CanTrack())
            {
                return;
            }

            int slotId = _loadedSlotId;

            long currencyNow = manager?.AccumulatedCurrency ?? 0;
            long currencyDelta = Math.Max(0, currencyNow - _lastCurrencyBaseline);
            _lastCurrencyBaseline = currencyNow;

            int cycleNumber = manager?.AccumulatedCycleCount ?? ++_lastCycleCount;
            _lastCycleCount = cycleNumber;

            if (currencyDelta > 0)
            {
                ulong hostSteam = ResolveSteamIdFromUid(0, isLocal: true);
                if (hostSteam != 0)
                {
                    PlayerStatisticsDocument hostDoc = GetOrCreatePlayer(hostSteam);
                    hostDoc.CurrentSession ??= NewSession(DateTime.UtcNow);
                    hostDoc.CurrentSession.Counters.CurrencyEarned += currencyDelta;
                    hostDoc.Global.Counters.CurrencyEarned += currencyDelta;
                }
            }

            Dictionary<ulong, PlayReportData>? playReports = manager?.CurrentReportDict;
            HashSet<ulong> affected = [];

            if (playReports != null)
            {
                foreach (KeyValuePair<ulong, PlayReportData> kvp in playReports)
                {
                    ulong steamId = kvp.Key;
                    if (steamId == 0)
                    {
                        continue;
                    }

                    _ = affected.Add(steamId);
                    ApplyPlayReportDelta(steamId, kvp.Value, cycleNumber);
                }
            }

            foreach (ulong steamId in _connectedSince.Keys)
            {
                _ = affected.Add(steamId);
            }

            List<ulong> connectedSteamIds = [.. _connectedSince.Keys];

            foreach (ulong steamId in connectedSteamIds)
            {
                if (_players.TryGetValue(steamId, out PlayerStatisticsDocument? connectedDoc))
                {
                    FlushConnectedTime(steamId, connectedDoc);
                }
            }

            _voiceCountCache = BuildVoiceCountCache();
            StatisticsWriteQueue.FlushAllSync();
            foreach (ulong steamId in affected)
            {
                PlayerStatisticsDocument doc = GetOrCreatePlayer(steamId);
                doc.DisplayName = ResolveDisplayName(steamId, doc.DisplayName);
                ApplyVoiceDelta(steamId, doc);
                StatisticsWriteQueue.SavePlayerImmediate(slotId, doc);
            }
            _voiceCountCache = null;

            ResetCycleBaselines(manager);
            PersistLeaderboardImmediate(slotId);

            if (ModConfig.ShowStatisticsToasts.Value)
            {
                StatisticsMessages.OnCycleCompleted(cycleNumber);
            }

            ModLog.Info(Feature, $"Cycle {cycleNumber} statistics saved for slot {slotId} ({affected.Count} players).");
            WebDashboardSnapshotCache.MarkDirty();
        }

        public static void OnGameSaved(int slotId)
        {
            if (!MimesisSaveManager.IsHost())
            {
                return;
            }

            if (!ModConfig.EnableStatistics.Value)
            {
                return;
            }

            if (!MimesisSaveManager.IsValidSaveSlotId(slotId))
            {
                return;
            }

            LoadForSlot(slotId);
            StatisticsWriteQueue.FlushAllSync();
            foreach (PlayerStatisticsDocument doc in _players.Values)
            {
                StatisticsWriteQueue.SavePlayerImmediate(slotId, doc);
            }

            PersistLeaderboardImmediate(slotId);
            ModLog.Debug(Feature, $"Statistics persisted on game save for slot {slotId}.");
            WebDashboardSnapshotCache.MarkDirty();
        }

        public static void OnPlayerDeath(ProtoActor actor)
        {
            if (!CanTrack() || actor == null)
            {
                return;
            }

            ulong steamId = ResolveSteamIdFromActor(actor);
            if (steamId == 0)
            {
                return;
            }

            PlayerStatisticsDocument doc = GetOrCreatePlayer(steamId);
            doc.CurrentSession ??= NewSession(DateTime.UtcNow);
            doc.CurrentSession.Counters.Deaths++;
            doc.Global.Counters.Deaths++;
            RegisterActorMapping(actor, steamId);
            PersistCombatStats(steamId, doc);
        }

        public static void OnPlayerRevive(ProtoActor actor)
        {
            if (!CanTrack() || actor == null)
            {
                return;
            }

            ulong steamId = ResolveSteamIdFromActor(actor);
            if (steamId == 0)
            {
                return;
            }

            PlayerStatisticsDocument doc = GetOrCreatePlayer(steamId);
            doc.CurrentSession ??= NewSession(DateTime.UtcNow);
            doc.CurrentSession.Counters.Revives++;
            doc.Global.Counters.Revives++;
            RegisterActorMapping(actor, steamId);
            PersistCombatStats(steamId, doc);
        }

        public static void OnKillCountChanged(ProtoActor actor, int killCount)
        {
            if (!CanTrack() || actor == null)
            {
                return;
            }

            ulong steamId = ResolveSteamIdFromActor(actor);
            if (steamId == 0)
            {
                return;
            }

            RegisterActorMapping(actor, steamId);
            PlayerStatisticsDocument doc = GetOrCreatePlayer(steamId);
            int previous = _lastKillCounts.TryGetValue(steamId, out int prev) ? prev : 0;

            if (previous == 0 && killCount <= doc.Global.Counters.Kills)
            {
                _lastKillCounts[steamId] = killCount;
                return;
            }

            if (killCount <= previous)
            {
                _lastKillCounts[steamId] = killCount;
                return;
            }

            int delta = killCount - previous;
            _lastKillCounts[steamId] = killCount;

            doc.CurrentSession ??= NewSession(DateTime.UtcNow);
            doc.CurrentSession.Counters.Kills += delta;
            doc.Global.Counters.Kills += delta;
            PersistCombatStats(steamId, doc);
        }

        public static void OnUpdate()
        {
            if (!CanTrack())
            {
                return;
            }

            ProcessDeferred();
            StatisticsWriteQueue.ProcessDebounced();
        }

        private static void PersistCombatStats(ulong steamId, PlayerStatisticsDocument doc)
        {
            _ = doc;
            StatisticsWriteQueue.MarkDirty(steamId);
        }

        private static bool CanTrack()
        {
            return ModConfig.EnableStatistics.Value
            && _loadedSlotId >= 0
            && MimesisSaveManager.IsValidSaveSlotId(_loadedSlotId)
            && MimesisSaveManager.IsHost();
        }

        private static PlayerStatisticsDocument GetOrCreatePlayer(ulong steamId)
        {
            if (!_players.TryGetValue(steamId, out PlayerStatisticsDocument? doc))
            {
                doc = StatisticsStore.LoadPlayer(_loadedSlotId, steamId);
                doc.SteamId = steamId;
                _players[steamId] = doc;
            }
            return doc;
        }

        internal static PlayerStatisticsDocument? TryGetPlayerDocument(ulong steamId)
        {
            return _players.TryGetValue(steamId, out PlayerStatisticsDocument? doc) ? doc : null;
        }

        internal static IReadOnlyList<PlayerStatisticsDocument> GetCachedPlayerDocuments()
        {
            return new List<PlayerStatisticsDocument>(_players.Values);
        }

        internal static IReadOnlyCollection<ulong> GetConnectedSteamIds()
        {
            return _connectedSince.Keys;
        }

        internal static ulong TryResolveSteamId(ProtoActor actor)
        {
            return ResolveSteamIdFromActor(actor);
        }

        internal static bool TryGetCurrentPlayReport(ulong steamId, out PlayReportData report)
        {
            PlayReportData? found = TryGetPlayReport(steamId);
            if (found != null)
            {
                report = found;
                return true;
            }

            report = null!;
            return false;
        }

        internal static bool TryGetSessionCounters(ulong steamId, out StatCounters counters)
        {
            counters = new StatCounters();
            if (steamId == 0)
            {
                return false;
            }

            if (_players.TryGetValue(steamId, out PlayerStatisticsDocument? doc) && doc.CurrentSession?.Counters != null)
            {
                counters = doc.CurrentSession.Counters.Clone();
                return true;
            }

            return false;
        }

        private static SessionStats NewSession(DateTime now)
        {
            return new()
            {
                SessionId = Guid.NewGuid().ToString("N"),
                StartedAtUtc = now,
                LastConnectedAtUtc = now,
                IsOpen = true,
                Counters = new StatCounters(),
            };
        }

        private static void FinalizeOpenSession(PlayerStatisticsDocument doc, bool countAsCompleted)
        {
            if (doc.CurrentSession == null || !doc.CurrentSession.IsOpen)
            {
                return;
            }

            doc.CurrentSession.IsOpen = false;
            doc.RecentSessions.Add(CloneSession(doc.CurrentSession));
            while (doc.RecentSessions.Count > StatisticsStore.MaxRecentSessionsPerPlayer)
            {
                doc.RecentSessions.RemoveAt(0);
            }

            if (countAsCompleted)
            {
                doc.Global.SessionsCompleted++;
            }

            doc.CurrentSession = null;
        }

        private static SessionStats CloneSession(SessionStats session)
        {
            return new()
            {
                SessionId = session.SessionId,
                StartedAtUtc = session.StartedAtUtc,
                LastConnectedAtUtc = session.LastConnectedAtUtc,
                LastDisconnectedAtUtc = session.LastDisconnectedAtUtc,
                ReconnectCount = session.ReconnectCount,
                IsOpen = false,
                Counters = session.Counters.Clone(),
            };
        }

        private static void FlushConnectedTime(ulong steamId, PlayerStatisticsDocument doc)
        {
            if (!_connectedSince.TryGetValue(steamId, out DateTime since))
            {
                return;
            }

            long seconds = (long)Math.Max(0, (DateTime.UtcNow - since).TotalSeconds);
            if (seconds <= 0)
            {
                return;
            }

            doc.CurrentSession ??= NewSession(since);
            doc.CurrentSession.Counters.TotalConnectedSeconds += seconds;
            doc.Global.Counters.TotalConnectedSeconds += seconds;
            _connectedSince[steamId] = DateTime.UtcNow;
        }

        private static void ApplyPlayReportDelta(ulong steamId, PlayReportData report, int cycleNumber)
        {
            PlayerStatisticsDocument doc = GetOrCreatePlayer(steamId);
            doc.CurrentSession ??= NewSession(DateTime.UtcNow);

            if (!_cycleBaselines.TryGetValue(steamId, out PlayReportSnapshot? baseline))
            {
                baseline = SnapshotReport(report);
                _cycleBaselines[steamId] = baseline;
            }

            StatCounters delta = new()
            {
                ItemCarryCount = Math.Max(0, report.TotalItemCarryCount - baseline.ItemCarryCount),
                DamageToAlly = Math.Max(0, report.TotalDamageToAlly - baseline.DamageToAlly),
                MimicEncounterCount = Math.Max(0, report.TotalMimicEncounterCount - baseline.MimicEncounterCount),
                TimeInStartingVolumeMs = Math.Max(0, report.TotalTimeInStartingVolume - baseline.TimeInStartingVolumeMs),
                CyclesCompleted = 1,
            };

            MergeDelta(doc, delta);
            _cycleBaselines[steamId] = SnapshotReport(report);
        }

        private static void ApplyVoiceDelta(ulong steamId, PlayerStatisticsDocument doc)
        {
            int current = CountVoiceEventsForSteamId(steamId);
            int baseline = _voiceEventBaselines.TryGetValue(steamId, out int b) ? b : current;
            int delta = Math.Max(0, current - baseline);
            if (delta == 0)
            {
                return;
            }

            doc.CurrentSession ??= NewSession(DateTime.UtcNow);
            doc.CurrentSession.Counters.VoiceEvents += delta;
            doc.Global.Counters.VoiceEvents += delta;
            _voiceEventBaselines[steamId] = current;
        }

        private static void MergeDelta(PlayerStatisticsDocument doc, StatCounters delta)
        {
            doc.CurrentSession ??= NewSession(DateTime.UtcNow);
            doc.CurrentSession.Counters.Add(delta);
            doc.Global.Counters.Add(delta);
        }

        private static PlayReportSnapshot SnapshotReport(PlayReportData report)
        {
            return new()
            {
                ItemCarryCount = report.TotalItemCarryCount,
                DamageToAlly = report.TotalDamageToAlly,
                MimicEncounterCount = report.TotalMimicEncounterCount,
                TimeInStartingVolumeMs = report.TotalTimeInStartingVolume,
            };
        }

        private static void EnsureCycleBaseline(ulong steamId)
        {
            if (_cycleBaselines.ContainsKey(steamId))
            {
                return;
            }

            PlayReportData? report = TryGetPlayReport(steamId);
            if (report != null)
            {
                _cycleBaselines[steamId] = SnapshotReport(report);
            }
        }

        private static void EnsureVoiceBaseline(ulong steamId)
        {
            if (_voiceEventBaselines.ContainsKey(steamId))
            {
                return;
            }

            _voiceEventBaselines[steamId] = CountVoiceEventsForSteamId(steamId);
        }

        private static void ResetCycleBaselines(PlayReportManager? manager = null)
        {
            _cycleBaselines.Clear();
            _voiceEventBaselines.Clear();
            manager ??= TryGetPlayReportManager();
            _lastCurrencyBaseline = manager?.AccumulatedCurrency ?? _lastCurrencyBaseline;
            _lastCycleCount = manager?.AccumulatedCycleCount ?? _lastCycleCount;

            Dictionary<ulong, PlayReportData>? dict = manager?.CurrentReportDict;
            if (dict == null)
            {
                return;
            }

            foreach (KeyValuePair<ulong, PlayReportData> kvp in dict)
            {
                if (kvp.Key == 0)
                {
                    continue;
                }

                _cycleBaselines[kvp.Key] = SnapshotReport(kvp.Value);
                _voiceEventBaselines[kvp.Key] = CountVoiceEventsForSteamId(kvp.Key);
            }
        }

        private static PlayReportData? TryGetPlayReport(ulong steamId)
        {
            Dictionary<ulong, PlayReportData>? dict = TryGetPlayReportManager()?.CurrentReportDict;
            return dict == null ? null : dict.TryGetValue(steamId, out PlayReportData? report) ? report : null;
        }

        private static PlayReportManager? TryGetPlayReportManager()
        {
            try
            {
                object? vworld = Hub.s == null
                    ? null
                    : HubVworldField?.GetValue(Hub.s) ?? HubVworldProperty?.GetValue(Hub.s);
                if (vworld == null)
                {
                    return null;
                }

                EnsurePlayReportChainResolved(vworld.GetType());

                if (_vRoomManagerField == null
                    || _gameSessionInfoField == null
                    || _playReportManagerProperty == null)
                {
                    return null;
                }

                object? roomManager = _vRoomManagerField.GetValue(vworld);
                if (roomManager == null)
                {
                    return null;
                }

                object? sessionInfo = _gameSessionInfoField.GetValue(roomManager);
                return sessionInfo == null ? null : _playReportManagerProperty.GetValue(sessionInfo) as PlayReportManager;
            }
            catch
            {
                return null;
            }
        }

        private static void EnsurePlayReportChainResolved(Type vworldType)
        {
            if (_vRoomManagerField != null)
            {
                return;
            }

            _vRoomManagerField = vworldType.GetField("_vRoomManager", InstanceMemberFlags);
            if (_vRoomManagerField == null)
            {
                return;
            }

            Type roomManagerType = _vRoomManagerField.FieldType;
            _gameSessionInfoField = roomManagerType.GetField("_gameSessionInfo", InstanceMemberFlags);
            if (_gameSessionInfoField == null)
            {
                return;
            }

            Type sessionInfoType = _gameSessionInfoField.FieldType;
            _playReportManagerProperty = sessionInfoType.GetProperty("PlayReportManager", InstanceMemberFlags);
        }

        private static Dictionary<ulong, int> BuildVoiceCountCache()
        {
            Dictionary<ulong, int> cache = [];
            try
            {
                IEnumerable<SpeechEventArchive> archives = SpeechEventArchiveRegistry.EnumerateActive();
                foreach (SpeechEventArchive archive in archives)
                {
                    if (archive == null)
                    {
                        continue;
                    }

                    long playerUid = 0;
                    bool isLocal = false;
                    try
                    {
                        playerUid = archive.PlayerUID;
                        isLocal = archive.IsLocal;
                    }
                    catch { /* not ready */ }

                    ulong archiveSteam = ResolveSteamIdFromUid(playerUid, isLocal);
                    if (archiveSteam == 0)
                    {
                        continue;
                    }

                    _ = cache.TryGetValue(archiveSteam, out int current);
                    cache[archiveSteam] = current + VoiceEventStats.GetEventCount(archive);
                }
            }
            catch { /* ignore */ }

            return cache;
        }

        private static int CountVoiceEventsForSteamId(ulong steamId)
        {
            return _voiceCountCache != null && _voiceCountCache.TryGetValue(steamId, out int cached) ? cached : 0;
        }

        private static ulong ResolveSteamIdFromArchive(SpeechEventArchive archive)
        {
            long playerUid = 0;
            bool isLocal = false;
            try
            {
                playerUid = archive.PlayerUID;
                isLocal = archive.IsLocal;
            }
            catch
            {
                return 0;
            }

            if (!isLocal && playerUid == 0)
            {
                try
                {
                    if (string.IsNullOrEmpty(archive.PlayerId))
                    {
                        return 0;
                    }
                }
                catch
                {
                    return 0;
                }
            }

            return ResolveSteamIdFromUid(playerUid, isLocal);
        }

        private static bool IsArchiveIdentityReady(SpeechEventArchive archive)
        {
            long playerUid = 0;
            bool isLocal = false;
            try
            {
                playerUid = archive.PlayerUID;
                isLocal = archive.IsLocal;
            }
            catch
            {
                return false;
            }

            if (!isLocal && playerUid == 0)
            {
                try
                {
                    return !string.IsNullOrEmpty(archive.PlayerId);
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }

        private static ulong ResolveSteamIdFromActor(ProtoActor actor)
        {
            if (actor.steamID != 0)
            {
                return actor.steamID;
            }

            if (actor.UID != 0)
            {
                ulong fromUid = ResolveSteamIdFromUid(actor.UID, actor.IsHost);
                if (fromUid != 0)
                {
                    return fromUid;
                }
            }

            return actor.ActorID != 0 && _actorToSteam.TryGetValue(actor.ActorID, out ulong mapped) ? mapped : 0;
        }

        private static void RegisterActorMapping(ProtoActor actor, ulong steamId)
        {
            if (actor.ActorID != 0)
            {
                _actorToSteam[actor.ActorID] = steamId;
            }
        }

        private static ulong ResolveSteamIdFromUid(long playerUid, bool isLocal)
        {
            try
            {
                object? pdata = typeof(Hub).GetField("pdata", InstanceMemberFlags)?.GetValue(Hub.s);
                FieldInfo? field = pdata?.GetType().GetField("actorUIDToSteamID", InstanceMemberFlags);
                if (field?.GetValue(pdata) is Dictionary<long, ulong> dict
                    && dict.TryGetValue(playerUid, out ulong steamId))
                {
                    return steamId;
                }
            }
            catch { /* ignore */ }

            if (isLocal)
            {
                try
                {
                    PlatformMgr platformMgr = MonoSingleton<PlatformMgr>.Instance;
                    FieldInfo pathField = typeof(PlatformMgr).GetField("_uniqueUserPath", InstanceMemberFlags);
                    string? userPath = pathField?.GetValue(platformMgr) as string;
                    if (!string.IsNullOrEmpty(userPath) && ulong.TryParse(userPath, out ulong localSteam))
                    {
                        return localSteam;
                    }
                }
                catch { /* ignore */ }
            }

            return 0;
        }

        private static string ResolveDisplayName(ulong steamId, string fallback)
        {
            try
            {
                object? pdata = typeof(Hub).GetField("pdata", InstanceMemberFlags)?.GetValue(Hub.s);
                object? main = pdata?.GetType().GetField("main", InstanceMemberFlags)?.GetValue(pdata);
                if (main != null)
                {
                    FieldInfo cacheField = main.GetType().GetField("steamIDToNameCache", InstanceMemberFlags);
                    if (cacheField?.GetValue(main) is Dictionary<ulong, string> cache
                        && cache.TryGetValue(steamId, out string? name)
                        && !string.IsNullOrWhiteSpace(name))
                    {
                        return name;
                    }
                }

                if (pdata?.GetType().GetField("MyNickName", InstanceMemberFlags)?.GetValue(pdata) is string myNick
                    && !string.IsNullOrWhiteSpace(myNick))
                {
                    ulong localSteam = ResolveSteamIdFromUid(0, isLocal: true);
                    if (localSteam == steamId)
                    {
                        return myNick;
                    }
                }
            }
            catch { /* ignore */ }

            return string.IsNullOrWhiteSpace(fallback) ? steamId.ToString() : fallback;
        }

        private static LeaderboardDocument PersistLeaderboardDocument(int slotId)
        {
            return LeaderboardBuilder.Build(slotId, _players.Values);
        }

        private static void PersistLeaderboardImmediate(int slotId)
        {
            LeaderboardDocument leaderboard = PersistLeaderboardDocument(slotId);
            StatisticsWriteQueue.SaveLeaderboardImmediate(slotId, leaderboard);
        }
    }
}
