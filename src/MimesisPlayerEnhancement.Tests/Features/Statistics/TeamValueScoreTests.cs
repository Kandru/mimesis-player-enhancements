using MimesisPlayerEnhancement.Features.Statistics;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Statistics
{
    public sealed class TeamValueScoreTests
    {
        [Fact]
        public void Compute_returns_zero_for_null_counters()
        {
            Assert.Equal(0, TeamValueScore.Compute(null!));
        }

        [Fact]
        public void Compute_returns_zero_for_empty_counters()
        {
            Assert.Equal(0, TeamValueScore.Compute(new StatCounters()));
        }

        [Theory]
        [InlineData(100, 0, 0, 0, 0, 0, 100)]
        [InlineData(0, 4, 0, 0, 0, 0, 100)]
        [InlineData(0, 0, 2, 0, 0, 0, 200)]
        [InlineData(0, 0, 0, 1, 0, 0, -200)]
        [InlineData(0, 0, 0, 0, 100, 0, -50)]
        [InlineData(0, 0, 0, 0, 0, 2, -100)]
        [InlineData(50, 2, 1, 0, 20, 1, 140)]
        public void Compute_applies_weighted_formula(
            long trainValue,
            long monsterKills,
            long revives,
            long friendsKilled,
            long damageToFriend,
            long survivalDeaths,
            double expected)
        {
            var counters = new StatCounters
            {
                TrainValueDeposited = trainValue,
                Revives = revives,
                FriendsKilled = friendsKilled,
                DamageToFriend = damageToFriend,
                SurvivalDeaths = survivalDeaths,
                MonsterKills = monsterKills > 0
                    ? new Dictionary<string, long> { ["monster:1"] = monsterKills }
                    : [],
            };

            Assert.Equal(expected, TeamValueScore.Compute(counters));
        }

        [Fact]
        public void ComputeMedianLifetimeMs_returns_null_for_null_list()
        {
            Assert.Null(TeamValueScore.ComputeMedianLifetimeMs(null));
        }

        [Fact]
        public void ComputeMedianLifetimeMs_returns_null_for_empty_list()
        {
            Assert.Null(TeamValueScore.ComputeMedianLifetimeMs([]));
        }

        [Fact]
        public void ComputeMedianLifetimeMs_returns_middle_for_odd_count()
        {
            Assert.Equal(30, TeamValueScore.ComputeMedianLifetimeMs([10, 30, 50]));
        }

        [Fact]
        public void ComputeMedianLifetimeMs_returns_average_of_middle_pair_for_even_count()
        {
            Assert.Equal(25, TeamValueScore.ComputeMedianLifetimeMs([10, 20, 30, 40]));
        }
    }
}
