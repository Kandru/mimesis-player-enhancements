using MimesisPlayerEnhancement.Features.Statistics;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Statistics
{
    public sealed class LeaderboardBuilderTests
    {
        private static PlayerStatisticsDocument Player(
            ulong steamId,
            string displayName,
            Action<StatCounters>? configureRun = null,
            Action<GlobalStats>? configureGlobal = null,
            Dictionary<int, StatCounters>? zones = null)
        {
            var runCounters = new StatCounters();
            configureRun?.Invoke(runCounters);

            var global = new GlobalStats();
            configureGlobal?.Invoke(global);

            return new PlayerStatisticsDocument
            {
                SteamId = steamId,
                DisplayName = displayName,
                Global = global,
                CurrentRun = new RunStats
                {
                    Counters = runCounters,
                    Zones = zones ?? [],
                },
            };
        }

        [Fact]
        public void BuildFromSnapshot_skips_players_with_zero_steam_id()
        {
            List<PlayerStatisticsDocument> players =
            [
                Player(0, "ghost", c => c.Revives = 5),
                Player(100, "real", c => c.Revives = 1),
            ];

            LeaderboardDocument leaderboard = LeaderboardBuilder.BuildFromSnapshot(
                slotId: 1,
                currentZone: 2,
                players,
                displayNames: new Dictionary<ulong, string> { [100] = "Real Player" });

            Assert.Single(leaderboard.Entries);
            Assert.Equal(100ul, leaderboard.Entries[0].SteamId);
        }

        [Fact]
        public void BuildFromSnapshot_sorts_by_score_then_train_value_then_revives()
        {
            List<PlayerStatisticsDocument> players =
            [
                Player(1, "low", c =>
                {
                    c.TrainValueDeposited = 50;
                    c.Revives = 3;
                }),
                Player(2, "high-score", c =>
                {
                    c.TrainValueDeposited = 10;
                    c.Revives = 1;
                }),
                Player(3, "tie-score", c =>
                {
                    c.TrainValueDeposited = 100;
                    c.Revives = 2;
                }),
                Player(4, "tie-score-higher-revives", c =>
                {
                    c.TrainValueDeposited = 100;
                    c.Revives = 5;
                }),
            ];

            LeaderboardDocument leaderboard = LeaderboardBuilder.BuildFromSnapshot(
                1,
                1,
                players,
                new Dictionary<ulong, string>());

            Assert.Equal(4, leaderboard.Entries.Count);
            Assert.Equal(4ul, leaderboard.Entries[0].SteamId);
            Assert.Equal(1ul, leaderboard.Entries[1].SteamId);
            Assert.Equal(3ul, leaderboard.Entries[2].SteamId);
            Assert.Equal(2ul, leaderboard.Entries[3].SteamId);
        }

        [Fact]
        public void BuildFromSnapshot_aggregates_zone_totals()
        {
            var zone1 = new StatCounters { Revives = 2 };
            var zone2 = new StatCounters { Revives = 3 };

            List<PlayerStatisticsDocument> players =
            [
                Player(
                    10,
                    "player",
                    zones: new Dictionary<int, StatCounters>
                    {
                        [1] = zone1,
                        [2] = zone2,
                    }),
            ];

            LeaderboardDocument leaderboard = LeaderboardBuilder.BuildFromSnapshot(1, 2, players, new Dictionary<ulong, string>());

            Assert.Equal(2, leaderboard.ZoneSummaries.Count);
            Assert.Equal(2, leaderboard.ZoneSummaries[0].Zone);
            Assert.Equal(3, leaderboard.ZoneSummaries[0].Totals.Revives);
            Assert.Equal(1, leaderboard.ZoneSummaries[1].Zone);
            Assert.Equal(2, leaderboard.ZoneSummaries[1].Totals.Revives);
        }

        [Fact]
        public void BuildFromSnapshot_resolves_display_names_and_median_lifetime()
        {
            List<PlayerStatisticsDocument> players =
            [
                Player(42, "fallback", c =>
                {
                    c.LifetimesOnDeathMs = [100, 300];
                }),
            ];

            LeaderboardDocument leaderboard = LeaderboardBuilder.BuildFromSnapshot(
                1,
                1,
                players,
                new Dictionary<ulong, string> { [42] = "Resolved" });

            LeaderboardEntry entry = Assert.Single(leaderboard.Entries);
            Assert.Equal("Resolved", entry.DisplayName);
            Assert.Equal(200, entry.MedianLifetimeMs);
        }

        [Fact]
        public void BuildFromSnapshot_sets_slot_and_zone_metadata()
        {
            LeaderboardDocument leaderboard = LeaderboardBuilder.BuildFromSnapshot(
                7,
                3,
                [],
                new Dictionary<ulong, string>());

            Assert.Equal(7, leaderboard.SaveSlotId);
            Assert.Equal(3, leaderboard.CurrentZone);
        }
    }
}
