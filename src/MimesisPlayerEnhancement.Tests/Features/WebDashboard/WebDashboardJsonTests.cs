using MimesisPlayerEnhancement.Features.Statistics.Models;
using MimesisPlayerEnhancement.Features.WebDashboard;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.WebDashboard
{
    public sealed class WebDashboardJsonTests
    {
        [Fact]
        public void SerializeError_includes_status_and_message()
        {
            string json = WebDashboardJson.SerializeError(404, "not found");

            Assert.Contains("\"error\":404", json);
            Assert.Contains("\"message\":\"not found\"", json);
        }

        [Fact]
        public void SerializeActionResult_serializes_success_flag()
        {
            string json = WebDashboardJson.SerializeActionResult(new WebDashboardActionResult
            {
                Success = true,
                Message = "ok",
            });

            Assert.Contains("\"success\":true", json);
            Assert.Contains("\"message\":\"ok\"", json);
        }

        [Fact]
        public void SerializeLeaderboardResponse_includes_connected_steam_ids()
        {
            LeaderboardDocument doc = new()
            {
                SaveSlotId = 2,
                CurrentZone = 1,
                UpdatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            };

            string json = WebDashboardJson.SerializeLeaderboardResponse(doc, [76561198000000001UL]);

            Assert.Contains("\"saveSlotId\":2", json);
            Assert.Contains("\"currentZone\":1", json);
            Assert.Contains("76561198000000001", json);
        }

        [Fact]
        public void SerializePlayers_maps_steam_id_and_display_name()
        {
            string json = WebDashboardJson.SerializePlayers(
            [
                new WebDashboardPlayerDto
                {
                    SteamId = 42,
                    DisplayName = "Alice",
                    IsHost = true,
                },
            ]);

            Assert.Contains("\"steamId\":\"42\"", json);
            Assert.Contains("\"displayName\":\"Alice\"", json);
            Assert.Contains("\"isHost\":true", json);
        }

        [Fact]
        public void SerializeSnapshotEvent_live_only_omits_leaderboard()
        {
            WebDashboardSnapshot snapshot = new()
            {
                Status = new WebDashboardStatusDto { IsHost = true },
                LeaderboardJson = "{\"entries\":[]}",
                Players =
                [
                    new WebDashboardPlayerDto { SteamId = 1, DisplayName = "Host" },
                ],
            };

            string json = WebDashboardJson.SerializeSnapshotEvent(
                snapshot,
                livePlayersOnly: true,
                livePlayers: snapshot.Players);

            Assert.Contains("\"playersLiveOnly\":true", json);
            Assert.DoesNotContain("leaderboard", json);
        }

        [Fact]
        public void SerializeMinimap_includes_layout_kind_and_markers()
        {
            WebDashboardMinimapLayoutDto layout = new()
            {
                LayoutKind = "dungeon",
                DisplayMode = "map",
            };

            string json = WebDashboardJson.SerializeMinimap(
                layout,
                [
                    new WebDashboardMinimapMarkerDto
                    {
                        SteamId = 9,
                        DisplayName = "Bob",
                        X = 0.5f,
                        Z = 0.25f,
                    },
                ],
                train: null);

            Assert.Contains("\"layoutKind\":\"dungeon\"", json);
            Assert.Contains("\"displayName\":\"Bob\"", json);
        }
    }
}
