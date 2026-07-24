using MimesisPlayerEnhancement.Features.WebDashboard;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.WebDashboard
{
    public sealed class WebDashboardMinimapAreaResolverTests
    {
        [Theory]
        [InlineData(WebDashboardMinimapAreaResolver.IndoorAreaId, true)]
        [InlineData("indoor-1", true)]
        [InlineData("indoor-floor-2", true)]
        [InlineData(WebDashboardMinimapAreaResolver.OutdoorAreaId, false)]
        [InlineData(WebDashboardMinimapAreaResolver.HubAreaId, false)]
        [InlineData("Indoor", false)]
        [InlineData("", false)]
        public void IsIndoorAreaId_matches_indoor_prefix(string areaId, bool expected)
        {
            bool result = WebDashboardMinimapAreaResolver.IsIndoorAreaId(areaId);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Area_id_constants_are_stable()
        {
            Assert.Equal("outdoor", WebDashboardMinimapAreaResolver.OutdoorAreaId);
            Assert.Equal("indoor", WebDashboardMinimapAreaResolver.IndoorAreaId);
            Assert.Equal("hub", WebDashboardMinimapAreaResolver.HubAreaId);
        }
    }
}
