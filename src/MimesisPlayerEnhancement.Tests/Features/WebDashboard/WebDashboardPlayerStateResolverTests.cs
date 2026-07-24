using MimesisPlayerEnhancement.Features.WebDashboard;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.WebDashboard
{
    public sealed class WebDashboardPlayerStateResolverTests
    {
        [Fact]
        public void ApplyActivityState_marks_connecting_when_uid_is_zero()
        {
            WebDashboardPlayerDto dto = new() { PlayerUid = 0, LateJoinLabel = "ignored" };

            WebDashboardPlayerStateResolver.ApplyActivityState(dto, context: null);

            Assert.Equal("connecting", dto.ActivityState);
            Assert.Equal(string.Empty, dto.ActivityDetail);
        }

        [Theory]
        [InlineData(0L, false, false, false, "Unknown", "Unknown", "connecting")]
        [InlineData(1L, false, false, false, "Unknown", "Unknown", "connecting")]
        [InlineData(1L, true, false, false, "Unknown", "Unknown", "loading")]
        [InlineData(1L, true, true, true, "Unknown", "Unknown", "late_join")]
        [InlineData(1L, true, true, false, "WaitingRoom", "Unknown", "tram")]
        [InlineData(1L, true, true, false, "MaintenanceRoom", "Maintenance", "maintenance_room")]
        [InlineData(1L, true, true, false, "MaintenanceRoom", "GamePlay", "maintenance")]
        [InlineData(1L, true, true, false, "Unknown", "GamePlay", "dungeon")]
        [InlineData(1L, true, true, false, "Unknown", "TramWaiting", "tram")]
        [InlineData(1L, true, true, false, "Unknown", "Maintenance", "maintenance_bay")]
        [InlineData(1L, true, true, false, "Unknown", "DeathMatch", "death_match")]
        [InlineData(1L, true, true, false, "Unknown", "Unknown", "online")]
        public void ResolveActivityState_covers_state_matrix(
            long playerUid,
            bool hasPlayer,
            bool levelLoadCompleted,
            bool hasLateJoinLabel,
            string roomKind,
            string sceneKind,
            string expected)
        {
            string result = WebDashboardPlayerStateResolver.ResolveActivityState(
                playerUid,
                hasPlayer,
                levelLoadCompleted,
                hasLateJoinLabel,
                Enum.Parse<WebDashboardActivityRoomKind>(roomKind),
                Enum.Parse<WebDashboardActivitySceneKind>(sceneKind));

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(null, "session")]
        [InlineData("", "session")]
        [InlineData("InTramWaitingScene", "tram")]
        [InlineData("MaintenanceScene", "maintenance")]
        [InlineData("GamePlayScene", "dungeon")]
        [InlineData("DeathMatchScene", "death_match")]
        [InlineData("CustomScene", "CustomScene")]
        public void ResolveLoadingSceneKeyFromTypeName_maps_known_scenes(string? typeName, string expected)
        {
            string result = WebDashboardPlayerStateResolver.ResolveLoadingSceneKeyFromTypeName(typeName);

            Assert.Equal(expected, result);
        }
    }
}
