using System.Linq;
using MimesisPlayerEnhancement.Features.Statistics.Models;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    public static class StatisticsHistoryBuilder
    {
        public static StatisticsHistoryDocument Build(
            int slotId,
            SlotStatisticsDocument document,
            Func<ulong, string?, string> resolveDisplayName)
        {
            StatisticsHistoryDocument history = new()
            {
                SaveSlotId = slotId,
                CurrentZone = document.History.CurrentZone,
                HistoryRevision = StatisticsHistory.Revision,
                UpdatedAtUtc = DateTime.UtcNow,
                TrimmedZoneCount = document.History.TrimmedZoneCount,
            };

            foreach (ZoneRecord zone in document.History.Zones.OrderByDescending(static z => z.Zone))
            {
                history.Zones.Add(MapZone(zone, document.History.CurrentZone, resolveDisplayName));
            }

            return history;
        }

        private static StatisticsHistoryZone MapZone(
            ZoneRecord zone,
            int currentZone,
            Func<ulong, string?, string> resolveDisplayName)
        {
            StatisticsHistoryZone mapped = new()
            {
                Zone = zone.Zone,
                IsCurrent = zone.Zone == currentZone,
                StartedAtUtc = zone.StartedAtUtc,
                EndedAtUtc = zone.EndedAtUtc,
                TrimmedRunCount = zone.TrimmedRunCount,
            };

            foreach (KeyValuePair<ulong, StatCounters> pair in zone.Players)
            {
                if (pair.Key == 0)
                {
                    continue;
                }

                mapped.Totals.Add(pair.Value);
                mapped.Players.Add(new StatisticsHistoryPlayerRow
                {
                    SteamId = pair.Key,
                    DisplayName = resolveDisplayName(pair.Key, GetDisplayName(pair.Key)),
                    Counters = pair.Value.Clone(),
                });
            }

            mapped.Players = [.. mapped.Players
                .OrderByDescending(static row => TeamValueScore.Compute(row.Counters))
                .ThenByDescending(static row => row.Counters.TrainValueDeposited)];

            foreach (DungeonRunRecord run in zone.Runs.OrderByDescending(static r => r.StartedAtUtc))
            {
                mapped.Runs.Add(MapRun(run, resolveDisplayName));
            }

            return mapped;
        }

        private static StatisticsHistoryRun MapRun(
            DungeonRunRecord run,
            Func<ulong, string?, string> resolveDisplayName)
        {
            StatisticsHistoryRun mapped = new()
            {
                RunId = run.RunId,
                Zone = run.Zone,
                Cycle = run.Cycle,
                Seed = run.Seed,
                MapId = run.MapId,
                MapKey = run.MapKey,
                MapName = run.MapName,
                DungeonMasterId = run.DungeonMasterId,
                StartedAtUtc = run.StartedAtUtc,
                EndedAtUtc = run.EndedAtUtc,
                Outcome = run.Outcome,
                DurationSeconds = run.EndedAtUtc.HasValue
                    ? (long)Math.Max(0, (run.EndedAtUtc.Value - run.StartedAtUtc).TotalSeconds)
                    : null,
            };

            foreach (KeyValuePair<ulong, StatCounters> pair in run.Players)
            {
                if (pair.Key == 0)
                {
                    continue;
                }

                mapped.Totals.Add(pair.Value);
                mapped.Players.Add(new StatisticsHistoryPlayerRow
                {
                    SteamId = pair.Key,
                    DisplayName = resolveDisplayName(pair.Key, GetDisplayName(pair.Key)),
                    Counters = pair.Value.Clone(),
                });
            }

            mapped.Players = [.. mapped.Players
                .OrderByDescending(static row => TeamValueScore.Compute(row.Counters))
                .ThenByDescending(static row => row.Counters.TrainValueDeposited)];

            return mapped;
        }

        private static string GetDisplayName(ulong steamId)
        {
            return StatisticsHistory.Document.Globals.TryGetValue(steamId, out PlayerGlobalStats? global)
                ? global.DisplayName
                : steamId.ToString();
        }
    }
}
