using System.Globalization;
using System.Linq;
using System.Text;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardJson
    {
        public static string SerializeStatus(WebDashboardStatusDto status)
        {
            return ModJson.Serialize(status);
        }

        public static string SerializePlayers(IReadOnlyList<WebDashboardPlayerDto> players)
        {
            List<PlayerApiDto> mapped = [];
            foreach (WebDashboardPlayerDto player in players)
            {
                mapped.Add(MapPlayer(player));
            }

            return ModJson.Serialize(new PlayersApiResponse { Players = mapped });
        }

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
                UpdatedAtUtc = doc.UpdatedAtUtc.ToString("O", CultureInfo.InvariantCulture),
                ConnectedSteamIds = connected,
                ServerTotals = MapCounters(doc.ServerTotals),
                ZoneSummaries = MapZoneSummaries(doc.ZoneSummaries),
                Entries = entries,
            });
        }

        public static string SerializePlayerStats(PlayerStatisticsDocument doc, string displayName)
        {
            return ModJson.Serialize(MapPlayerStats(doc, displayName));
        }

        public static string SerializeActionResult(WebDashboardActionResult result)
        {
            return ModJson.Serialize(result);
        }

        public static string SerializeChangelogAcknowledgeResult(WebDashboardChangelogAcknowledgeResult result)
        {
            return ModJson.Serialize(result);
        }

        public static string SerializeError(int statusCode, string message)
        {
            return ModJson.Serialize(new ErrorApiResponse
            {
                Error = statusCode,
                Message = message,
            });
        }

        public static string SerializeConfigUpdateResult(WebDashboardConfigUpdateResult result)
        {
            return ModJson.Serialize(result);
        }

        public static string SerializeItems(IReadOnlyList<WebDashboardItemOptionDto> items)
        {
            return ModJson.Serialize(new WebDashboardItemsApiResponse { Items = [.. items] });
        }

        public static string SerializeDungeons(IReadOnlyList<WebDashboardDungeonOptionDto> dungeons)
        {
            return ModJson.Serialize(new WebDashboardDungeonsApiResponse { Dungeons = [.. dungeons] });
        }

        public static string SerializeSpawnItemResult(WebDashboardSpawnItemResult result)
        {
            return ModJson.Serialize(result);
        }

        public static string SerializeSnapshotEvent(
            WebDashboardSnapshot snapshot,
            bool livePlayersOnly = false,
            IReadOnlyList<WebDashboardPlayerDto>? livePlayers = null)
        {
            IReadOnlyList<WebDashboardPlayerDto> playerSource =
                livePlayersOnly && livePlayers != null ? livePlayers : snapshot.Players;

            List<PlayerApiDto> players = [];
            foreach (WebDashboardPlayerDto player in playerSource)
            {
                players.Add(MapPlayer(player));
            }

            if (livePlayersOnly)
            {
                LiveSnapshotEventDto liveDto = new()
                {
                    Status = snapshot.Status,
                    Players = players,
                    PlayersLiveOnly = true,
                };

                return ModJson.Serialize(liveDto);
            }

            bool includeLeaderboard = snapshot.Status.IsHost && !string.IsNullOrEmpty(snapshot.LeaderboardJson);
            if (includeLeaderboard)
            {
                StringBuilder payload = new();
                _ = payload.Append("{\"status\":")
                    .Append(ModJson.Serialize(snapshot.Status))
                    .Append(",\"players\":")
                    .Append(ModJson.Serialize(players))
                    .Append(",\"leaderboard\":")
                    .Append(snapshot.LeaderboardJson);

                _ = payload.Append('}');
                return payload.ToString();
            }

            SnapshotEventDto dto = new()
            {
                Status = snapshot.Status,
                Players = players,
            };

            return ModJson.Serialize(dto);
        }

        public static string SerializeMinimap(
            WebDashboardMinimapLayoutDto layout,
            IReadOnlyList<WebDashboardMinimapMarkerDto> markers,
            WebDashboardMinimapTrainDto? train)
        {
            return ModJson.Serialize(BuildMinimapResponse(layout, markers, train));
        }

        private static MinimapApiResponse BuildMinimapResponse(
            WebDashboardMinimapLayoutDto layout,
            IReadOnlyList<WebDashboardMinimapMarkerDto> markers,
            WebDashboardMinimapTrainDto? train)
        {
            List<MinimapMarkerApiDto> mappedMarkers = [];
            foreach (WebDashboardMinimapMarkerDto marker in markers)
            {
                mappedMarkers.Add(new MinimapMarkerApiDto
                {
                    SteamId = marker.SteamId.ToString(),
                    DisplayName = marker.DisplayName,
                    X = marker.X,
                    Z = marker.Z,
                    Yaw = marker.Yaw,
                    RoomName = marker.RoomName,
                    AreaId = marker.AreaId,
                    TileId = marker.TileId,
                    IsAlive = marker.IsAlive,
                    IsHost = marker.IsHost,
                    IsLocal = marker.IsLocal,
                    FloorIndex = marker.FloorIndex,
                });
            }

            return new MinimapApiResponse
            {
                LayoutVersion = layout.LayoutVersion,
                LayoutKind = layout.LayoutKind,
                DisplayMode = layout.DisplayMode,
                SceneLabel = layout.SceneLabel,
                DefaultAreaId = layout.DefaultAreaId,
                Bounds = layout.Bounds,
                Areas = layout.Areas,
                Tiles = layout.Tiles,
                Connections = layout.Connections,
                Train = train,
                Markers = mappedMarkers,
                PointsOfInterest = layout.PointsOfInterest,
            };
        }

        private static string NormalizeApiDisplayName(ulong steamId, string? displayName)
        {
            if (steamId == 0)
            {
                return "";
            }

            if (!string.IsNullOrWhiteSpace(displayName) && displayName != steamId.ToString())
            {
                return displayName;
            }

            return steamId.ToString();
        }

        private static PlayerApiDto MapPlayer(WebDashboardPlayerDto player)
        {
            bool hideOtherPlayerDetails =
                WebDashboardMinimapBlindMode.ShouldHideOtherPlayers() && !player.IsLocal;

            return new PlayerApiDto
            {
                SteamId = player.SteamId.ToString(),
                PlayerUid = player.PlayerUid,
                DisplayName = NormalizeApiDisplayName(player.SteamId, player.DisplayName),
                IsHost = player.IsHost,
                IsLocal = player.IsLocal,
                IsBanned = player.IsBanned,
                IsAlive = hideOtherPlayerDetails ? true : player.IsAlive,
                NetworkGrade = player.NetworkGrade,
                ConnectionRole = player.ConnectionRole,
                ConnectionAddress = player.ConnectionAddress,
                VoiceLineCount = player.VoiceLineCount,
                CurrentSession = hideOtherPlayerDetails || player.CurrentSession == null
                    ? null
                    : MapSessionStats(player.CurrentSession),
                TotalStats = hideOtherPlayerDetails || player.TotalStats == null
                    ? null
                    : MapSessionStats(player.TotalStats),
                RunStats = hideOtherPlayerDetails || player.RunStats == null
                    ? null
                    : MapSessionStats(player.RunStats),
                ActivityState = hideOtherPlayerDetails ? "" : player.ActivityState,
                ActivityDetail = hideOtherPlayerDetails ? "" : player.ActivityDetail,
                Health = hideOtherPlayerDetails ? null : player.Health,
                MaxHealth = hideOtherPlayerDetails ? null : player.MaxHealth,
                ToxicPercent = hideOtherPlayerDetails ? null : player.ToxicPercent,
                LateJoinPhase = hideOtherPlayerDetails ? "" : player.LateJoinPhase,
                LateJoinLabel = hideOtherPlayerDetails ? "" : player.LateJoinLabel,
                LateJoinStuckSeconds = hideOtherPlayerDetails ? null : player.LateJoinStuckSeconds,
                LateJoinAttemptCount = hideOtherPlayerDetails ? 0 : player.LateJoinAttemptCount,
                GodMode = hideOtherPlayerDetails ? false : player.GodMode,
                NoClip = hideOtherPlayerDetails ? false : player.NoClip,
            };
        }

        private static SessionStatsApiDto MapSessionStats(WebDashboardSessionStatsDto stats)
        {
            return new SessionStatsApiDto
            {
                CurrencyEarned = stats.CurrencyEarned,
                SurvivalDeaths = stats.SurvivalDeaths,
                SurvivalWins = stats.SurvivalWins,
                SurvivalLeftBehind = stats.SurvivalLeftBehind,
                DeathmatchDeaths = stats.DeathmatchDeaths,
                DeathmatchWins = stats.DeathmatchWins,
                Revives = stats.Revives,
                MimicEncounterCount = stats.MimicEncounterCount,
                ItemCarryCount = stats.ItemCarryCount,
                DamageToFriend = stats.DamageToFriend,
                FriendsKilled = stats.FriendsKilled,
                TotalConnectedSeconds = stats.TotalConnectedSeconds,
                TrainValueDeposited = stats.TrainValueDeposited,
                TrapDeaths = stats.TrapDeaths,
                KilledByPlayers = stats.KilledByPlayers,
                DungeonExitsAlive = stats.DungeonExitsAlive,
                DungeonExitsDead = stats.DungeonExitsDead,
                MedianLifetimeMs = stats.MedianLifetimeMs,
                Score = stats.Score,
                MonsterKills = stats.MonsterKills,
                DeathsByMonster = stats.DeathsByMonster,
                DeathsByTrap = stats.DeathsByTrap,
            };
        }

        private static StatCountersApiDto MapCounters(StatCounters counters)
        {
            return new StatCountersApiDto
            {
                CurrencyEarned = counters.CurrencyEarned,
                SurvivalDeaths = counters.SurvivalDeaths,
                SurvivalWins = counters.SurvivalWins,
                SurvivalLeftBehind = counters.SurvivalLeftBehind,
                DeathmatchDeaths = counters.DeathmatchDeaths,
                DeathmatchWins = counters.DeathmatchWins,
                Revives = counters.Revives,
                MimicEncounterCount = counters.MimicEncounterCount,
                ItemCarryCount = counters.ItemCarryCount,
                DamageToFriend = counters.DamageToFriend,
                FriendsKilled = counters.FriendsKilled,
                TotalConnectedSeconds = counters.TotalConnectedSeconds,
                TrainValueDeposited = counters.TrainValueDeposited,
                TrapDeaths = counters.TrapDeaths,
                KilledByPlayers = counters.KilledByPlayers,
                DungeonExitsAlive = counters.DungeonExitsAlive,
                DungeonExitsDead = counters.DungeonExitsDead,
                MedianLifetimeMs = TeamValueScore.ComputeMedianLifetimeMs(counters.LifetimesOnDeathMs),
                Score = TeamValueScore.Compute(counters),
                MonsterKills = counters.MonsterKills,
                DeathsByMonster = counters.DeathsByMonster,
                DeathsByTrap = counters.DeathsByTrap,
                MonsterKillBreakdown = StatisticsApiMapper.MapEntityCounts(counters.MonsterKills),
                DeathsByMonsterBreakdown = StatisticsApiMapper.MapEntityCounts(counters.DeathsByMonster),
                DeathsByTrapBreakdown = StatisticsApiMapper.MapEntityCounts(counters.DeathsByTrap),
            };
        }

        private static List<ZoneStatsApiDto> MapZoneSummaries(IReadOnlyList<LeaderboardZoneSummary> zones)
        {
            List<ZoneStatsApiDto> mapped = [];
            foreach (LeaderboardZoneSummary zone in zones)
            {
                mapped.Add(new ZoneStatsApiDto
                {
                    Zone = zone.Zone,
                    Totals = MapCounters(zone.Totals),
                });
            }

            return mapped;
        }

        private static LeaderboardEntryApiDto MapLeaderboardEntry(LeaderboardEntry entry)
        {
            return new LeaderboardEntryApiDto
            {
                SteamId = entry.SteamId.ToString(),
                DisplayName = NormalizeApiDisplayName(entry.SteamId, entry.DisplayName),
                Score = entry.Score,
                AllTimeScore = entry.AllTimeScore,
                ItemCarryCount = entry.ItemCarryCount,
                DamageToFriend = entry.DamageToFriend,
                FriendsKilled = entry.FriendsKilled,
                MimicEncounterCount = entry.MimicEncounterCount,
                TimeInStartingVolumeMs = entry.TimeInStartingVolumeMs,
                CurrencyEarned = entry.CurrencyEarned,
                VoiceEvents = entry.VoiceEvents,
                SurvivalDeaths = entry.SurvivalDeaths,
                SurvivalWins = entry.SurvivalWins,
                SurvivalLeftBehind = entry.SurvivalLeftBehind,
                DeathmatchDeaths = entry.DeathmatchDeaths,
                DeathmatchWins = entry.DeathmatchWins,
                Revives = entry.Revives,
                TotalConnectedSeconds = entry.TotalConnectedSeconds,
                TrainValueDeposited = entry.TrainValueDeposited,
                TrapDeaths = entry.TrapDeaths,
                KilledByPlayers = entry.KilledByPlayers,
                DungeonExitsAlive = entry.DungeonExitsAlive,
                DungeonExitsDead = entry.DungeonExitsDead,
                MedianLifetimeMs = entry.MedianLifetimeMs,
                SessionsCompleted = entry.SessionsCompleted,
                RunRestarts = entry.RunRestarts,
                Run = MapCounters(entry.RunCounters),
                AllTime = MapCounters(entry.AllTimeCounters),
                Zones = MapPlayerZones(entry.ZoneCounters),
            };
        }

        private static Dictionary<string, StatCountersApiDto> MapPlayerZones(Dictionary<int, StatCounters> zones)
        {
            Dictionary<string, StatCountersApiDto> mapped = [];
            foreach (KeyValuePair<int, StatCounters> pair in zones.OrderByDescending(static zone => zone.Key))
            {
                mapped[pair.Key.ToString()] = MapCounters(pair.Value);
            }

            return mapped;
        }

        private static PlayerStatsApiDto MapPlayerStats(PlayerStatisticsDocument doc, string displayName)
        {
            return new PlayerStatsApiDto
            {
                Version = doc.Version,
                SteamId = doc.SteamId.ToString(),
                DisplayName = displayName,
                Global = doc.Global,
                CurrentRun = new RunStatsApiDto
                {
                    StartedAtUtc = doc.CurrentRun.StartedAtUtc.ToString("O", CultureInfo.InvariantCulture),
                    Counters = MapCounters(doc.CurrentRun.Counters),
                    Zones = MapPlayerZones(doc.CurrentRun.Zones),
                },
                CurrentSession = doc.CurrentSession,
                RecentSessions = doc.RecentSessions,
            };
        }

        private sealed class SnapshotEventDto
        {
            public WebDashboardStatusDto Status = new();
            public List<PlayerApiDto> Players = [];
        }

        private sealed class LiveSnapshotEventDto
        {
            public WebDashboardStatusDto Status = new();
            public List<PlayerApiDto> Players = [];
            public bool PlayersLiveOnly;
        }

        private sealed class PlayersApiResponse
        {
            public List<PlayerApiDto> Players = [];
        }

        private sealed class PlayerApiDto
        {
            public string SteamId = "";
            public long PlayerUid;
            public string DisplayName = "";
            public bool IsHost;
            public bool IsLocal;
            public bool IsBanned;
            public bool IsAlive = true;
            public int NetworkGrade = -1;
            public string ConnectionRole = "";
            public string ConnectionAddress = "";
            public int VoiceLineCount;
            public SessionStatsApiDto? CurrentSession;
            public SessionStatsApiDto? TotalStats;
            public SessionStatsApiDto? RunStats;
            public string ActivityState = "";
            public string ActivityDetail = "";
            public long? Health;
            public long? MaxHealth;
            public double? ToxicPercent;
            public string LateJoinPhase = "";
            public string LateJoinLabel = "";
            public float? LateJoinStuckSeconds;
            public int LateJoinAttemptCount;
            public bool GodMode;
            public bool NoClip;
        }

        private sealed class SessionStatsApiDto
        {
            public long CurrencyEarned;
            public long SurvivalDeaths;
            public long SurvivalWins;
            public long SurvivalLeftBehind;
            public long DeathmatchDeaths;
            public long DeathmatchWins;
            public long Revives;
            public long MimicEncounterCount;
            public long ItemCarryCount;
            public long DamageToFriend;
            public long FriendsKilled;
            public long TotalConnectedSeconds;
            public long TrainValueDeposited;
            public long TrapDeaths;
            public long KilledByPlayers;
            public long DungeonExitsAlive;
            public long DungeonExitsDead;
            public long? MedianLifetimeMs;
            public double Score;
            public Dictionary<string, long> MonsterKills = [];
            public Dictionary<string, long> DeathsByMonster = [];
            public Dictionary<string, long> DeathsByTrap = [];
        }

        private sealed class StatCountersApiDto
        {
            public long CurrencyEarned;
            public long SurvivalDeaths;
            public long SurvivalWins;
            public long SurvivalLeftBehind;
            public long DeathmatchDeaths;
            public long DeathmatchWins;
            public long Revives;
            public long MimicEncounterCount;
            public long ItemCarryCount;
            public long DamageToFriend;
            public long FriendsKilled;
            public long TotalConnectedSeconds;
            public long TrainValueDeposited;
            public long TrapDeaths;
            public long KilledByPlayers;
            public long DungeonExitsAlive;
            public long DungeonExitsDead;
            public long? MedianLifetimeMs;
            public double Score;
            public Dictionary<string, long> MonsterKills = [];
            public Dictionary<string, long> DeathsByMonster = [];
            public Dictionary<string, long> DeathsByTrap = [];
            public List<EntityCountEntry> MonsterKillBreakdown = [];
            public List<EntityCountEntry> DeathsByMonsterBreakdown = [];
            public List<EntityCountEntry> DeathsByTrapBreakdown = [];
        }

        private sealed class ZoneStatsApiDto
        {
            public int Zone;
            public StatCountersApiDto Totals = new();
        }

        private sealed class RunStatsApiDto
        {
            public string StartedAtUtc = "";
            public StatCountersApiDto Counters = new();
            public Dictionary<string, StatCountersApiDto> Zones = [];
        }

        private sealed class LeaderboardApiResponse
        {
            public int SaveSlotId;
            public int CurrentZone;
            public string UpdatedAtUtc = "";
            public List<string> ConnectedSteamIds = [];
            public StatCountersApiDto ServerTotals = new();
            public List<ZoneStatsApiDto> ZoneSummaries = [];
            public List<LeaderboardEntryApiDto> Entries = [];
        }

        private sealed class LeaderboardEntryApiDto
        {
            public string SteamId = "";
            public string DisplayName = "";
            public double Score;
            public double AllTimeScore;
            public long ItemCarryCount;
            public long DamageToFriend;
            public long FriendsKilled;
            public long MimicEncounterCount;
            public long TimeInStartingVolumeMs;
            public long CurrencyEarned;
            public long VoiceEvents;
            public long SurvivalDeaths;
            public long SurvivalWins;
            public long SurvivalLeftBehind;
            public long DeathmatchDeaths;
            public long DeathmatchWins;
            public long Revives;
            public long TotalConnectedSeconds;
            public long TrainValueDeposited;
            public long TrapDeaths;
            public long KilledByPlayers;
            public long DungeonExitsAlive;
            public long DungeonExitsDead;
            public long? MedianLifetimeMs;
            public int SessionsCompleted;
            public long RunRestarts;
            public StatCountersApiDto Run = new();
            public StatCountersApiDto AllTime = new();
            public Dictionary<string, StatCountersApiDto> Zones = [];
        }

        private sealed class PlayerStatsApiDto
        {
            public int Version;
            public string SteamId = "";
            public string DisplayName = "";
            public GlobalStats Global = new();
            public RunStatsApiDto? CurrentRun;
            public SessionStats? CurrentSession;
            public List<SessionStats> RecentSessions = [];
        }

        private sealed class ErrorApiResponse
        {
            public int Error;
            public string Message = "";
        }

        private sealed class MinimapApiResponse
        {
            public int LayoutVersion;
            public string LayoutKind = "";
            public string DisplayMode = "hidden";
            public string SceneLabel = "";
            public string DefaultAreaId = "";
            public WebDashboardMinimapBoundsDto Bounds = new();
            public List<WebDashboardMinimapAreaDto> Areas = [];
            public List<WebDashboardMinimapTileDto> Tiles = [];
            public List<WebDashboardMinimapConnectionDto> Connections = [];
            public WebDashboardMinimapTrainDto? Train;
            public List<MinimapMarkerApiDto> Markers = [];
            public List<WebDashboardMinimapPoiDto> PointsOfInterest = [];
        }

        private sealed class MinimapMarkerApiDto
        {
            public string SteamId = "";
            public string DisplayName = "";
            public float X;
            public float Z;
            public float Yaw;
            public string RoomName = "";
            public string AreaId = "";
            public string TileId = "";
            public bool IsAlive = true;
            public bool IsHost;
            public bool IsLocal;
            public int FloorIndex;
        }
    }
}
