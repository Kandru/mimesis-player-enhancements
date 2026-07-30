using System.Text;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static partial class WebDashboardJson
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

        public static string SerializeMonsters(IReadOnlyList<WebDashboardMonsterOptionDto> monsters)
        {
            return ModJson.Serialize(new WebDashboardMonstersApiResponse { Monsters = [.. monsters] });
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
