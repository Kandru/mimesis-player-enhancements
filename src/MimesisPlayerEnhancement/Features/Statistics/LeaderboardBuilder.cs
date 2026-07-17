using System.Linq;
using MimesisPlayerEnhancement.Features.Statistics.Models;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    public static class LeaderboardBuilder
    {
        public static LeaderboardDocument Build(int slotId, IEnumerable<PlayerStatisticsDocument> players)
        {
            return BuildCore(
                slotId,
                StatisticsRunTracker.GetCurrentZone(),
                players,
                (steamId, fallback) => SaveSlotDocumentStore.ResolveDisplayName(slotId, steamId, fallback));
        }

        internal static LeaderboardDocument BuildFromSnapshot(
            int slotId,
            int currentZone,
            IEnumerable<PlayerStatisticsDocument> players,
            IReadOnlyDictionary<ulong, string> displayNames)
        {
            return BuildCore(
                slotId,
                currentZone,
                players,
                (steamId, fallback) => displayNames.TryGetValue(steamId, out string? name) && !string.IsNullOrWhiteSpace(name)
                    ? name
                    : fallback ?? steamId.ToString());
        }

        private static LeaderboardDocument BuildCore(
            int slotId,
            int currentZone,
            IEnumerable<PlayerStatisticsDocument> players,
            Func<ulong, string?, string> resolveDisplayName)
        {
            LeaderboardDocument leaderboard = new()
            {
                SaveSlotId = slotId,
                CurrentZone = currentZone,
                UpdatedAtUtc = DateTime.UtcNow,
            };

            Dictionary<int, StatCounters> zoneTotals = [];

            foreach (PlayerStatisticsDocument player in players)
            {
                if (player.SteamId == 0)
                {
                    continue;
                }

                StatCounters run = player.CurrentRun.Counters;
                StatCounters allTime = player.Global.Counters;
                leaderboard.ServerTotals.Add(run);

                foreach (KeyValuePair<int, StatCounters> zone in player.CurrentRun.Zones)
                {
                    if (!zoneTotals.TryGetValue(zone.Key, out StatCounters? totals))
                    {
                        totals = new StatCounters();
                        zoneTotals[zone.Key] = totals;
                    }

                    totals.Add(zone.Value);
                }

                leaderboard.Entries.Add(new LeaderboardEntry
                {
                    SteamId = player.SteamId,
                    DisplayName = resolveDisplayName(player.SteamId, player.DisplayName),
                    Score = TeamValueScore.Compute(run),
                    AllTimeScore = TeamValueScore.Compute(allTime),
                    ItemCarryCount = run.ItemCarryCount,
                    DamageToFriend = run.DamageToFriend,
                    FriendsKilled = run.FriendsKilled,
                    MimicEncounterCount = run.MimicEncounterCount,
                    TimeInStartingVolumeMs = run.TimeInStartingVolumeMs,
                    CurrencyEarned = run.CurrencyEarned,
                    VoiceEvents = run.VoiceEvents,
                    SurvivalDeaths = run.SurvivalDeaths,
                    SurvivalWins = run.SurvivalWins,
                    SurvivalLeftBehind = run.SurvivalLeftBehind,
                    DeathmatchDeaths = run.DeathmatchDeaths,
                    DeathmatchWins = run.DeathmatchWins,
                    Revives = run.Revives,
                    TotalConnectedSeconds = run.TotalConnectedSeconds,
                    TrainValueDeposited = run.TrainValueDeposited,
                    TrapDeaths = run.TrapDeaths,
                    KilledByPlayers = run.KilledByPlayers,
                    DungeonExitsAlive = run.DungeonExitsAlive,
                    DungeonExitsDead = run.DungeonExitsDead,
                    MedianLifetimeMs = TeamValueScore.ComputeMedianLifetimeMs(run.LifetimesOnDeathMs),
                    SessionsCompleted = player.Global.SessionsCompleted,
                    RunRestarts = player.Global.RunRestarts,
                    RunCounters = run.Clone(),
                    AllTimeCounters = allTime.Clone(),
                    ZoneCounters = CloneZoneCounters(player.CurrentRun.Zones),
                });
            }

            foreach (KeyValuePair<int, StatCounters> zone in zoneTotals.OrderByDescending(static pair => pair.Key))
            {
                leaderboard.ZoneSummaries.Add(new LeaderboardZoneSummary
                {
                    Zone = zone.Key,
                    Totals = zone.Value.Clone(),
                });
            }

            leaderboard.Entries = [.. leaderboard.Entries
                .OrderByDescending(static entry => entry.Score)
                .ThenByDescending(static entry => entry.TrainValueDeposited)
                .ThenByDescending(static entry => entry.Revives)];

            return leaderboard;
        }

        private static Dictionary<int, StatCounters> CloneZoneCounters(Dictionary<int, StatCounters> zones)
        {
            Dictionary<int, StatCounters> clone = [];
            foreach (KeyValuePair<int, StatCounters> pair in zones)
            {
                clone[pair.Key] = pair.Value.Clone();
            }

            return clone;
        }
    }
}
