using System.Threading;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    public static class StatisticsTracker
    {
        private const string Feature = "Statistics";

        private static readonly Dictionary<ulong, PlayerStatisticsDocument> _players = [];
        private static readonly Dictionary<ulong, DateTime> _connectedSince = [];

        private const float ConnectedTimeFlushIntervalSeconds = 1f;
        private static float _nextConnectedTimeFlushTime;

        private static int _loadedSlotId = -999;
        private static bool _wasEnabled;
        private static int _revision;

        internal static int Revision => Volatile.Read(ref _revision);

        internal static void BumpRevision()
        {
            _ = Interlocked.Increment(ref _revision);
        }

        /// <summary>
        /// Config-sync hook — persists and clears runtime state when the feature is toggled off live.
        /// </summary>
        internal static void RefreshFromConfig()
        {
            bool enabled = ModConfig.EnableStatistics.Value;
            if (_wasEnabled && !enabled)
            {
                OnSessionEnded();
            }

            _wasEnabled = enabled;
        }

        internal static void ClearRuntimeState()
        {
            _players.Clear();
            _connectedSince.Clear();
            StatisticsVoiceCounter.Clear();
            _loadedSlotId = -999;
            _nextConnectedTimeFlushTime = 0f;
            _revision = 0;
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
                StatisticsVoiceCounter.Clear();
                StatisticsStore.LoadAllPlayersForSlot(slotId, _players);
                StatisticsWriteQueue.Configure(slotId, () => _players);
                BumpRevision();
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
                return;
            }

            if (!StatisticsArchiveIdentity.IsArchiveIdentityReady(archive))
            {
                return;
            }

            ulong steamId = StatisticsArchiveIdentity.ResolveSteamIdFromArchive(archive);
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
            doc.DisplayName = StatisticsDisplayNameResolver.Resolve(steamId, doc.DisplayName);
            if (SaveSlotDocumentStore.IsUsableName(doc.DisplayName, steamId))
            {
                SaveSlotDocumentStore.UpsertPlayer(steamId, doc.DisplayName);
            }
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
            }
            else
            {
                FinalizeOpenSession(doc, countAsCompleted: true);
                doc.CurrentSession = NewSession(now);
            }

            _connectedSince[steamId] = now;
            StatisticsVoiceCounter.EnsureBaseline(steamId);
            BumpRevision();

            bool isNewSession = !resumeSession;
            int reconnectCount = doc.CurrentSession?.ReconnectCount ?? 0;
            StatisticsMessages.OnPlayerJoinedSession(steamId, doc.DisplayName, doc, isNewSession, reconnectCount);
            WebDashboardSnapshotCache.MarkDirty();

            PlayerLifecycleCoordinator.NotifyStatisticsConnect(steamId, BuildSessionConnectContribution(doc, isNewSession, reconnectCount));
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

            if (!_connectedSince.ContainsKey(steamId))
            {
                return;
            }

            if (!_players.TryGetValue(steamId, out PlayerStatisticsDocument? doc))
            {
                return;
            }

            PlayerLifecycleContribution? disconnectContribution = BuildSessionDisconnectContribution(steamId, doc);

            doc.DisplayName = StatisticsDisplayNameResolver.Resolve(steamId, doc.DisplayName);
            if (_loadedSlotId >= 0 && SaveSlotDocumentStore.IsUsableName(doc.DisplayName, steamId))
            {
                SaveSlotDocumentStore.UpsertPlayer(steamId, doc.DisplayName);
            }

            FlushConnectedTime(steamId, doc);
            _ = _connectedSince.Remove(steamId);
            StatisticsVoiceCounter.RemoveBaseline(steamId);
            if (doc.CurrentSession != null)
            {
                doc.CurrentSession.LastDisconnectedAtUtc = DateTime.UtcNow;
                doc.CurrentSession.IsOpen = true;
            }

            BumpRevision();
            StatisticsMessages.OnPlayerLeftSession(steamId, doc.DisplayName, doc);
            WebDashboardSnapshotCache.MarkDirty();

            PlayerLifecycleCoordinator.NotifyStatisticsDisconnect(steamId, disconnectContribution);
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
                BumpRevision();
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

            Dictionary<ulong, int> voiceCounts = StatisticsVoiceCounter.GetVoiceCountCache();

            foreach (ulong steamId in affected)
            {
                PlayerStatisticsDocument doc = GetOrCreatePlayer(steamId);
                doc.DisplayName = StatisticsDisplayNameResolver.Resolve(steamId, doc.DisplayName);
                ApplyVoiceDelta(steamId, doc, voiceCounts);
                FlushConnectedTime(steamId, doc);
            }

            StatisticsVoiceCounter.UpdateBaselines(affected, voiceCounts);
            StatisticsVoiceCounter.InvalidateVoiceCountCache();
            BumpRevision();

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
            BumpRevision();
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

            if (sig.attackerActorID != 0)
            {
                VActor? attacker;
                try
                {
                    attacker = room.FindActorByObjectID(sig.attackerActorID);
                }
                catch
                {
                    attacker = null;
                }

                if (attacker is VMonster)
                {
                    return;
                }
            }

            if (!DeathAttributionHelper.TryResolveTrapDeath(player, sig, room, out TrapType trapType))
            {
                return;
            }

            string trapKey = StatisticsEntityNames.FormatTrapName(trapType);
            IncrementDictionaryCounter(player.SteamID, counters => counters.DeathsByTrap, trapKey);
        }

        public static void OnMonsterKilled(ulong steamId, int monsterMasterId)
        {
            if (!CanTrack() || steamId == 0 || monsterMasterId <= 0)
            {
                return;
            }

            string monsterKey = StatisticsEntityNames.FormatMonsterName(monsterMasterId);
            IncrementDictionaryCounter(steamId, counters => counters.MonsterKills, monsterKey);
        }

        public static void OnDeathByMonster(ulong steamId, int monsterMasterId)
        {
            if (!CanTrack() || steamId == 0 || monsterMasterId <= 0)
            {
                return;
            }

            string monsterKey = StatisticsEntityNames.FormatMonsterName(monsterMasterId);
            IncrementDictionaryCounter(steamId, counters => counters.DeathsByMonster, monsterKey);
        }

        public static void OnFriendKilled(ulong steamId)
        {
            if (!CanTrack() || steamId == 0)
            {
                return;
            }

            IncrementCounter(steamId, counters => counters.FriendsKilled++);
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

                if (args.AttackerActorID != 0)
                {
                    VActor? playerAttacker;
                    try
                    {
                        playerAttacker = room.FindActorByObjectID(args.AttackerActorID);
                    }
                    catch
                    {
                        playerAttacker = null;
                    }

                    if (playerAttacker is VMonster killingMonster)
                    {
                        OnDeathByMonster(player.SteamID, killingMonster.MasterID);
                    }
                    else if (playerAttacker is VPlayer friendKiller
                             && friendKiller.SteamID != player.SteamID
                             && room is not DeathMatchRoom)
                    {
                        OnFriendKilled(friendKiller.SteamID);
                    }
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

            BumpRevision();
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

            if (!MimesisSaveManager.IsValidSaveSlotId(slotId) || _loadedSlotId != slotId)
            {
                return;
            }

            PersistLoadedSlot(waitForCompletion: false);
            ModLog.Debug(Feature, $"Statistics queued on game save for slot {slotId}.");
        }

        internal static void OnSessionEnded()
        {
            if (MimesisSaveManager.IsHost() && _loadedSlotId >= 0)
            {
                try
                {
                    List<ulong> connectedSteamIds = [.. _connectedSince.Keys];
                    foreach (ulong steamId in connectedSteamIds)
                    {
                        if (_players.TryGetValue(steamId, out PlayerStatisticsDocument? doc))
                        {
                            FlushConnectedTime(steamId, doc);
                        }
                    }

                    foreach (PlayerStatisticsDocument doc in _players.Values)
                    {
                        if (doc.CurrentSession?.IsOpen == true)
                        {
                            FinalizeOpenSession(doc, countAsCompleted: true);
                        }
                    }

                    BumpRevision();
                    ModLog.Debug(Feature, $"Statistics finalized in memory for save slot {_loadedSlotId} on session end.");
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"OnSessionEnded finalize failed — {ex.Message}");
                }
            }

            ClearRuntimeState();
        }

        public static void OnUpdate()
        {
            if (!CanTrack())
            {
                return;
            }

            ProcessDeferred();
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
            BumpRevision();
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
            BumpRevision();
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

            counters.MonsterKills ??= [];
            counters.DeathsByMonster ??= [];
            counters.DeathsByTrap ??= [];
        }

        private static PlayerLifecycleContribution? BuildSessionConnectContribution(
            PlayerStatisticsDocument doc,
            bool isNewSession,
            int reconnectCount)
        {
            if (doc.CurrentSession == null)
            {
                return null;
            }

            string detail = isNewSession
                ? $"new session {doc.CurrentSession.SessionId}"
                : $"resumed session {doc.CurrentSession.SessionId} (reconnects={reconnectCount})";
            return new PlayerLifecycleContribution("Statistics", detail);
        }

        private static PlayerLifecycleContribution? BuildSessionDisconnectContribution(
            ulong steamId,
            PlayerStatisticsDocument doc)
        {
            if (doc.CurrentSession == null)
            {
                return null;
            }

            string detail = $"session {doc.CurrentSession.SessionId} closed";
            if (_connectedSince.TryGetValue(steamId, out DateTime since))
            {
                TimeSpan connected = DateTime.UtcNow - since;
                if (connected.TotalMinutes >= 1)
                {
                    detail += $" (connected {connected.TotalMinutes:F0}m)";
                }
                else if (connected.TotalSeconds >= 1)
                {
                    detail += $" (connected {connected.TotalSeconds:F0}s)";
                }
            }

            return new PlayerLifecycleContribution("Statistics", detail);
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
                doc = NewInMemoryPlayer(steamId);
                _players[steamId] = doc;
            }

            return doc;
        }

        private static PlayerStatisticsDocument NewInMemoryPlayer(ulong steamId)
        {
            return new PlayerStatisticsDocument
            {
                Version = PlayerStatisticsDocument.CurrentVersion,
                SteamId = steamId,
                DisplayName = steamId.ToString(),
                Global = new GlobalStats { Counters = new StatCounters() },
            };
        }

        internal static PlayerStatisticsDocument? TryGetPlayerDocument(ulong steamId)
        {
            return _players.TryGetValue(steamId, out PlayerStatisticsDocument? doc) ? doc : null;
        }

        internal static IReadOnlyList<PlayerStatisticsDocument> GetCachedPlayerDocuments()
        {
            return [.. _players.Values];
        }

        internal static Dictionary<ulong, PlayerStatisticsDocument>.ValueCollection GetCachedPlayerDocumentsView()
        {
            return _players.Values;
        }

        internal static IReadOnlyCollection<ulong> GetConnectedSteamIds()
        {
            return [.. _connectedSince.Keys];
        }

        /// <summary>
        /// Removes a player from in-memory statistics and rewrites the stats sidecar for the loaded slot.
        /// </summary>
        internal static bool RemovePlayer(ulong steamId, bool waitForCompletion = true)
        {
            if (steamId == 0 || !TryGetLoadedSlotId(out int slotId))
            {
                return false;
            }

            if (!_players.Remove(steamId))
            {
                return false;
            }

            _ = _connectedSince.Remove(steamId);
            StatisticsVoiceCounter.RemoveBaseline(steamId);
            StatisticsMessages.ClearPlayerRuntimeState(steamId);
            BumpRevision();
            PersistSlot(slotId, waitForCompletion);
            WebDashboardSnapshotCache.MarkDirty();
            ModLog.Info(Feature, $"Removed player statistics — steamId={steamId}, slot={slotId}");
            return true;
        }

        /// <summary>
        /// Drops an in-memory statistics document that was never fully connected this session.
        /// </summary>
        internal static void RemovePlayerIfNeverConnected(ulong steamId)
        {
            if (steamId == 0 || _connectedSince.ContainsKey(steamId))
            {
                return;
            }

            if (!_players.Remove(steamId))
            {
                return;
            }

            StatisticsVoiceCounter.RemoveBaseline(steamId);
            StatisticsMessages.ClearPlayerRuntimeState(steamId);
            BumpRevision();
            WebDashboardSnapshotCache.MarkDirty();
            ModLog.Debug(Feature, $"Removed incomplete-connect statistics — steamId={steamId}");
        }

        /// <summary>
        /// Clears a prematurely registered connection that never reached fully-ready state.
        /// </summary>
        internal static void AbandonIncompleteConnection(ulong steamId)
        {
            if (steamId == 0)
            {
                return;
            }

            _ = _connectedSince.Remove(steamId);

            if (!_players.Remove(steamId))
            {
                return;
            }

            StatisticsVoiceCounter.RemoveBaseline(steamId);
            StatisticsMessages.ClearPlayerRuntimeState(steamId);
            BumpRevision();
            WebDashboardSnapshotCache.MarkDirty();
            ModLog.Debug(Feature, $"Abandoned incomplete connection statistics — steamId={steamId}");
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
                DamageToFriend = report.TotalDamageToAlly,
                MimicEncounterCount = report.TotalMimicEncounterCount,
                TimeInStartingVolumeMs = report.TotalTimeInStartingVolume,
                CyclesCompleted = 1,
            };

            MergeDelta(doc, totals);
        }

        private static void ApplyVoiceDelta(ulong steamId, PlayerStatisticsDocument doc, Dictionary<ulong, int> voiceCounts)
        {
            int delta = StatisticsVoiceCounter.GetDeltaSinceBaseline(steamId, voiceCounts);
            if (delta == 0)
            {
                return;
            }

            doc.CurrentSession ??= NewSession(DateTime.UtcNow);
            doc.CurrentSession.Counters.VoiceEvents += delta;
            doc.Global.Counters.VoiceEvents += delta;
        }

        private static void MergeDelta(PlayerStatisticsDocument doc, StatCounters delta)
        {
            doc.CurrentSession ??= NewSession(DateTime.UtcNow);
            doc.CurrentSession.Counters.Add(delta);
            doc.Global.Counters.Add(delta);
        }

        internal static void SyncVoiceBaseline(SpeechEventArchive archive)
        {
            if (!ModConfig.EnableStatistics.Value || archive == null)
            {
                return;
            }

            if (!StatisticsArchiveIdentity.IsArchiveIdentityReady(archive))
            {
                return;
            }

            ulong steamId = StatisticsArchiveIdentity.ResolveSteamIdFromArchive(archive);
            if (steamId == 0)
            {
                return;
            }

            StatisticsVoiceCounter.SetBaselineToCurrent(steamId);
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

        internal static void PersistSlot(int slotId, bool waitForCompletion = false)
        {
            if (!ModConfig.EnableStatistics.Value || !MimesisSaveManager.IsValidSaveSlotId(slotId))
            {
                return;
            }

            if (_loadedSlotId != slotId)
            {
                return;
            }

            StatisticsWriteQueue.Configure(slotId, () => _players);
            StatisticsWriteQueue.SaveLoadedSlot(waitForCompletion);
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
