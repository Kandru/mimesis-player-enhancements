using MimesisPlayerEnhancement.Features.Statistics;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Statistics
{
    public sealed class StatisticsMessagesTests
    {
        private static PlayerGlobalStats EmptyGlobal() => new();

        [Fact]
        public void HasAnyGlobalStats_returns_false_for_empty_global()
        {
            Assert.False(StatisticsMessages.HasAnyGlobalStats(EmptyGlobal()));
        }

        [Fact]
        public void HasAnyGlobalStats_returns_true_for_sessions_completed()
        {
            PlayerGlobalStats global = new() { SessionsCompleted = 1 };
            Assert.True(StatisticsMessages.HasAnyGlobalStats(global));
        }

        [Theory]
        [InlineData(nameof(StatCounters.Deaths), 1L)]
        [InlineData(nameof(StatCounters.TrainValueDeposited), 1L)]
        [InlineData(nameof(StatCounters.ItemsDeposited), 1L)]
        public void HasAnyGlobalStats_returns_true_for_scalar_counter(string counterName, object value)
        {
            PlayerGlobalStats global = new();
            typeof(StatCounters).GetField(counterName)!.SetValue(global.Counters, Convert.ToInt64(value));
            Assert.True(StatisticsMessages.HasAnyGlobalStats(global));
        }
    }
}
