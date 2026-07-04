using System;
using System.Globalization;
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
                UpdatedAtUtc = doc.UpdatedAtUtc.ToString("O", CultureInfo.InvariantCulture),
                ConnectedSteamIds = connected,
                Entries = entries,
            });
        }

        public static string SerializePlayerStats(PlayerStatisticsDocument doc)
        {
            return ModJson.Serialize(MapPlayerStats(doc));
        }

        public static string SerializePlayerStatsSnapshot(PlayerStatisticsDocument source, string? displayNameOverride)
        {
            PlayerStatisticsDocument snapshot = new()
            {
                Version = source.Version,
                SteamId = source.SteamId,
                DisplayName = source.DisplayName,
                Global = source.Global,
                CurrentSession = source.CurrentSession,
                RecentSessions = source.RecentSessions,
            };

            if (!string.IsNullOrWhiteSpace(displayNameOverride)
                && !string.Equals(displayNameOverride, source.SteamId.ToString(), StringComparison.Ordinal))
            {
                snapshot.DisplayName = displayNameOverride;
            }

            return SerializePlayerStats(snapshot);
        }

        public static string SerializeActionResult(WebDashboardActionResult result)
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

        public static string SerializeSettings(WebDashboardSettingsDto settings)
        {
            return ModJson.Serialize(settings);
        }

        public static string SerializeConfigUpdateResult(WebDashboardConfigUpdateResult result)
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
                if (snapshot.Status.IsConnected)
                {
                    _ = payload.Append(",\"minimap\":")
                        .Append(SerializeMinimap(
                            snapshot.MinimapLayout,
                            snapshot.MinimapMarkers,
                            snapshot.MinimapTrain));
                }

                _ = payload.Append('}');
                return payload.ToString();
            }

            SnapshotEventDto dto = new()
            {
                Status = snapshot.Status,
                Players = players,
            };

            if (snapshot.Status.IsConnected)
            {
                dto.Minimap = BuildMinimapResponse(
                    snapshot.MinimapLayout,
                    snapshot.MinimapMarkers,
                    snapshot.MinimapTrain);
            }

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
            };
        }

        private static PlayerApiDto MapPlayer(WebDashboardPlayerDto player)
        {
            return new PlayerApiDto
            {
                SteamId = player.SteamId.ToString(),
                PlayerUid = player.PlayerUid,
                DisplayName = player.DisplayName,
                IsHost = player.IsHost,
                IsLocal = player.IsLocal,
                IsBanned = player.IsBanned,
                IsAlive = player.IsAlive,
                NetworkGrade = player.NetworkGrade,
                ConnectionRole = player.ConnectionRole,
                ConnectionAddress = player.ConnectionAddress,
                VoiceLineCount = player.VoiceLineCount,
                CurrentSession = player.CurrentSession == null ? null : MapSessionStats(player.CurrentSession),
                Health = player.Health,
                MaxHealth = player.MaxHealth,
                ToxicPercent = player.ToxicPercent,
                LateJoinPhase = player.LateJoinPhase,
                LateJoinLabel = player.LateJoinLabel,
                LateJoinStuckSeconds = player.LateJoinStuckSeconds,
                LateJoinAttemptCount = player.LateJoinAttemptCount,
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
                DisplayName = entry.DisplayName,
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
                SessionsCompleted = entry.SessionsCompleted,
            };
        }

        private static PlayerStatsApiDto MapPlayerStats(PlayerStatisticsDocument doc)
        {
            return new PlayerStatsApiDto
            {
                Version = doc.Version,
                SteamId = doc.SteamId.ToString(),
                DisplayName = doc.DisplayName,
                Global = doc.Global,
                CurrentSession = doc.CurrentSession,
                RecentSessions = doc.RecentSessions,
            };
        }

        private sealed class SnapshotEventDto
        {
            public WebDashboardStatusDto Status = new();
            public List<PlayerApiDto> Players = [];
            public MinimapApiResponse? Minimap;
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
            public long? Health;
            public long? MaxHealth;
            public double? ToxicPercent;
            public string LateJoinPhase = "";
            public string LateJoinLabel = "";
            public float? LateJoinStuckSeconds;
            public int LateJoinAttemptCount;
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
            public Dictionary<string, long> MonsterKills = [];
            public Dictionary<string, long> DeathsByMonster = [];
            public Dictionary<string, long> DeathsByTrap = [];
        }

        private sealed class LeaderboardApiResponse
        {
            public int SaveSlotId;
            public string UpdatedAtUtc = "";
            public List<string> ConnectedSteamIds = [];
            public List<LeaderboardEntryApiDto> Entries = [];
        }

        private sealed class LeaderboardEntryApiDto
        {
            public string SteamId = "";
            public string DisplayName = "";
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
            public int SessionsCompleted;
        }

        private sealed class PlayerStatsApiDto
        {
            public int Version;
            public string SteamId = "";
            public string DisplayName = "";
            public GlobalStats Global = new();
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
        }
    }
}
