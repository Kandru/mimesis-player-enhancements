using System;
using System.Threading;
using System.Threading.Tasks;
using MimesisPlayerEnhancement.Features.Statistics.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardLeaderboardCache
    {
        private const string Feature = "WebDashboard";

        private static readonly object SerializeLock = new();

        private static string? _cachedJson;
        private static int _cachedSlotId = -1;
        private static int _cachedRevision = -1;
        private static List<ulong> _cachedLeaderboardSteamIds = [];
        private static int _serializeInFlight;
        private static int _pendingRevision;

        internal static string? UpdateAndGetCached(
            int saveSlotId,
            IReadOnlyList<ulong> connectedSteamIds)
        {
            if (saveSlotId < 0 || !ModConfig.EnableStatistics.Value || !WebDashboardGameState.IsHost())
            {
                return _cachedSlotId == saveSlotId ? _cachedJson : null;
            }

            int revision = StatisticsTracker.Revision;
            if (_cachedSlotId == saveSlotId
                && revision == _cachedRevision
                && !string.IsNullOrEmpty(_cachedJson))
            {
                return _cachedJson;
            }

            ScheduleBackgroundRebuild(saveSlotId, connectedSteamIds, revision);
            return _cachedSlotId == saveSlotId ? _cachedJson : null;
        }

        internal static IReadOnlyList<ulong> GetCachedLeaderboardSteamIds()
        {
            lock (SerializeLock)
            {
                return _cachedLeaderboardSteamIds;
            }
        }

        internal static void Clear()
        {
            lock (SerializeLock)
            {
                _cachedJson = null;
                _cachedSlotId = -1;
                _cachedRevision = -1;
                _cachedLeaderboardSteamIds = [];
            }

            _pendingRevision = 0;
        }

        private static void ScheduleBackgroundRebuild(
            int saveSlotId,
            IReadOnlyList<ulong> connectedSteamIds,
            int revision)
        {
            if (revision > _pendingRevision)
            {
                _pendingRevision = revision;
            }

            if (Interlocked.CompareExchange(ref _serializeInFlight, 1, 0) != 0)
            {
                return;
            }

            List<ulong> connectedIds = [.. connectedSteamIds];
            int rebuildRevision = revision;
            _ = Task.Run(() => BuildAndSerializeBackground(saveSlotId, connectedIds, rebuildRevision));
        }

        private static void BuildAndSerializeBackground(
            int saveSlotId,
            List<ulong> connectedSteamIds,
            int revision)
        {
            try
            {
                List<PlayerStatisticsDocument> livePlayers = CloneForLeaderboard(
                    StatisticsTracker.GetCachedPlayerDocuments());
                LeaderboardDocument doc = livePlayers.Count == 0
                    ? new LeaderboardDocument { SaveSlotId = saveSlotId, UpdatedAtUtc = DateTime.UtcNow }
                    : LeaderboardBuilder.Build(saveSlotId, livePlayers);

                string json = WebDashboardJson.SerializeLeaderboardResponse(doc, connectedSteamIds);
                List<ulong> leaderboardSteamIds = [];
                foreach (LeaderboardEntry entry in doc.Entries)
                {
                    if (entry.SteamId != 0)
                    {
                        leaderboardSteamIds.Add(entry.SteamId);
                    }
                }

                lock (SerializeLock)
                {
                    _cachedJson = json;
                    _cachedSlotId = saveSlotId;
                    _cachedRevision = revision;
                    _cachedLeaderboardSteamIds = leaderboardSteamIds;
                }

                WebDashboardSnapshotCache.MarkDirty();
                WebDashboardSnapshotCache.RequestFullPublish();
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Background leaderboard build failed — {ex.Message}");
            }
            finally
            {
                _ = Interlocked.Exchange(ref _serializeInFlight, 0);
                int cachedRevision;
                int pending;
                lock (SerializeLock)
                {
                    cachedRevision = _cachedRevision;
                }

                pending = Volatile.Read(ref _pendingRevision);
                if (pending != 0 && pending != cachedRevision)
                {
                    WebDashboardSnapshotCache.MarkDirty();
                    WebDashboardSnapshotCache.RequestFullPublish();
                }
            }
        }

        private static List<PlayerStatisticsDocument> CloneForLeaderboard(
            IReadOnlyList<PlayerStatisticsDocument> source)
        {
            List<PlayerStatisticsDocument> cloned = [];
            foreach (PlayerStatisticsDocument player in source)
            {
                if (player.SteamId == 0)
                {
                    continue;
                }

                StatCounters counters = player.Global.Counters;
                cloned.Add(new PlayerStatisticsDocument
                {
                    SteamId = player.SteamId,
                    DisplayName = player.DisplayName,
                    Global = new GlobalStats
                    {
                        SessionsCompleted = player.Global.SessionsCompleted,
                        Counters = new StatCounters
                        {
                            ItemCarryCount = counters.ItemCarryCount,
                            DamageToFriend = counters.DamageToFriend,
                            FriendsKilled = counters.FriendsKilled,
                            MimicEncounterCount = counters.MimicEncounterCount,
                            TimeInStartingVolumeMs = counters.TimeInStartingVolumeMs,
                            CurrencyEarned = counters.CurrencyEarned,
                            VoiceEvents = counters.VoiceEvents,
                            SurvivalDeaths = counters.SurvivalDeaths,
                            SurvivalWins = counters.SurvivalWins,
                            SurvivalLeftBehind = counters.SurvivalLeftBehind,
                            DeathmatchDeaths = counters.DeathmatchDeaths,
                            DeathmatchWins = counters.DeathmatchWins,
                            Revives = counters.Revives,
                            TotalConnectedSeconds = counters.TotalConnectedSeconds,
                        },
                    },
                });
            }

            return cloned;
        }
    }
}
