using MimesisPlayerEnhancement.Features.WebDashboard;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.WebDashboard
{
    public sealed class WebDashboardPlayerServiceTests
    {
        [Fact]
        public void ResolveDisplayNameForSteamId_returns_empty_for_zero_steam_id()
        {
            string name = WebDashboardPlayerService.ResolveDisplayNameForSteamId(0);

            Assert.Equal("", name);
        }
    }
}
