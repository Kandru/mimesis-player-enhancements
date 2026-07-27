using MimesisPlayerEnhancement.Features.WebDashboard;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.WebDashboard
{
    public sealed class WebDashboardMinimapServiceTests
    {
        [Fact]
        public void FilterMarkers_with_focus_returns_matching_alive_marker()
        {
            List<WebDashboardMinimapMarkerDto> markers =
            [
                new() { SteamId = 10, IsAlive = true },
                new() { SteamId = 20, IsAlive = false },
            ];

            List<WebDashboardMinimapMarkerDto> filtered =
                WebDashboardMinimapService.FilterMarkers(markers, focusSteamId: 10);

            Assert.Single(filtered);
            Assert.Equal(10UL, filtered[0].SteamId);
        }

        [Fact]
        public void FilterMarkers_without_focus_returns_all_alive_markers()
        {
            List<WebDashboardMinimapMarkerDto> markers =
            [
                new() { SteamId = 1, IsAlive = true },
                new() { SteamId = 2, IsAlive = false },
                new() { SteamId = 3, IsAlive = true },
            ];

            List<WebDashboardMinimapMarkerDto> filtered =
                WebDashboardMinimapService.FilterMarkers(markers);

            Assert.Equal(2, filtered.Count);
            Assert.Contains(filtered, marker => marker.SteamId == 1);
            Assert.Contains(filtered, marker => marker.SteamId == 3);
        }
    }
}
