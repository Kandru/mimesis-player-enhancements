using System.Linq;
using MimesisPlayerEnhancement.Features.Statistics.Models;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    public static class StatisticsSummaryBuilder
    {
        public static LeaderboardDocument Build(int slotId, SlotStatisticsDocument document)
        {
            return BuildCore(
                slotId,
                document,
                (steamId, fallback) => SaveSlotDocumentStore.ResolveDisplayName(slotId, steamId, fallback));
        }

        internal static LeaderboardDocument BuildFromSnapshot(
            int slotId,
            SlotStatisticsDocument document,
            IReadOnlyDictionary<ulong, string> displayNames)
        {
            return BuildCore(
                slotId,
                document,
                (steamId, fallback) => displayNames.TryGetValue(steamId, out string? name) && !string.IsNullOrWhiteSpace(name)
                    ? name
                    : fallback ?? steamId.ToString());
        }

        private static LeaderboardDocument BuildCore(
            int slotId,
            SlotStatisticsDocument document,
            Func<ulong, string?, string> resolveDisplayName)
        {
            LeaderboardDocument leaderboard = new()
            {
                SaveSlotId = slotId,
                CurrentZone = document.History.CurrentZone,
                HistoryRevision = StatisticsHistory.Revision,
                UpdatedAtUtc = DateTime.UtcNow,
            };

            ZoneRecord? currentZone = document.History.Zones.FirstOrDefault(z => z.Zone == document.History.CurrentZone);
            foreach (PlayerGlobalStats global in document.Globals.Values)
            {
                if (global.SteamId == 0)
                {
                    continue;
                }

                StatCounters zoneCounters = new();
                if (currentZone != null && currentZone.Players.TryGetValue(global.SteamId, out StatCounters? zonePlayer))
                {
                    zoneCounters = zonePlayer;
                }

                leaderboard.ServerGlobalTotals.Add(global.Counters);
                leaderboard.ServerZoneTotals.Add(zoneCounters);

                leaderboard.Entries.Add(new LeaderboardEntry
                {
                    SteamId = global.SteamId,
                    DisplayName = resolveDisplayName(global.SteamId, global.DisplayName),
                    Score = TeamValueScore.Compute(zoneCounters),
                    AllTimeScore = TeamValueScore.Compute(global.Counters),
                    HighestZoneReached = global.HighestZoneReached,
                    SessionsCompleted = global.SessionsCompleted,
                    RunRestarts = global.RunRestarts,
                    DungeonRunsPlayed = global.DungeonRunsPlayed,
                    Global = global.Counters.Clone(),
                    CurrentZone = zoneCounters.Clone(),
                });
            }

            leaderboard.Entries = [.. leaderboard.Entries
                .OrderByDescending(static entry => entry.AllTimeScore)
                .ThenByDescending(static entry => entry.Global.TrainValueDeposited)
                .ThenByDescending(static entry => entry.Global.Revives)];

            return leaderboard;
        }
    }
}
