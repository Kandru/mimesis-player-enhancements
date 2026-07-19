using MimesisPlayerEnhancement.Features.Statistics;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Statistics
{
    public sealed class StatisticsMessagesTests
    {
        private static GlobalStats EmptyGlobal() => new();

        [Fact]
        public void HasAnyGlobalStats_returns_false_for_empty_global()
        {
            Assert.False(StatisticsMessages.HasAnyGlobalStats(EmptyGlobal()));
        }

        [Fact]
        public void HasAnyGlobalStats_returns_true_for_sessions_completed()
        {
            var global = EmptyGlobal();
            global.SessionsCompleted = 1;

            Assert.True(StatisticsMessages.HasAnyGlobalStats(global));
        }

        [Theory]
        [InlineData(nameof(StatCounters.SurvivalDeaths), 1L)]
        [InlineData(nameof(StatCounters.SurvivalWins), 1L)]
        [InlineData(nameof(StatCounters.SurvivalLeftBehind), 1L)]
        [InlineData(nameof(StatCounters.DeathmatchDeaths), 1L)]
        [InlineData(nameof(StatCounters.DeathmatchWins), 1L)]
        [InlineData(nameof(StatCounters.Revives), 1L)]
        [InlineData(nameof(StatCounters.VoiceEvents), 1L)]
        [InlineData(nameof(StatCounters.CurrencyEarned), 1L)]
        [InlineData(nameof(StatCounters.ItemCarryCount), 1L)]
        [InlineData(nameof(StatCounters.DamageToFriend), 1L)]
        [InlineData(nameof(StatCounters.FriendsKilled), 1L)]
        [InlineData(nameof(StatCounters.MimicEncounterCount), 1L)]
        [InlineData(nameof(StatCounters.TimeInStartingVolumeMs), 1L)]
        [InlineData(nameof(StatCounters.TotalConnectedSeconds), 1L)]
        [InlineData(nameof(StatCounters.CyclesCompleted), 1)]
        public void HasAnyGlobalStats_returns_true_for_scalar_counter(string counterName, object value)
        {
            var global = EmptyGlobal();
            typeof(StatCounters).GetField(counterName)!.SetValue(global.Counters, value);

            Assert.True(StatisticsMessages.HasAnyGlobalStats(global));
        }

        [Theory]
        [InlineData(nameof(StatCounters.MonsterKills))]
        [InlineData(nameof(StatCounters.DeathsByMonster))]
        [InlineData(nameof(StatCounters.DeathsByTrap))]
        public void HasAnyGlobalStats_returns_true_for_dictionary_counter(string counterName)
        {
            var global = EmptyGlobal();
            var dictionary = new Dictionary<string, long> { ["key"] = 1 };
            typeof(StatCounters).GetField(counterName)!.SetValue(global.Counters, dictionary);

            Assert.True(StatisticsMessages.HasAnyGlobalStats(global));
        }

        [Fact]
        public void HasAnyGlobalStats_ignores_zero_dictionary_values()
        {
            var global = EmptyGlobal();
            global.Counters.MonsterKills = new Dictionary<string, long> { ["monster:1"] = 0 };

            Assert.False(StatisticsMessages.HasAnyGlobalStats(global));
        }
    }
}
