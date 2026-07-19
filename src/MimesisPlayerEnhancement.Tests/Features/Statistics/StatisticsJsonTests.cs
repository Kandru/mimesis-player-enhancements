using MimesisPlayerEnhancement.Features.Statistics;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Statistics
{
    public sealed class StatisticsJsonTests
    {
        [Fact]
        public void Serialize_and_deserialize_round_trips_slot_document()
        {
            var slot = new SlotStatisticsDocument
            {
                Players =
                {
                    [100] = new PlayerStatisticsDocument
                    {
                        SteamId = 100,
                        DisplayName = "Player One",
                        Global = new GlobalStats { SessionsCompleted = 2 },
                        CurrentRun = new RunStats
                        {
                            Counters = new StatCounters { Revives = 3, TrainValueDeposited = 50 },
                        },
                    },
                },
            };

            string json = StatisticsJson.SerializeSlot(slot);
            SlotStatisticsDocument? restored = StatisticsJson.DeserializeSlot(json);

            Assert.NotNull(restored);
            Assert.True(restored.Players.TryGetValue(100, out PlayerStatisticsDocument? player));
            Assert.Equal("Player One", player.DisplayName);
            Assert.Equal(2, player.Global.SessionsCompleted);
            Assert.Equal(3, player.CurrentRun.Counters.Revives);
            Assert.Equal(50, player.CurrentRun.Counters.TrainValueDeposited);
        }

        [Fact]
        public void DeserializeSlot_returns_null_for_blank_json()
        {
            Assert.Null(StatisticsJson.DeserializeSlot(""));
            Assert.Null(StatisticsJson.DeserializeSlot("   "));
        }
    }
}
