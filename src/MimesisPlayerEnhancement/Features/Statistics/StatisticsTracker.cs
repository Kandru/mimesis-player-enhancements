using System.Linq;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    public static class StatisticsTracker
    {
        private const string Feature = "Statistics";

        private const float ConnectedTimeFlushIntervalSeconds = 1f;
        private const float GracePeriodCheckIntervalSeconds = 5f;
        private static float _nextConnectedTimeFlushTime;
        private static float _nextGracePeriodCheckTime;
        private static bool _hasOpenGraceSessions;

        private static bool _wasEnabled;

        private static readonly Action<ulong> FlushConnectedTimeCallback = FlushConnectedTimeForConnectedPlayer;

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
            _nextGracePeriodCheckTime = 0f;
            _hasOpenGraceSessions = false;
            StatisticsMessages.ClearRuntimeState();
            StatisticsRunTracker.ClearRuntimeState();
            StatisticsDeathHandler.ClearRuntimeState();
            TrainDepositTracker.ClearDungeonState();
            StatisticsDisplayNameResolver.ClearRuntimeState();
            StatisticsRuntime.Clear();
            StatisticsWriteQueue.Clear();
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

            PlayerPresenceEvents.OnPlayerRegistered(steamId, slotId);
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

            PlayerRegistry.LoadForSlot(slotId);

            PlayerGlobalStats global = StatisticsHistory.EnsureGlobal(steamId);
            string displayName = PlayerRegistry.ApplyResolvedDisplayName(steamId, global.DisplayName);
            if (SaveSlotDocumentStore.IsUsableName(displayName, steamId))
            {
                SaveSlotDocumentStore.UpsertPlayer(steamId, displayName);
            }

            DateTime now = DateTime.UtcNow;
            int graceMinutes = ModConfig.SessionReconnectGraceMinutes.Value;
            SessionStats? session = StatisticsRuntime.GetCurrentSession(steamId);

            bool resumeSession = session != null
                                 && session.IsOpen
                                 && session.LastDisconnectedAtUtc.HasValue
                                 && now - session.LastDisconnectedAtUtc.Value <= TimeSpan.FromMinutes(graceMinutes);

            if (resumeSession && session != null)
            {
                session.ReconnectCount++;
                session.LastConnectedAtUtc = now;
                session.LastDisconnectedAtUtc = null;
                StatisticsRuntime.SetCurrentSession(steamId, session);
                _hasOpenGraceSessions = HasOpenDisconnectedSessions();
            }
            else
            {
                FinalizeOpenSession(steamId, countAsCompleted: true);
                StatisticsRuntime.SetCurrentSession(steamId, StatisticsRuntime.CreateSession(now));
            }

            StatisticsVoiceCounter.EnsureBaseline(steamId);
            PlayerRegistry.BumpRevision();

            bool isNewSession = !resumeSession;
            int reconnectCount = StatisticsRuntime.GetCurrentSession(steamId)?.ReconnectCount ?? 0;
            StatisticsMessages.OnPlayerJoinedSession(steamId, displayName, global, isNewSession, reconnectCount);
            WebDashboardSnapshotCache.MarkDirty();

            PlayerLifecycleCoordinator.NotifyStatisticsConnect(steamId, BuildSessionConnectContribution(isNewSession, reconnectCount));
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

            if (!PlayerRegistry.TryGetGlobal(steamId, out PlayerGlobalStats? global))
            {
                return;
            }

            PlayerLifecycleContribution? disconnectContribution = BuildSessionDisconnectContribution(steamId);

            string displayName = PlayerRegistry.ApplyResolvedDisplayName(steamId, global.DisplayName);
            if (PlayerRegistry.TryGetLoadedSlotId(out int slotId) && SaveSlotDocumentStore.IsUsableName(displayName, steamId))
            {
                SaveSlotDocumentStore.UpsertPlayer(steamId, displayName);
            }

            FlushConnectedTime(steamId);
            PlayerRegistry.MarkDisconnected(steamId);
            StatisticsVoiceCounter.RemoveBaseline(steamId);
            SessionStats? session = StatisticsRuntime.GetCurrentSession(steamId);
            if (session != null)
            {
                session.LastDisconnectedAtUtc = DateTime.UtcNow;
                session.IsOpen = true;
                _hasOpenGraceSessions = true;
            }

            PlayerRegistry.BumpRevision();
            StatisticsMessages.OnPlayerLeftSession(steamId, displayName, global);
            WebDashboardSnapshotCache.MarkDirty();

            PlayerLifecycleCoordinator.NotifyStatisticsDisconnect(steamId, disconnectContribution);
        }

        public static void ProcessDeferred()
        {
            if (!CanTrack())
            {
                return;
            }

            bool hasConnected = PlayerRegistry.HasAnyConnected();
            if (!hasConnected && !_hasOpenGraceSessions)
            {
                return;
            }

            bool changed = false;
            if (_hasOpenGraceSessions && UnityEngine.Time.time >= _nextGracePeriodCheckTime)
            {
                _nextGracePeriodCheckTime = UnityEngine.Time.time + GracePeriodCheckIntervalSeconds;
                changed = FinalizeExpiredGraceSessions();
                _hasOpenGraceSessions = HasOpenDisconnectedSessions();
            }

            if (hasConnected && UnityEngine.Time.time >= _nextConnectedTimeFlushTime)
            {
                _nextConnectedTimeFlushTime = UnityEngine.Time.time + ConnectedTimeFlushIntervalSeconds;
                PlayerRegistry.ForEachConnected(FlushConnectedTimeCallback);
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
                PlayerGlobalStats global = StatisticsHistory.EnsureGlobal(steamId);
                PlayerRegistry.ApplyResolvedDisplayName(steamId, global.DisplayName);
                ApplyVoiceDelta(steamId, voiceCounts);
                FlushConnectedTime(steamId);
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

        public static void OnSurvivalDungeonEnded(IEnumerable<VPlayer> players, DungeonState state)
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
            StatisticsHistory.CloseRun(
                state == DungeonState.Success ? DungeonRunOutcome.Success : DungeonRunOutcome.Failed,
                notify: false);
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

        public static void OnDungeonStarted(DungeonProperty? property)
        {
            if (!CanTrack())
            {
                return;
            }

            StatisticsDeathHandler.OnDungeonStarted();

            if (property == null)
            {
                return;
            }

            StatisticsHistory.OpenRun(new DungeonRunIdentity(
                zone: StatisticsHistory.CurrentZone,
                cycle: property.CycleCount,
                seed: property.RandomDungeonSeed,
                dungeonMasterId: property.DungeonMasterID,
                mapId: property.PickedMapID));
            StatisticsCounterWriter.NotifyChanged();
        }

        public static void OnGameSaved(int slotId, bool waitForCompletion = false)
        {
            if (!HostApplyGate.ShouldApplyHostOnlyFeature())
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
            if (HostApplyGate.ShouldApplyHostOnlyFeature() && PlayerRegistry.TryGetLoadedSlotId(out int slotId))
            {
                try
                {
                    foreach (ulong steamId in PlayerRegistry.GetConnectedSteamIds())
                    {
                        FlushConnectedTime(steamId);
                    }

                    foreach (ulong steamId in StatisticsHistory.Document.Globals.Keys.ToList())
                    {
                        FinalizeOpenSession(steamId, countAsCompleted: true);
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

        internal static bool CanTrack() =>
            ModConfig.EnableStatistics.Value
            && PlayerRegistry.TryGetLoadedSlotId(out int slotId)
            && MimesisSaveManager.IsValidSaveSlotId(slotId)
            && HostApplyGate.ShouldApplyHostOnlyFeature();

        internal static bool TryGetSessionCounters(ulong steamId, out StatCounters counters)
        {
            counters = new StatCounters();
            if (steamId == 0)
            {
                return false;
            }

            SessionStats? session = StatisticsRuntime.GetCurrentSession(steamId);
            if (session?.Counters != null)
            {
                counters = session.Counters.Clone();
                return true;
            }

            return false;
        }

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
            StatisticsRuntime.SetCurrentSession(steamId, null);
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

            StatisticsWriteQueue.Configure(slotId);
            PlayerRegistry.PersistStatistics(waitForCompletion);
        }

        internal static void PersistLoadedSlot(bool waitForCompletion = false)
        {
            if (PlayerRegistry.TryGetLoadedSlotId(out int slotId))
            {
                PersistSlot(slotId, waitForCompletion);
            }
        }

        private static PlayerLifecycleContribution? BuildSessionConnectContribution(bool isNewSession, int reconnectCount)
        {
            string detail = isNewSession
                ? "session started"
                : reconnectCount > 0
                    ? $"session resumed (reconnects={reconnectCount})"
                    : "session resumed";
            return new PlayerLifecycleContribution("Statistics", detail);
        }

        private static PlayerLifecycleContribution? BuildSessionDisconnectContribution(ulong steamId)
        {
            SessionStats? session = StatisticsRuntime.GetCurrentSession(steamId);
            if (session == null)
            {
                return null;
            }

            string detail = $"session {session.SessionId} closed";
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

        private static void FinalizeOpenSession(ulong steamId, bool countAsCompleted)
        {
            SessionStats? session = StatisticsRuntime.GetCurrentSession(steamId);
            if (session == null || !session.IsOpen)
            {
                return;
            }

            StatisticsRuntime.FinalizeSession(steamId, session, countAsCompleted);
        }

        private static void FlushConnectedTimeForConnectedPlayer(ulong steamId)
        {
            SessionStats? session = StatisticsRuntime.GetCurrentSession(steamId);
            if (session?.LastDisconnectedAtUtc.HasValue == true)
            {
                return;
            }

            FlushConnectedTime(steamId);
        }

        private static void FlushConnectedTime(ulong steamId)
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

            SessionStats? session = StatisticsRuntime.GetCurrentSession(steamId);
            if (session == null)
            {
                session = StatisticsRuntime.CreateSession(since);
                StatisticsRuntime.SetCurrentSession(steamId, session);
            }

            StatisticsCounterWriter.AddConnectedSeconds(steamId, seconds);
            PlayerRegistry.SetConnectedSince(steamId, DateTime.UtcNow);
        }

        private static void ApplyDungeonReportTotals(ulong steamId, PlayReportData report)
        {
            StatCounters totals = new()
            {
                ItemsCarried = report.TotalItemCarryCount,
                DamageToFriend = report.TotalDamageToAlly,
                MimicEncounters = report.TotalMimicEncounterCount,
            };

            StatisticsCounterWriter.MergeDelta(steamId, totals);
        }

        private static void ApplyVoiceDelta(ulong steamId, Dictionary<ulong, int> voiceCounts)
        {
            int delta = StatisticsVoiceCounter.GetDeltaSinceBaseline(steamId, voiceCounts);
            if (delta == 0)
            {
                return;
            }

            StatisticsCounterWriter.AddVoiceEvents(steamId, delta);
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

        private static bool FinalizeExpiredGraceSessions()
        {
            int graceMinutes = ModConfig.SessionReconnectGraceMinutes.Value;
            DateTime now = DateTime.UtcNow;
            bool changed = false;

            foreach (ulong steamId in StatisticsHistory.Document.Globals.Keys.ToList())
            {
                SessionStats? session = StatisticsRuntime.GetCurrentSession(steamId);
                if (session == null || !session.IsOpen || !session.LastDisconnectedAtUtc.HasValue)
                {
                    continue;
                }

                if (now - session.LastDisconnectedAtUtc.Value <= TimeSpan.FromMinutes(graceMinutes))
                {
                    continue;
                }

                ModLog.Info(Feature, $"Session finalized — steamId={steamId} session={session.SessionId} after grace period");
                FinalizeOpenSession(steamId, countAsCompleted: true);
                changed = true;
            }

            return changed;
        }

        private static bool HasOpenDisconnectedSessions()
        {
            return StatisticsRuntime.HasOpenDisconnectedSessions();
        }
    }
}
