using System.Collections.Specialized;
using MimesisPlayerEnhancement.Features.WebDashboard;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.WebDashboard
{
    public sealed class WebDashboardSseSubscriptionsTests
    {
        [Fact]
        public void Parse_defaults_to_snapshot_when_channels_missing()
        {
            WebDashboardSseSubscriptions subscriptions = WebDashboardSseSubscriptions.Parse(null);

            Assert.True(subscriptions.Snapshot);
            Assert.False(subscriptions.Minimap);
            Assert.True(subscriptions.Wants("snapshot"));
            Assert.False(subscriptions.Wants("minimap"));
        }

        [Fact]
        public void Parse_defaults_to_snapshot_when_channels_empty()
        {
            NameValueCollection query = new() { ["channels"] = "   " };

            WebDashboardSseSubscriptions subscriptions = WebDashboardSseSubscriptions.Parse(query);

            Assert.True(subscriptions.Snapshot);
            Assert.False(subscriptions.Minimap);
        }

        [Fact]
        public void Parse_accepts_minimap_only()
        {
            NameValueCollection query = new() { ["channels"] = "minimap" };

            WebDashboardSseSubscriptions subscriptions = WebDashboardSseSubscriptions.Parse(query);

            Assert.False(subscriptions.Snapshot);
            Assert.True(subscriptions.Minimap);
            Assert.True(subscriptions.Wants("minimap"));
            Assert.False(subscriptions.Wants("snapshot"));
        }

        [Fact]
        public void Parse_accepts_both_channels_case_insensitively()
        {
            NameValueCollection query = new() { ["channels"] = " Snapshot , MINIMAP " };

            WebDashboardSseSubscriptions subscriptions = WebDashboardSseSubscriptions.Parse(query);

            Assert.True(subscriptions.Snapshot);
            Assert.True(subscriptions.Minimap);
        }

        [Fact]
        public void Parse_falls_back_to_snapshot_for_unknown_channels()
        {
            NameValueCollection query = new() { ["channels"] = "telemetry,metrics" };

            WebDashboardSseSubscriptions subscriptions = WebDashboardSseSubscriptions.Parse(query);

            Assert.True(subscriptions.Snapshot);
            Assert.False(subscriptions.Minimap);
        }
    }
}
