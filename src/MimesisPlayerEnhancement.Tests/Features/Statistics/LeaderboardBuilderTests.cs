using MimesisPlayerEnhancement.Features.Statistics;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Statistics
{
    public sealed class LeaderboardBuilderTests
    {
        [Fact]
        public void BuildFromSnapshot_includes_global_and_zone_counters()
        {
            SlotStatisticsDocument document = new()
            {
                History = { CurrentZone = 2 },
            };
            document.Globals[1] = new PlayerGlobalStats
            {
                SteamId = 1,
                DisplayName = "A",
                HighestZoneReached = 2,
                Counters = { TrainValueDeposited = 40, Revives = 1 },
            };
            document.History.Zones.Add(new ZoneRecord { Zone = 2 });
            document.History.Zones[0].Players[1] = new StatCounters { TrainValueDeposited = 15 };

            LeaderboardDocument leaderboard = StatisticsSummaryBuilder.BuildFromSnapshot(
                1,
                document,
                new Dictionary<ulong, string> { [1] = "A" });

            Assert.Equal(2, leaderboard.CurrentZone);
            Assert.Single(leaderboard.Entries);
            Assert.Equal(40, leaderboard.Entries[0].Global.TrainValueDeposited);
            Assert.Equal(15, leaderboard.Entries[0].CurrentZone.TrainValueDeposited);
            Assert.Equal(2, leaderboard.Entries[0].HighestZoneReached);
        }
    }
}
