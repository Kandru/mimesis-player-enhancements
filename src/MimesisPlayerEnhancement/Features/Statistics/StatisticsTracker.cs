using System;
using System.Collections.Generic;
using System.Reflection;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using MimesisPlayerEnhancement.Features.WebDashboard;
using MimesisPlayerEnhancement.Util;
using Mimic.Voice.SpeechSystem;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    public static class StatisticsTracker
    {
        private const string Feature = "Statistics";
        private const BindingFlags InstanceMemberFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Dictionary<ulong, PlayerStatisticsDocument> _players = [];
        private static readonly Dictionary<ulong, DateTime> _connectedSince = [];
        private static readonly Dictionary<ulong, int> _voiceEventBaselines = [];
        private static Dictionary<ulong, int>? _voiceCountCache;

        private static FieldInfo? _steamIdToNameCacheField;
        private static FieldInfo? _myNickNameField;
        private static object? _cachedNameCacheOwner;
        private static Dictionary<ulong, string>? _cachedNameDictionary;

        private const float ConnectedTimeFlushIntervalSeconds = 1f;
        private static float _nextConnectedTimeFlushTime;

        private static int _loadedSlotId = -999;

        internal static void ClearRuntimeState()
        {
            _players.Clear();
            _connectedSince.Clear();
            _voiceEventBaselines.Clear();
            _voiceCountCache = null;
            _loadedSlotId = -999;
            _nextConnectedTimeFlushTime = 0f;
            StatisticsWriteQueue.Clear();
            StatisticsMessages.ClearRuntimeState();
        }

        public static void LoadForSlot(int slotId)
        {
            if (!ModConfig.EnableStatistics.Value || !MimesisSaveManager.IsValidSaveSlotId(slotId))
            {
                return;
            }

            if (slotId == _loadedSlotId)
            {
                StatisticsWriteQueue.Configure(slotId, () => _players);
                return;
            }

            try
            {
                _loadedSlotId = slotId;
                _players.Clear();
                _connectedSince.Clear();
                _voiceEventBaselines.Clear();
                StatisticsStore.LoadAllPlayersForSlot(slotId, _players);
                StatisticsWriteQueue.Configure(slotId, () => _players);
                ModLog.Info(Feature, $"Loaded statistics for save slot {slotId} ({_players.Count} players).");
            }
            catch (Exception ex)
            {
                _loadedSlotId = -999;
                ModLog.Warn(Feature, $"LoadForSlot({slotId}) failed — {ex.Message}");
            }
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
            EnsureVoiceBaseline(steamId);
            PersistSlot(slotId);

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
            _ = _voiceEventBaselines.Remove(steamId);
            if (doc.CurrentSession != null)
            {
                doc.CurrentSession.LastDisconnectedAtUtc = DateTime.UtcNow;
                doc.CurrentSession.IsOpen = true;
            }

            ModLog.Info(Feature, $"Player disconnected — steamId={steamId} displayName={doc.DisplayName}");
            PersistSlot(_loadedSlotId);
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

            List<KeyValuePair<ulong, PlayerStatisticsDocument>> playersSnapshot = [.. _players];
            foreach (KeyValuePair<ulong, PlayerStatisticsDocument> kvp in playersSnapshot)
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
                StatisticsWriteQueue.MarkDirty(kvp.Key);
                changed = true;
            }

            if (UnityEngine.Time.time >= _nextConnectedTimeFlushTime)
            {
                _nextConnectedTimeFlushTime = UnityEngine.Time.time + ConnectedTimeFlushIntervalSeconds;
                List<ulong> connectedSteamIds = [.. _connectedSince.Keys];
                foreach (ulong steamId in connectedSteamIds)
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
            }

            if (changed)
            {
                StatisticsWriteQueue.FlushPendingWrites();
            }
        }

        public static void OnDungeonReportFlushed(
            PlayReportManager manager,
            IReadOnlyDictionary<ulong, PlayReportData> dungeonReports)
        {
            if (!CanTrack())
            {
                return;
            }

            int slotId = _loadedSlotId;
            HashSet<ulong> affected = [];

            foreach (KeyValuePair<ulong, PlayReportData> kvp in dungeonReports)
            {
                ulong steamId = kvp.Key;
                if (steamId == 0)
                {
                    continue;
                }

                _ = affected.Add(steamId);
                ApplyDungeonReportTotals(steamId, kvp.Value);
            }

            List<ulong> connectedSteamIds = [.. _connectedSince.Keys];
            foreach (ulong steamId in connectedSteamIds)
            {
                _ = affected.Add(steamId);
            }

            Dictionary<ulong, int> voiceCounts = GetVoiceCountCache();

            foreach (ulong steamId in affected)
            {
                PlayerStatisticsDocument doc = GetOrCreatePlayer(steamId);
                doc.DisplayName = ResolveDisplayName(steamId, doc.DisplayName);
                ApplyVoiceDelta(steamId, doc, voiceCounts);
                FlushConnectedTime(steamId, doc);
            }

            UpdateVoiceBaselines(affected, voiceCounts);
            InvalidateVoiceCountCache();
            PersistSlot(slotId);

            int cycleNumber = manager.AccumulatedCycleCount;
            if (ModConfig.ShowStatisticsToasts.Value)
            {
                StatisticsMessages.OnDungeonCompleted(cycleNumber);
            }

            ModLog.Info(Feature, $"Dungeon report flushed for slot {slotId} ({affected.Count} players, cycle baseline {cycleNumber}).");
            WebDashboardSnapshotCache.MarkDirty();
        }

        public static void OnCurrencyEarned(long amount)
        {
            if (!CanTrack() || amount <= 0)
            {
                return;
            }

            ulong hostSteam = GameSessionAccess.GetLocalSteamId();
            if (hostSteam == 0)
            {
                return;
            }

            PlayerStatisticsDocument doc = GetOrCreatePlayer(hostSteam);
            doc.CurrentSession ??= NewSession(DateTime.UtcNow);
            doc.CurrentSession.Counters.CurrencyEarned += amount;
            doc.Global.Counters.CurrencyEarned += amount;
            StatisticsWriteQueue.MarkDirty(hostSteam);
            WebDashboardSnapshotCache.MarkDirty();
        }

        public static void OnSurvivalPlayerDeath(ulong steamId)
        {
            if (!CanTrack() || steamId == 0)
            {
                return;
            }

            IncrementCounter(steamId, counters => counters.SurvivalDeaths++);
        }

        public static void OnPlayerDying(VPlayer player, ActorDyingSig sig, IVroom room)
        {
            if (!CanTrack() || player == null || player.SteamID == 0 || room == null)
            {
                return;
            }

            if (!DeathAttributionHelper.TryResolveTrapDeath(player, sig, room, out TrapType trapType))
            {
                return;
            }

            string trapKey = DeathAttributionHelper.FormatTrapType(trapType);
            IncrementDictionaryCounter(player.SteamID, counters => counters.DeathsByTrapType, trapKey);
        }

        public static void OnMonsterKilled(ulong steamId, int monsterMasterId)
        {
            if (!CanTrack() || steamId == 0 || monsterMasterId <= 0)
            {
                return;
            }

            string monsterKey = DeathAttributionHelper.FormatMonsterMasterId(monsterMasterId);
            IncrementDictionaryCounter(steamId, counters => counters.MonsterKillsByMasterId, monsterKey);
        }

        public static void HandleActorDeath(IVroom room, GameActorDeadEventArgs args)
        {
            if (!CanTrack() || room == null || args?.Victim == null)
            {
                return;
            }

            if (args.Victim is VPlayer player)
            {
                if (room is DeathMatchRoom)
                {
                    OnDeathmatchPlayerDeath(player.SteamID);
                }
                else
                {
                    OnSurvivalPlayerDeath(player.SteamID);
                }

                return;
            }

            if (args.Victim is not VMonster monster || args.AttackerActorID == 0)
            {
                return;
            }

            VActor? attacker;
            try
            {
                attacker = room.FindActorByObjectID(args.AttackerActorID);
            }
            catch
            {
                return;
            }

            if (attacker is VPlayer killer)
            {
                OnMonsterKilled(killer.SteamID, monster.MasterID);
            }
        }

        public static void OnSurvivalDungeonEnded(IEnumerable<VPlayer> players)
        {
            if (!CanTrack())
            {
                return;
            }

            foreach (VPlayer player in players)
            {
                if (player == null || player.SteamID == 0)
                {
                    continue;
                }

                PlayerResultStatus status = ResolveSurvivalResultStatus(player);
                switch (status)
                {
                    case PlayerResultStatus.Alived:
                        IncrementCounter(player.SteamID, counters => counters.SurvivalWins++);
                        break;
                    case PlayerResultStatus.Wasted:
                        IncrementCounter(player.SteamID, counters => counters.SurvivalLeftBehind++);
                        break;
                }
            }

            StatisticsWriteQueue.FlushPendingWrites();
            WebDashboardSnapshotCache.MarkDirty();
        }

        public static void OnDeathmatchPlayerDeath(ulong steamId)
        {
            if (!CanTrack() || steamId == 0)
            {
                return;
            }

            IncrementCounter(steamId, counters => counters.DeathmatchDeaths++);
        }

        public static void OnDeathmatchSurvivor(ulong steamId)
        {
            if (!CanTrack() || steamId == 0)
            {
                return;
            }

            IncrementCounter(steamId, counters => counters.DeathmatchWins++);
            StatisticsWriteQueue.FlushPendingWrites();
            WebDashboardSnapshotCache.MarkDirty();
        }

        public static void OnPlayerRevived(ulong steamId)
        {
            if (!CanTrack() || steamId == 0)
            {
                return;
            }

            IncrementCounter(steamId, counters => counters.Revives++);
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
            PersistSlot(slotId, waitForCompletion: false);
            ModLog.Debug(Feature, $"Statistics queued on game save for slot {slotId}.");
            WebDashboardSnapshotCache.MarkDirty();
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

        private static void IncrementCounter(ulong steamId, Action<StatCounters> increment)
        {
            PlayerStatisticsDocument doc = GetOrCreatePlayer(steamId);
            doc.CurrentSession ??= NewSession(DateTime.UtcNow);
            doc.CurrentSession.Counters ??= new StatCounters();
            doc.Global ??= new GlobalStats();
            doc.Global.Counters ??= new StatCounters();
            EnsureCounterDictionaries(doc.CurrentSession.Counters);
            EnsureCounterDictionaries(doc.Global.Counters);
            increment(doc.CurrentSession.Counters);
            increment(doc.Global.Counters);
            StatisticsWriteQueue.MarkDirty(steamId);
        }

        private static void IncrementDictionaryCounter(
            ulong steamId,
            Func<StatCounters, Dictionary<string, long>> selector,
            string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            PlayerStatisticsDocument doc = GetOrCreatePlayer(steamId);
            doc.CurrentSession ??= NewSession(DateTime.UtcNow);
            doc.CurrentSession.Counters ??= new StatCounters();
            doc.Global ??= new GlobalStats();
            doc.Global.Counters ??= new StatCounters();
            EnsureCounterDictionaries(doc.CurrentSession.Counters);
            EnsureCounterDictionaries(doc.Global.Counters);
            IncrementDictionaryValue(selector(doc.CurrentSession.Counters), key);
            IncrementDictionaryValue(selector(doc.Global.Counters), key);
            StatisticsWriteQueue.MarkDirty(steamId);
        }

        private static void IncrementDictionaryValue(Dictionary<string, long> dictionary, string key)
        {
            dictionary ??= [];
            _ = dictionary.TryGetValue(key, out long current);
            dictionary[key] = current + 1;
        }

        private static void EnsureCounterDictionaries(StatCounters? counters)
        {
            if (counters == null)
            {
                return;
            }

            counters.MonsterKillsByMasterId ??= [];
            counters.DeathsByTrapType ??= [];
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
            return [.. _players.Values];
        }

        internal static IReadOnlyCollection<ulong> GetConnectedSteamIds()
        {
            return [.. _connectedSince.Keys];
        }

        internal static bool TryGetLoadedSlotId(out int slotId)
        {
            slotId = _loadedSlotId;
            return slotId >= 0;
        }

        internal static ulong TryResolveSteamId(Mimic.Actors.ProtoActor actor)
        {
            if (actor == null)
            {
                return 0;
            }

            if (actor.steamID != 0)
            {
                return actor.steamID;
            }

            if (actor.UID != 0)
            {
                return GameSessionAccess.ResolveSteamId(actor.UID, actor.IsHost);
            }

            return 0;
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

        private static void ApplyDungeonReportTotals(ulong steamId, PlayReportData report)
        {
            PlayerStatisticsDocument doc = GetOrCreatePlayer(steamId);
            doc.CurrentSession ??= NewSession(DateTime.UtcNow);

            StatCounters totals = new()
            {
                ItemCarryCount = report.TotalItemCarryCount,
                DamageToAlly = report.TotalDamageToAlly,
                MimicEncounterCount = report.TotalMimicEncounterCount,
                TimeInStartingVolumeMs = report.TotalTimeInStartingVolume,
                CyclesCompleted = 1,
            };

            MergeDelta(doc, totals);
        }

        private static void ApplyVoiceDelta(ulong steamId, PlayerStatisticsDocument doc, Dictionary<ulong, int> voiceCounts)
        {
            int current = voiceCounts.TryGetValue(steamId, out int count) ? count : 0;
            int baseline = _voiceEventBaselines.TryGetValue(steamId, out int b) ? b : current;
            int delta = Math.Max(0, current - baseline);
            if (delta == 0)
            {
                return;
            }

            doc.CurrentSession ??= NewSession(DateTime.UtcNow);
            doc.CurrentSession.Counters.VoiceEvents += delta;
            doc.Global.Counters.VoiceEvents += delta;
        }

        private static void UpdateVoiceBaselines(IEnumerable<ulong> steamIds, Dictionary<ulong, int> voiceCounts)
        {
            foreach (ulong steamId in steamIds)
            {
                _voiceEventBaselines[steamId] = voiceCounts.TryGetValue(steamId, out int count) ? count : 0;
            }
        }

        private static void MergeDelta(PlayerStatisticsDocument doc, StatCounters delta)
        {
            doc.CurrentSession ??= NewSession(DateTime.UtcNow);
            doc.CurrentSession.Counters.Add(delta);
            doc.Global.Counters.Add(delta);
        }

        private static void EnsureVoiceBaseline(ulong steamId)
        {
            if (_voiceEventBaselines.ContainsKey(steamId))
            {
                return;
            }

            _voiceEventBaselines[steamId] = CountVoiceEventsForSteamId(steamId);
        }

        private static Dictionary<ulong, int> GetVoiceCountCache()
        {
            _voiceCountCache ??= BuildVoiceCountCache();
            return _voiceCountCache;
        }

        private static void InvalidateVoiceCountCache()
        {
            _voiceCountCache = null;
        }

        internal static void SyncVoiceBaseline(SpeechEventArchive archive)
        {
            if (!ModConfig.EnableStatistics.Value || archive == null)
            {
                return;
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

            _voiceEventBaselines[steamId] = CountVoiceEventsForSteamId(steamId);
        }

        private static PlayReportData? TryGetPlayReport(ulong steamId)
        {
            Dictionary<ulong, PlayReportData>? dict = GameSessionAccess.TryGetPlayReportManager()?.CurrentReportDict;
            return dict == null ? null : dict.TryGetValue(steamId, out PlayReportData? report) ? report : null;
        }

        private static PlayerResultStatus ResolveSurvivalResultStatus(VPlayer player)
        {
            if (player.ReasonOfDeath != ReasonOfDeath.None)
            {
                return PlayerResultStatus.Dead;
            }

            if (player.Wasted)
            {
                return PlayerResultStatus.Wasted;
            }

            return PlayerResultStatus.Alived;
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
                    catch
                    {
                        /* not ready */
                    }

                    ulong archiveSteam = GameSessionAccess.ResolveSteamId(playerUid, isLocal);
                    if (archiveSteam == 0)
                    {
                        continue;
                    }

                    _ = cache.TryGetValue(archiveSteam, out int current);
                    cache[archiveSteam] = current + VoiceEventStats.GetVoiceLineCount(archive);
                }
            }
            catch
            {
                /* ignore */
            }

            return cache;
        }

        private static int CountVoiceEventsForSteamId(ulong steamId)
        {
            Dictionary<ulong, int> cache = GetVoiceCountCache();
            return cache.TryGetValue(steamId, out int count) ? count : 0;
        }

        private static ulong ResolveSteamIdFromArchive(SpeechEventArchive archive)
        {
            long playerUid;
            bool isLocal;
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

            return GameSessionAccess.ResolveSteamId(playerUid, isLocal);
        }

        private static bool IsArchiveIdentityReady(SpeechEventArchive archive)
        {
            long playerUid;
            bool isLocal;
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

        private static string ResolveDisplayName(ulong steamId, string fallback)
        {
            try
            {
                Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
                object? main = pdata?.GetType().GetField("main", InstanceMemberFlags)?.GetValue(pdata);
                if (main != null)
                {
                    _steamIdToNameCacheField ??= main.GetType().GetField("steamIDToNameCache", InstanceMemberFlags);
                    if (_steamIdToNameCacheField != null)
                    {
                        if (!ReferenceEquals(_cachedNameCacheOwner, main))
                        {
                            _cachedNameCacheOwner = main;
                            _cachedNameDictionary = _steamIdToNameCacheField.GetValue(main) as Dictionary<ulong, string>;
                        }

                        if (_cachedNameDictionary != null
                            && _cachedNameDictionary.TryGetValue(steamId, out string? name)
                            && !string.IsNullOrWhiteSpace(name))
                        {
                            return name;
                        }
                    }
                }

                _myNickNameField ??= pdata?.GetType().GetField("MyNickName", InstanceMemberFlags);
                if (_myNickNameField?.GetValue(pdata) is string myNick
                    && !string.IsNullOrWhiteSpace(myNick))
                {
                    ulong localSteam = GameSessionAccess.GetLocalSteamId();
                    if (localSteam == steamId)
                    {
                        return myNick;
                    }
                }
            }
            catch
            {
                /* ignore */
            }

            return string.IsNullOrWhiteSpace(fallback) ? steamId.ToString() : fallback;
        }

        internal static void PersistSlot(int slotId, bool waitForCompletion = false)
        {
            if (!ModConfig.EnableStatistics.Value || !MimesisSaveManager.IsValidSaveSlotId(slotId))
            {
                return;
            }

            if (_loadedSlotId != slotId)
            {
                LoadForSlot(slotId);
            }

            StatisticsWriteQueue.Configure(slotId, () => _players);
            StatisticsWriteQueue.PersistImmediate(waitForCompletion);
        }

        internal static void PersistLoadedSlot(bool waitForCompletion = false)
        {
            if (TryGetLoadedSlotId(out int slotId))
            {
                PersistSlot(slotId, waitForCompletion);
            }
        }

        private static bool HasOpenDisconnectedSessions()
        {
            List<PlayerStatisticsDocument> playerDocs = [.. _players.Values];
            foreach (PlayerStatisticsDocument doc in playerDocs)
            {
                SessionStats? session = doc.CurrentSession;
                if (session != null && session.IsOpen && session.LastDisconnectedAtUtc.HasValue)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
