using MimesisPlayerEnhancement.Features.Statistics;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Statistics
{
    public sealed class StatisticsVoiceCounterTests
    {
        private const ulong SteamId = 42;

        [Fact]
        public void GetDeltaSinceBaseline_returns_zero_when_current_equals_baseline()
        {
            StatisticsVoiceCounter.Clear();
            var counts = new Dictionary<ulong, int> { [SteamId] = 5 };
            StatisticsVoiceCounter.UpdateBaselines([SteamId], counts);

            int delta = StatisticsVoiceCounter.GetDeltaSinceBaseline(SteamId, counts);

            Assert.Equal(0, delta);
        }

        [Fact]
        public void GetDeltaSinceBaseline_returns_positive_delta_since_baseline()
        {
            StatisticsVoiceCounter.Clear();
            StatisticsVoiceCounter.UpdateBaselines([SteamId], new Dictionary<ulong, int> { [SteamId] = 5 });

            int delta = StatisticsVoiceCounter.GetDeltaSinceBaseline(
                SteamId,
                new Dictionary<ulong, int> { [SteamId] = 8 });

            Assert.Equal(3, delta);
        }

        [Fact]
        public void GetDeltaSinceBaseline_never_returns_negative_delta()
        {
            StatisticsVoiceCounter.Clear();
            StatisticsVoiceCounter.UpdateBaselines([SteamId], new Dictionary<ulong, int> { [SteamId] = 10 });

            int delta = StatisticsVoiceCounter.GetDeltaSinceBaseline(
                SteamId,
                new Dictionary<ulong, int> { [SteamId] = 4 });

            Assert.Equal(0, delta);
        }

        [Fact]
        public void Clear_resets_baseline_state()
        {
            StatisticsVoiceCounter.Clear();
            StatisticsVoiceCounter.UpdateBaselines([SteamId], new Dictionary<ulong, int> { [SteamId] = 5 });
            StatisticsVoiceCounter.Clear();

            int delta = StatisticsVoiceCounter.GetDeltaSinceBaseline(
                SteamId,
                new Dictionary<ulong, int> { [SteamId] = 20 });

            Assert.Equal(0, delta);
        }
    }
}
