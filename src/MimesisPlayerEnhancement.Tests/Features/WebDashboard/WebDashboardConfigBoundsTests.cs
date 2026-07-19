using MimesisPlayerEnhancement;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.WebDashboard
{
    public sealed class WebDashboardConfigBoundsTests
    {
        private const string SectionId = "MimesisPlayerEnhancement_WebDashboard";

        [Fact]
        public void WebDashboardListenPort_is_clamped_to_valid_tcp_range()
        {
            Assert.True(ModConfigEntryBounds.TryGet(
                SectionId,
                "WebDashboardListenPort",
                out ModConfigEntryBound bound));
            Assert.Equal("1", bound.MinValue);
            Assert.Equal("65535", bound.MaxValue);
        }
    }
}
