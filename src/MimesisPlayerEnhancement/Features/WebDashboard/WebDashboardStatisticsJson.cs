using System.Globalization;
using System.Linq;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static partial class WebDashboardJson
    {
        public static string SerializeLeaderboardResponse(LeaderboardDocument doc, IReadOnlyCollection<ulong> connectedSteamIds)
        {
            List<LeaderboardEntryApiDto> entries = [];
            foreach (LeaderboardEntry entry in doc.Entries)
            {
                entries.Add(MapLeaderboardEntry(entry));
            }

            List<string> connected = [];
            foreach (ulong steamId in connectedSteamIds)
            {
                connected.Add(steamId.ToString());
            }

            return ModJson.Serialize(new LeaderboardApiResponse
            {
                SaveSlotId = doc.SaveSlotId,
                CurrentZone = doc.CurrentZone,
                HistoryRevision = doc.HistoryRevision,
                UpdatedAtUtc = doc.UpdatedAtUtc.ToString("O", CultureInfo.InvariantCulture),
                ConnectedSteamIds = connected,
                ServerGlobalTotals = MapCounters(doc.ServerGlobalTotals),
                ServerZoneTotals = MapCounters(doc.ServerZoneTotals),
                Entries = entries,
            });
        }

        public static string SerializeStatisticsHistory(StatisticsHistoryDocument history)
        {
            List<StatisticsHistoryZoneApiDto> zones = [];
            foreach (StatisticsHistoryZone zone in history.Zones)
            {
                zones.Add(MapHistoryZone(zone));
            }

            return ModJson.Serialize(new StatisticsHistoryApiResponse
            {
                SaveSlotId = history.SaveSlotId,
                CurrentZone = history.CurrentZone,
                HistoryRevision = history.HistoryRevision,
                UpdatedAtUtc = history.UpdatedAtUtc.ToString("O", CultureInfo.InvariantCulture),
                TrimmedZoneCount = history.TrimmedZoneCount,
                Zones = zones,
            });
        }

        public static string SerializePlayerStats(ulong steamId, string displayName, SlotStatisticsDocument document)
        {
            return ModJson.Serialize(MapPlayerStats(steamId, displayName, document));
        }

        internal static StatCountersApiDto MapCounters(StatCounters counters)
        {
            return new StatCountersApiDto
            {
                TrainValueDeposited = counters.TrainValueDeposited,
                ItemsDeposited = counters.ItemsDeposited,
                ItemsCarried = counters.ItemsCarried,
                MonsterKillsTotal = counters.MonsterKillTotal,
                FriendsKilled = counters.FriendsKilled,
                KilledByFriends = counters.KilledByFriends,
                Deaths = counters.Deaths,
                TrapDeathsTotal = counters.TrapDeathTotal,
                Revives = counters.Revives,
                SurvivalWins = counters.SurvivalWins,
                SurvivalLeftBehind = counters.SurvivalLeftBehind,
                DeathmatchDeaths = counters.DeathmatchDeaths,
                DeathmatchWins = counters.DeathmatchWins,
                DamageToFriend = counters.DamageToFriend,
                MimicEncounters = counters.MimicEncounters,
                ConnectedSeconds = counters.ConnectedSeconds,
                DungeonExitsAlive = counters.DungeonExitsAlive,
                DungeonExitsDead = counters.DungeonExitsDead,
                MedianLifetimeMs = TeamValueScore.ComputeMedianLifetimeMs(counters.LifetimesOnDeathMs),
                Score = TeamValueScore.Compute(counters),
                MonsterKillBreakdown = StatisticsApiMapper.MapEntityCounts(counters.MonsterKills),
                DeathsByMonsterBreakdown = StatisticsApiMapper.MapEntityCounts(counters.DeathsByMonster),
                DeathsByTrapBreakdown = StatisticsApiMapper.MapEntityCounts(counters.DeathsByTrap),
            };
        }

        internal static SessionStatsApiDto MapSessionStats(WebDashboardSessionStatsDto stats)
        {
            return new SessionStatsApiDto
            {
                TrainValueDeposited = stats.TrainValueDeposited,
                ItemsDeposited = stats.ItemsDeposited,
                ItemsCarried = stats.ItemsCarried,
                MonsterKillsTotal = SumDictionary(stats.MonsterKills),
                FriendsKilled = stats.FriendsKilled,
                KilledByFriends = stats.KilledByFriends,
                Deaths = stats.Deaths,
                TrapDeathsTotal = SumDictionary(stats.DeathsByTrap),
                Revives = stats.Revives,
                SurvivalWins = stats.SurvivalWins,
                SurvivalLeftBehind = stats.SurvivalLeftBehind,
                DeathmatchDeaths = stats.DeathmatchDeaths,
                DeathmatchWins = stats.DeathmatchWins,
                DamageToFriend = stats.DamageToFriend,
                MimicEncounters = stats.MimicEncounters,
                ConnectedSeconds = stats.ConnectedSeconds,
                DungeonExitsAlive = stats.DungeonExitsAlive,
                DungeonExitsDead = stats.DungeonExitsDead,
                MedianLifetimeMs = stats.MedianLifetimeMs,
                Score = stats.Score,
                MonsterKills = stats.MonsterKills,
                DeathsByMonster = stats.DeathsByMonster,
                DeathsByTrap = stats.DeathsByTrap,
            };
        }

        private static LeaderboardEntryApiDto MapLeaderboardEntry(LeaderboardEntry entry)
        {
            return new LeaderboardEntryApiDto
            {
                SteamId = entry.SteamId.ToString(),
                DisplayName = NormalizeApiDisplayName(entry.SteamId, entry.DisplayName),
                Score = entry.Score,
                AllTimeScore = entry.AllTimeScore,
                HighestZoneReached = entry.HighestZoneReached,
                SessionsCompleted = entry.SessionsCompleted,
                RunRestarts = entry.RunRestarts,
                DungeonRunsPlayed = entry.DungeonRunsPlayed,
                Global = MapCounters(entry.Global),
                CurrentZone = MapCounters(entry.CurrentZone),
            };
        }

        private static StatisticsHistoryZoneApiDto MapHistoryZone(StatisticsHistoryZone zone)
        {
            List<StatisticsHistoryPlayerApiDto> players = [];
            foreach (StatisticsHistoryPlayerRow row in zone.Players)
            {
                players.Add(MapHistoryPlayer(row));
            }

            List<StatisticsHistoryRunApiDto> runs = [];
            foreach (StatisticsHistoryRun run in zone.Runs)
            {
                runs.Add(MapHistoryRun(run));
            }

            return new StatisticsHistoryZoneApiDto
            {
                Zone = zone.Zone,
                IsCurrent = zone.IsCurrent,
                StartedAtUtc = zone.StartedAtUtc.ToString("O", CultureInfo.InvariantCulture),
                EndedAtUtc = zone.EndedAtUtc?.ToString("O", CultureInfo.InvariantCulture),
                TrimmedRunCount = zone.TrimmedRunCount,
                Totals = MapCounters(zone.Totals),
                Players = players,
                Runs = runs,
            };
        }

        private static StatisticsHistoryRunApiDto MapHistoryRun(StatisticsHistoryRun run)
        {
            List<StatisticsHistoryPlayerApiDto> players = [];
            foreach (StatisticsHistoryPlayerRow row in run.Players)
            {
                players.Add(MapHistoryPlayer(row));
            }

            return new StatisticsHistoryRunApiDto
            {
                RunId = run.RunId,
                Zone = run.Zone,
                Cycle = run.Cycle,
                Seed = run.Seed,
                MapId = run.MapId,
                MapKey = run.MapKey,
                MapName = run.MapName,
                DungeonMasterId = run.DungeonMasterId,
                StartedAtUtc = run.StartedAtUtc.ToString("O", CultureInfo.InvariantCulture),
                EndedAtUtc = run.EndedAtUtc?.ToString("O", CultureInfo.InvariantCulture),
                DurationSeconds = run.DurationSeconds,
                Outcome = run.Outcome.ToString().ToLowerInvariant(),
                Totals = MapCounters(run.Totals),
                Players = players,
            };
        }

        private static StatisticsHistoryPlayerApiDto MapHistoryPlayer(StatisticsHistoryPlayerRow row)
        {
            return new StatisticsHistoryPlayerApiDto
            {
                SteamId = row.SteamId.ToString(),
                DisplayName = NormalizeApiDisplayName(row.SteamId, row.DisplayName),
                Counters = MapCounters(row.Counters),
            };
        }

        private static PlayerStatsApiDto MapPlayerStats(ulong steamId, string displayName, SlotStatisticsDocument document)
        {
            _ = document.Globals.TryGetValue(steamId, out PlayerGlobalStats? global);
            global ??= new PlayerGlobalStats { SteamId = steamId, DisplayName = displayName };

            int currentZone = document.History.CurrentZone;
            StatCounters zoneCounters = new();
            ZoneRecord? zoneRecord = document.History.Zones.Find(z => z.Zone == currentZone);
            if (zoneRecord != null && zoneRecord.Players.TryGetValue(steamId, out StatCounters? counters))
            {
                zoneCounters = counters;
            }

            List<PlayerZoneStatsApiDto> zones = [];
            foreach (ZoneRecord zone in document.History.Zones.OrderByDescending(z => z.Zone))
            {
                if (!zone.Players.TryGetValue(steamId, out StatCounters? playerZone))
                {
                    continue;
                }

                zones.Add(new PlayerZoneStatsApiDto
                {
                    Zone = zone.Zone,
                    Counters = MapCounters(playerZone),
                });
            }

            List<PlayerRunStatsApiDto> recentRuns = [];
            foreach (ZoneRecord zone in document.History.Zones.OrderByDescending(z => z.Zone))
            {
                foreach (DungeonRunRecord run in zone.Runs.OrderByDescending(r => r.StartedAtUtc))
                {
                    if (!run.Players.TryGetValue(steamId, out StatCounters? runCounters))
                    {
                        continue;
                    }

                    recentRuns.Add(new PlayerRunStatsApiDto
                    {
                        RunId = run.RunId,
                        Zone = run.Zone,
                        Cycle = run.Cycle,
                        Seed = run.Seed,
                        MapKey = run.MapKey,
                        MapName = run.MapName,
                        Outcome = run.Outcome.ToString().ToLowerInvariant(),
                        EndedAtUtc = run.EndedAtUtc?.ToString("O", CultureInfo.InvariantCulture),
                        Counters = MapCounters(runCounters),
                    });
                }
            }

            return new PlayerStatsApiDto
            {
                SteamId = steamId.ToString(),
                DisplayName = displayName,
                Global = new PlayerGlobalStatsApiDto
                {
                    Counters = MapCounters(global.Counters),
                    HighestZoneReached = global.HighestZoneReached,
                    RunRestarts = global.RunRestarts,
                    SessionsCompleted = global.SessionsCompleted,
                    DungeonRunsPlayed = global.DungeonRunsPlayed,
                    VoiceEvents = global.VoiceEvents,
                },
                CurrentZone = new PlayerZoneStatsApiDto
                {
                    Zone = currentZone,
                    Counters = MapCounters(zoneCounters),
                },
                Zones = zones,
                RecentRuns = recentRuns,
            };
        }

        private static long SumDictionary(Dictionary<string, long>? values)
        {
            if (values == null)
            {
                return 0;
            }

            long total = 0;
            foreach (long value in values.Values)
            {
                total += value;
            }

            return total;
        }

        private sealed class LeaderboardApiResponse
        {
            public int SaveSlotId;
            public int CurrentZone;
            public int HistoryRevision;
            public string UpdatedAtUtc = "";
            public List<string> ConnectedSteamIds = [];
            public StatCountersApiDto ServerGlobalTotals = new();
            public StatCountersApiDto ServerZoneTotals = new();
            public List<LeaderboardEntryApiDto> Entries = [];
        }

        private sealed class LeaderboardEntryApiDto
        {
            public string SteamId = "";
            public string DisplayName = "";
            public double Score;
            public double AllTimeScore;
            public int HighestZoneReached;
            public int SessionsCompleted;
            public long RunRestarts;
            public int DungeonRunsPlayed;
            public StatCountersApiDto Global = new();
            public StatCountersApiDto CurrentZone = new();
        }

        private sealed class StatisticsHistoryApiResponse
        {
            public int SaveSlotId;
            public int CurrentZone;
            public int HistoryRevision;
            public string UpdatedAtUtc = "";
            public int TrimmedZoneCount;
            public List<StatisticsHistoryZoneApiDto> Zones = [];
        }

        private sealed class StatisticsHistoryZoneApiDto
        {
            public int Zone;
            public bool IsCurrent;
            public string StartedAtUtc = "";
            public string? EndedAtUtc;
            public int TrimmedRunCount;
            public StatCountersApiDto Totals = new();
            public List<StatisticsHistoryPlayerApiDto> Players = [];
            public List<StatisticsHistoryRunApiDto> Runs = [];
        }

        private sealed class StatisticsHistoryRunApiDto
        {
            public string RunId = "";
            public int Zone;
            public int Cycle;
            public int Seed;
            public int MapId;
            public string MapKey = "";
            public string MapName = "";
            public int DungeonMasterId;
            public string StartedAtUtc = "";
            public string? EndedAtUtc;
            public long? DurationSeconds;
            public string Outcome = "";
            public StatCountersApiDto Totals = new();
            public List<StatisticsHistoryPlayerApiDto> Players = [];
        }

        private sealed class StatisticsHistoryPlayerApiDto
        {
            public string SteamId = "";
            public string DisplayName = "";
            public StatCountersApiDto Counters = new();
        }

        internal sealed class StatCountersApiDto
        {
            public long TrainValueDeposited;
            public long ItemsDeposited;
            public long ItemsCarried;
            public long MonsterKillsTotal;
            public long FriendsKilled;
            public long KilledByFriends;
            public long Deaths;
            public long TrapDeathsTotal;
            public long Revives;
            public long SurvivalWins;
            public long SurvivalLeftBehind;
            public long DeathmatchDeaths;
            public long DeathmatchWins;
            public long DamageToFriend;
            public long MimicEncounters;
            public long ConnectedSeconds;
            public long DungeonExitsAlive;
            public long DungeonExitsDead;
            public long? MedianLifetimeMs;
            public double Score;
            public List<EntityCountEntry> MonsterKillBreakdown = [];
            public List<EntityCountEntry> DeathsByMonsterBreakdown = [];
            public List<EntityCountEntry> DeathsByTrapBreakdown = [];
        }

        internal sealed class SessionStatsApiDto
        {
            public long TrainValueDeposited;
            public long ItemsDeposited;
            public long ItemsCarried;
            public long MonsterKillsTotal;
            public long FriendsKilled;
            public long KilledByFriends;
            public long Deaths;
            public long TrapDeathsTotal;
            public long Revives;
            public long SurvivalWins;
            public long SurvivalLeftBehind;
            public long DeathmatchDeaths;
            public long DeathmatchWins;
            public long DamageToFriend;
            public long MimicEncounters;
            public long ConnectedSeconds;
            public long DungeonExitsAlive;
            public long DungeonExitsDead;
            public long? MedianLifetimeMs;
            public double Score;
            public Dictionary<string, long> MonsterKills = [];
            public Dictionary<string, long> DeathsByMonster = [];
            public Dictionary<string, long> DeathsByTrap = [];
        }

        private sealed class PlayerStatsApiDto
        {
            public string SteamId = "";
            public string DisplayName = "";
            public PlayerGlobalStatsApiDto Global = new();
            public PlayerZoneStatsApiDto? CurrentZone;
            public List<PlayerZoneStatsApiDto> Zones = [];
            public List<PlayerRunStatsApiDto> RecentRuns = [];
        }

        private sealed class PlayerGlobalStatsApiDto
        {
            public StatCountersApiDto Counters = new();
            public int HighestZoneReached;
            public long RunRestarts;
            public int SessionsCompleted;
            public int DungeonRunsPlayed;
            public long VoiceEvents;
        }

        private sealed class PlayerZoneStatsApiDto
        {
            public int Zone;
            public StatCountersApiDto Counters = new();
        }

        private sealed class PlayerRunStatsApiDto
        {
            public string RunId = "";
            public int Zone;
            public int Cycle;
            public int Seed;
            public string MapKey = "";
            public string MapName = "";
            public string Outcome = "";
            public string? EndedAtUtc;
            public StatCountersApiDto Counters = new();
        }
    }
}
