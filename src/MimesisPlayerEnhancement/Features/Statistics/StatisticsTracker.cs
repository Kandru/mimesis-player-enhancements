using MimesisPlayerEnhancement.Features.Statistics.Models;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    public static class StatisticsTracker
    {
        private const string Feature = "Statistics";

        private const float ConnectedTimeFlushIntervalSeconds = 1f;
        private static float _nextConnectedTimeFlushTime;

        private static bool _wasEnabled;

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
            StatisticsVoiceCounter.Clear();
            _nextConnectedTimeFlushTime = 0f;
            StatisticsMessages.ClearRuntimeState();
            StatisticsRunTracker.ClearRuntimeState();
            StatisticsDeathHandler.ClearRuntimeState();
            TrainDepositTracker.ClearDungeonState();
        }

        internal static void HandleArchiveStarted(SpeechEventArchive archive, int slotId)
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

            if (PlayerRegistry.IsConnected(steamId))
            {
                return;
            }

            PlayerRegistry.LoadForSlot(slotId);

            PlayerStatisticsDocument doc = PlayerRegistry.GetOrCreate(steamId).Statistics;
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

            PlayerRegistry.SetConnectedSince(steamId, now);
            StatisticsVoiceCounter.EnsureBaseline(steamId);
            PlayerRegistry.BumpRevision();

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

            if (!PlayerRegistry.IsConnected(steamId))
            {
                return;
            }

            if (!PlayerRegistry.TryGetStatistics(steamId, out PlayerStatisticsDocument? doc))
            {
                return;
            }

            PlayerLifecycleContribution? disconnectContribution = BuildSessionDisconnectContribution(steamId, doc);

            doc.DisplayName = StatisticsDisplayNameResolver.Resolve(steamId, doc.DisplayName);
            if (PlayerRegistry.TryGetLoadedSlotId(out int slotId) && SaveSlotDocumentStore.IsUsableName(doc.DisplayName, steamId))
            {
                SaveSlotDocumentStore.UpsertPlayer(steamId, doc.DisplayName);
            }

            FlushConnectedTime(steamId, doc);
            PlayerRegistry.MarkDisconnected(steamId);
            StatisticsVoiceCounter.RemoveBaseline(steamId);
            if (doc.CurrentSession != null)
            {
                doc.CurrentSession.LastDisconnectedAtUtc = DateTime.UtcNow;
                doc.CurrentSession.IsOpen = true;
            }

            PlayerRegistry.BumpRevision();
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

            if (PlayerRegistry.GetConnectedSteamIds().Count == 0 && !HasOpenDisconnectedSessions())
            {
                return;
            }

            int graceMinutes = ModConfig.SessionReconnectGraceMinutes.Value;
            DateTime now = DateTime.UtcNow;
            bool changed = false;

            foreach (PlayerStatisticsDocument doc in PlayerRegistry.GetAllStatistics())
            {
                SessionStats? session = doc.CurrentSession;
                if (session == null || !session.IsOpen || !session.LastDisconnectedAtUtc.HasValue)
                {
                    continue;
                }

                if (now - session.LastDisconnectedAtUtc.Value <= TimeSpan.FromMinutes(graceMinutes))
                {
                    continue;
                }

                ModLog.Info(Feature, $"Session finalized — steamId={doc.SteamId} session={session.SessionId} after grace period");
                FinalizeOpenSession(doc, countAsCompleted: true);
                changed = true;
            }

            if (UnityEngine.Time.time >= _nextConnectedTimeFlushTime)
            {
                _nextConnectedTimeFlushTime = UnityEngine.Time.time + ConnectedTimeFlushIntervalSeconds;
                foreach (ulong steamId in PlayerRegistry.GetConnectedSteamIds())
                {
                    if (!PlayerRegistry.TryGetStatistics(steamId, out PlayerStatisticsDocument? doc))
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
                PlayerRegistry.BumpRevision();
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

            int slotId = PlayerRegistry.LoadedSlotId;
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

            foreach (ulong steamId in PlayerRegistry.GetConnectedSteamIds())
            {
                _ = affected.Add(steamId);
            }

            Dictionary<ulong, int> voiceCounts = StatisticsVoiceCounter.GetVoiceCountCache();

            foreach (ulong steamId in affected)
            {
                PlayerStatisticsDocument doc = PlayerRegistry.GetOrCreate(steamId).Statistics;
                doc.DisplayName = StatisticsDisplayNameResolver.Resolve(steamId, doc.DisplayName);
                ApplyVoiceDelta(steamId, doc, voiceCounts);
                FlushConnectedTime(steamId, doc);
            }

            StatisticsVoiceCounter.UpdateBaselines(affected, voiceCounts);
            StatisticsVoiceCounter.InvalidateVoiceCountCache();
            PlayerRegistry.BumpRevision();

            int cycleNumber = manager.AccumulatedCycleCount;
            if (ModConfig.ShowStatisticsToasts.Value)
            {
                StatisticsMessages.OnDungeonCompleted(cycleNumber);
            }

            ModLog.Info(Feature, $"Dungeon report flushed for slot {slotId} ({affected.Count} players, cycle baseline {cycleNumber}).");
            WebDashboardSnapshotCache.MarkDirty();
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

                switch (ResolveSurvivalResultStatus(player))
                {
                    case PlayerResultStatus.Alived:
                        StatisticsCounterWriter.Modify(
                            player.SteamID,
                            counters => counters.SurvivalWins++,
                            notify: false);
                        break;
                    case PlayerResultStatus.Wasted:
                        StatisticsCounterWriter.Modify(
                            player.SteamID,
                            counters => counters.SurvivalLeftBehind++,
                            notify: false);
                        break;
                }
            }

            StatisticsDeathHandler.OnDungeonEnded(players, notify: false);
            StatisticsCounterWriter.NotifyChanged();
        }

        public static void OnDeathmatchSurvivor(ulong steamId)
        {
            if (!CanTrack() || steamId == 0)
            {
                return;
            }

            StatisticsCounterWriter.Modify(steamId, counters => counters.DeathmatchWins++);
        }

        public static void OnPlayerRevived(ulong steamId)
        {
            if (!CanTrack() || steamId == 0)
            {
                return;
            }

            StatisticsCounterWriter.Modify(steamId, counters => counters.Revives++);
            StatisticsDeathHandler.OnPlayerRevived(steamId);
        }

        public static void OnDungeonStarted()
        {
            if (!CanTrack())
            {
                return;
            }

            StatisticsDeathHandler.OnDungeonStarted();
        }

        public static void OnGameSaved(int slotId, bool waitForCompletion = false)
        {
            if (!MimesisSaveManager.IsHost())
            {
                return;
            }

            if (!MimesisSaveManager.IsValidSaveSlotId(slotId) || PlayerRegistry.LoadedSlotId != slotId)
            {
                return;
            }

            PersistLoadedSlot(waitForCompletion);
            ModLog.Debug(Feature, $"Statistics queued on game save for slot {slotId}.");
        }

        internal static void OnSessionEnded()
        {
            if (MimesisSaveManager.IsHost() && PlayerRegistry.TryGetLoadedSlotId(out int slotId))
            {
                try
                {
                    foreach (ulong steamId in PlayerRegistry.GetConnectedSteamIds())
                    {
                        if (PlayerRegistry.TryGetStatistics(steamId, out PlayerStatisticsDocument? doc))
                        {
                            FlushConnectedTime(steamId, doc);
                        }
                    }

                    foreach (PlayerStatisticsDocument doc in PlayerRegistry.GetAllStatistics())
                    {
                        if (doc.CurrentSession?.IsOpen == true)
                        {
                            FinalizeOpenSession(doc, countAsCompleted: true);
                        }
                    }

                    PlayerRegistry.BumpRevision();
                    ModLog.Debug(Feature, $"Statistics finalized in memory for save slot {slotId} on session end.");
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"OnSessionEnded finalize failed — {ex.Message}");
                }

                PlayerRegistry.PersistStatistics(waitForCompletion: false);
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

        private static void MergeDelta(PlayerStatisticsDocument doc, StatCounters delta) =>
            StatisticsCounterWriter.MergeDelta(doc.SteamId, delta);

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
                ? "session started"
                : reconnectCount > 0
                    ? $"session resumed (reconnects={reconnectCount})"
                    : "session resumed";
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
            if (PlayerRegistry.TryGetConnectedSince(steamId, out DateTime since))
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

        internal static bool CanTrack() =>
            ModConfig.EnableStatistics.Value
            && PlayerRegistry.TryGetLoadedSlotId(out int slotId)
            && MimesisSaveManager.IsValidSaveSlotId(slotId)
            && MimesisSaveManager.IsHost();

        internal static SessionStats CreateSession(DateTime now) => NewSession(now);

        internal static PlayerStatisticsDocument? TryGetPlayerDocument(ulong steamId)
        {
            return PlayerRegistry.TryGetStatistics(steamId, out PlayerStatisticsDocument? doc) ? doc : null;
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

            PlayerRegistry.MarkDisconnected(steamId);

            if (!PlayerRegistry.RemoveIfNeverConnected(steamId))
            {
                return;
            }

            StatisticsVoiceCounter.RemoveBaseline(steamId);
            StatisticsMessages.ClearPlayerRuntimeState(steamId);
            PlayerRegistry.BumpRevision();
            WebDashboardSnapshotCache.MarkDirty();
            ModLog.Debug(Feature, $"Abandoned incomplete connection statistics — steamId={steamId}");
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

            if (PlayerRegistry.TryGetStatistics(steamId, out PlayerStatisticsDocument? doc) && doc.CurrentSession?.Counters != null)
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
            if (!PlayerRegistry.TryGetConnectedSince(steamId, out DateTime since))
            {
                return;
            }

            long seconds = (long)Math.Max(0, (DateTime.UtcNow - since).TotalSeconds);
            if (seconds <= 0)
            {
                return;
            }

            doc.CurrentSession ??= NewSession(since);
            StatisticsCounterWriter.AddConnectedSeconds(steamId, seconds);
            PlayerRegistry.SetConnectedSince(steamId, DateTime.UtcNow);
        }

        private static void ApplyDungeonReportTotals(ulong steamId, PlayReportData report)
        {
            StatCounters totals = new()
            {
                ItemCarryCount = report.TotalItemCarryCount,
                DamageToFriend = report.TotalDamageToAlly,
                MimicEncounterCount = report.TotalMimicEncounterCount,
                TimeInStartingVolumeMs = report.TotalTimeInStartingVolume,
                CyclesCompleted = 1,
            };

            StatisticsCounterWriter.MergeDelta(steamId, totals);
        }

        private static void ApplyVoiceDelta(ulong steamId, PlayerStatisticsDocument doc, Dictionary<ulong, int> voiceCounts)
        {
            int delta = StatisticsVoiceCounter.GetDeltaSinceBaseline(steamId, voiceCounts);
            if (delta == 0)
            {
                return;
            }

            StatisticsCounterWriter.Modify(steamId, counters => counters.VoiceEvents += delta);
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

            if (PlayerRegistry.LoadedSlotId != slotId)
            {
                return;
            }

            StatisticsWriteQueue.Configure(slotId, PlayerRegistry.GetStatisticsDictionary);
            PlayerRegistry.PersistStatistics(waitForCompletion);
        }

        internal static void PersistLoadedSlot(bool waitForCompletion = false)
        {
            if (PlayerRegistry.TryGetLoadedSlotId(out int slotId))
            {
                PersistSlot(slotId, waitForCompletion);
            }
        }

        private static bool HasOpenDisconnectedSessions()
        {
            foreach (PlayerStatisticsDocument doc in PlayerRegistry.GetAllStatistics())
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
