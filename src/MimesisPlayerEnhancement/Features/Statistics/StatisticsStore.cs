using System.Collections.Concurrent;
using System.Threading.Tasks;
using MimesisPlayerEnhancement.Features.Statistics.Models;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    public static class StatisticsStore
    {
        private const string Feature = "Statistics";
        private const int MaxRecentSessions = 20;

        private static int _cachedLoadSlotId = -999;
        private static SlotStatisticsDocument? _cachedLoadSlot;

        private static readonly ConcurrentDictionary<int, PendingSlotSave> InFlightBySlot = new();

        public static int MaxRecentSessionsPerPlayer => MaxRecentSessions;

        private sealed class PendingSlotSave
        {
            internal Dictionary<ulong, PlayerStatisticsDocument>? LatestPlayers;
            internal bool WaitForCompletion;
            internal Task? PrepareTask;
        }

        public static void LoadAllPlayersForSlot(int slotId, Dictionary<ulong, PlayerStatisticsDocument> cache)
        {
            SlotStatisticsDocument? slot = TryLoadSlot(slotId);
            if (slot?.Players == null)
            {
                return;
            }

            foreach (KeyValuePair<ulong, PlayerStatisticsDocument> pair in slot.Players)
            {
                cache[pair.Key] = NormalizePlayer(pair.Value, pair.Key);
            }
        }

        internal static void SaveSlot(
            int slotId,
            IReadOnlyDictionary<ulong, PlayerStatisticsDocument> players,
            bool waitForCompletion = false)
        {
            string? path = SaveSidecarPaths.GetStatisticsPath(slotId);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            PendingSlotSave pending = InFlightBySlot.GetOrAdd(slotId, static _ => new PendingSlotSave());
            lock (pending)
            {
                pending.LatestPlayers = ClonePlayers(players);
                pending.WaitForCompletion = waitForCompletion;

                if (pending.PrepareTask is { IsCompleted: false })
                {
                    if (!waitForCompletion)
                    {
                        ModLog.Debug(Feature, $"Coalescing slot {slotId} statistics save — serialize already in flight.");
                        return;
                    }
                }
                else
                {
                    pending.PrepareTask = Task.Run(() => PrepareAndWrite(slotId, path, pending));
                    if (!waitForCompletion)
                    {
                        return;
                    }
                }
            }

            if (waitForCompletion)
            {
                WaitForTask(pending.PrepareTask);
            }
        }

        internal static void FlushAllSync()
        {
            for (int pass = 0; pass < 8 && !InFlightBySlot.IsEmpty; pass++)
            {
                foreach (KeyValuePair<int, PendingSlotSave> kvp in InFlightBySlot)
                {
                    Task? task;
                    lock (kvp.Value)
                    {
                        task = kvp.Value.PrepareTask;
                    }

                    WaitForTask(task);
                }
            }
        }

        public static void DeleteStatisticsData(int slotId)
        {
            try
            {
                InvalidateLoadCache(slotId);
                SaveSidecarPaths.DeleteAllFilesForSlot(slotId, Feature);
                ModLog.Info(Feature, $"Deleted statistics data for slot {slotId}.");
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"DeleteStatisticsData: {ex.Message}");
            }
        }

        private static void PrepareAndWrite(int slotId, string path, PendingSlotSave pending)
        {
            Dictionary<ulong, PlayerStatisticsDocument>? players;
            bool waitForCompletion;
            lock (pending)
            {
                players = pending.LatestPlayers;
                waitForCompletion = pending.WaitForCompletion;
                pending.LatestPlayers = null;
                pending.WaitForCompletion = false;
            }

            try
            {
                if (players == null)
                {
                    return;
                }

                string json = SerializeSlot(players);
                BackgroundFileWriteQueue.EnqueueText(path, json, Feature, waitForCompletion);
                InvalidateLoadCache(slotId);
                ModLog.Debug(Feature, $"Saved slot {slotId} statistics ({players.Count} players) -> {path}");
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Background slot {slotId} statistics save failed: {ex.Message}");
            }

            ScheduleRetryIfNeeded(slotId, path, pending);
        }

        private static void ScheduleRetryIfNeeded(int slotId, string path, PendingSlotSave pending)
        {
            lock (pending)
            {
                if (pending.LatestPlayers == null)
                {
                    pending.PrepareTask = null;
                    _ = InFlightBySlot.TryRemove(slotId, out _);
                    return;
                }

                pending.PrepareTask = Task.Run(() => PrepareAndWrite(slotId, path, pending));
            }
        }

        private static string SerializeSlot(IReadOnlyDictionary<ulong, PlayerStatisticsDocument> players)
        {
            Dictionary<ulong, PlayerStatisticsDocument> prepared = new(players.Count);
            foreach (KeyValuePair<ulong, PlayerStatisticsDocument> pair in players)
            {
                PlayerStatisticsDocument doc = pair.Value;
                PreparePlayer(doc);
                prepared[pair.Key] = doc;
            }

            return StatisticsJson.SerializeSlot(new SlotStatisticsDocument
            {
                Version = SlotStatisticsDocument.CurrentVersion,
                Players = prepared,
            });
        }

        private static Dictionary<ulong, PlayerStatisticsDocument> ClonePlayers(
            IReadOnlyDictionary<ulong, PlayerStatisticsDocument> players)
        {
            Dictionary<ulong, PlayerStatisticsDocument> clone = new(players.Count);
            foreach (KeyValuePair<ulong, PlayerStatisticsDocument> pair in players)
            {
                clone[pair.Key] = ClonePlayerDocument(pair.Value, pair.Key);
            }

            return clone;
        }

        private static PlayerStatisticsDocument ClonePlayerDocument(PlayerStatisticsDocument source, ulong steamId)
        {
            List<SessionStats> recentSessions = [];
            foreach (SessionStats session in source.RecentSessions)
            {
                recentSessions.Add(CloneSession(session));
            }

            return new PlayerStatisticsDocument
            {
                Version = source.Version,
                SteamId = steamId,
                DisplayName = source.DisplayName,
                Global = new GlobalStats
                {
                    SessionsCompleted = source.Global.SessionsCompleted,
                    Counters = source.Global.Counters.Clone(),
                },
                CurrentSession = source.CurrentSession == null ? null : CloneSession(source.CurrentSession),
                RecentSessions = recentSessions,
            };
        }

        private static SessionStats CloneSession(SessionStats session)
        {
            return new SessionStats
            {
                SessionId = session.SessionId,
                StartedAtUtc = session.StartedAtUtc,
                LastConnectedAtUtc = session.LastConnectedAtUtc,
                LastDisconnectedAtUtc = session.LastDisconnectedAtUtc,
                ReconnectCount = session.ReconnectCount,
                IsOpen = session.IsOpen,
                Counters = session.Counters.Clone(),
            };
        }

        private static void PreparePlayer(PlayerStatisticsDocument doc)
        {
            TrimRecentSessions(doc);
            doc.Version = PlayerStatisticsDocument.CurrentVersion;
        }

        private static SlotStatisticsDocument? TryLoadSlot(int slotId)
        {
            if (_cachedLoadSlotId == slotId && _cachedLoadSlot != null)
            {
                return _cachedLoadSlot;
            }

            string? path = SaveSidecarPaths.GetStatisticsPath(slotId);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            string? json = AtomicFileIO.ReadText(path, Feature);
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            SlotStatisticsDocument? slot = StatisticsJson.DeserializeSlot(json);
            if (slot == null)
            {
                ModLog.Warn(Feature, $"Corrupt statistics file — ignoring: {path}");
                return null;
            }

            _cachedLoadSlotId = slotId;
            _cachedLoadSlot = slot;
            return slot;
        }

        private static void InvalidateLoadCache(int slotId)
        {
            if (_cachedLoadSlotId == slotId)
            {
                _cachedLoadSlotId = -999;
                _cachedLoadSlot = null;
            }
        }

        private static PlayerStatisticsDocument NormalizePlayer(PlayerStatisticsDocument doc, ulong steamId)
        {
            doc.SteamId = steamId;
            doc.RecentSessions ??= [];
            doc.Global ??= new GlobalStats();
            doc.Global.Counters ??= new StatCounters();
            EnsureCounterDictionaries(doc.Global.Counters);
            if (doc.CurrentSession != null)
            {
                doc.CurrentSession.Counters ??= new StatCounters();
                EnsureCounterDictionaries(doc.CurrentSession.Counters);
            }

            foreach (SessionStats session in doc.RecentSessions)
            {
                session.Counters ??= new StatCounters();
                EnsureCounterDictionaries(session.Counters);
            }

            if (doc.Version < PlayerStatisticsDocument.CurrentVersion)
            {
                doc.Version = PlayerStatisticsDocument.CurrentVersion;
            }

            return doc;
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

        private static void TrimRecentSessions(PlayerStatisticsDocument doc)
        {
            while (doc.RecentSessions.Count > MaxRecentSessions)
            {
                doc.RecentSessions.RemoveAt(0);
            }
        }

        private static void WaitForTask(Task? task)
        {
            TaskWaitHelper.WaitSync(task, Feature, "Statistics save");
        }
    }
}
