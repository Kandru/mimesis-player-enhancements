using MimesisPlayerEnhancement.Features.PlayerAnnouncements;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.PlayerAnnouncements
{
    public sealed class MapRunStatsFormatterTests
    {
        [Fact]
        public void Subtract_computes_delta()
        {
            MapRunStatsSnapshot current = Snapshot(deaths: 3, revives: 2);
            MapRunStatsSnapshot baseline = Snapshot(deaths: 1, revives: 1);

            MapRunStatsSnapshot delta = MapRunStatsFormatter.Subtract(current, baseline);

            Assert.Equal(2, delta.Deaths);
            Assert.Equal(1, delta.Revives);
        }

        [Fact]
        public void Format_returns_empty_message_for_no_activity()
        {
            string result = MapRunStatsFormatter.Format(new MapRunStatsSnapshot());
            Assert.Contains("no recorded activity", result, StringComparison.OrdinalIgnoreCase);
        }

        private static MapRunStatsSnapshot Snapshot(long deaths = 0, long revives = 0)
        {
            return new MapRunStatsSnapshot
            {
                Deaths = deaths,
                Revives = revives,
            };
        }
    }
}
