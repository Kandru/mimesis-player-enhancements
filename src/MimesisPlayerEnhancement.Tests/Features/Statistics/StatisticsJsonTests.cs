using MimesisPlayerEnhancement.Features.Statistics;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Statistics
{
    public sealed class StatisticsJsonTests
    {
        [Fact]
        public void Round_trip_serializes_v10_document()
        {
            SlotStatisticsDocument slot = new()
            {
                Globals =
                {
                    [100] = new PlayerGlobalStats
                    {
                        SteamId = 100,
                        DisplayName = "Tester",
                        SessionsCompleted = 2,
                        Counters = { Revives = 3, TrainValueDeposited = 50 },
                    },
                },
            };
            slot.History.Zones.Add(new ZoneRecord
            {
                Zone = 1,
                StartedAtUtc = DateTime.UtcNow,
            });

            string json = StatisticsJson.SerializeSlot(slot);
            SlotStatisticsDocument? restored = StatisticsJson.DeserializeSlot(json);

            Assert.NotNull(restored);
            Assert.Equal(SlotStatisticsDocument.CurrentVersion, restored!.Version);
            Assert.True(restored.Globals.TryGetValue(100, out PlayerGlobalStats? player));
            Assert.NotNull(player);
            Assert.Equal(3, player!.Counters.Revives);
            Assert.Equal(50, player.Counters.TrainValueDeposited);
        }
    }
}
