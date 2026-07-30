using MimesisPlayerEnhancement.Features.Statistics;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Statistics
{
    public sealed class StatisticsSummaryBuilderTests
    {
        [Fact]
        public void Build_orders_entries_by_all_time_score()
        {
            SlotStatisticsDocument document = new();
            document.Globals[1] = new PlayerGlobalStats
            {
                SteamId = 1,
                DisplayName = "A",
                Counters = { TrainValueDeposited = 10 },
            };
            document.Globals[2] = new PlayerGlobalStats
            {
                SteamId = 2,
                DisplayName = "B",
                Counters = { TrainValueDeposited = 100 },
            };

            LeaderboardDocument leaderboard = StatisticsSummaryBuilder.Build(0, document);

            Assert.Equal(2UL, leaderboard.Entries[0].SteamId);
            Assert.Equal(100, leaderboard.ServerGlobalTotals.TrainValueDeposited);
        }
    }
}
