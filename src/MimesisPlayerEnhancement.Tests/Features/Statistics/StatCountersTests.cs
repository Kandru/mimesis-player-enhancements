using MimesisPlayerEnhancement.Features.Statistics.Models;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Statistics
{
    public sealed class StatCountersTests
    {
        [Fact]
        public void Add_merges_dictionaries_and_scalars()
        {
            StatCounters left = new()
            {
                Deaths = 1,
                ItemsDeposited = 2,
                MonsterKills = { ["monster:1"] = 3 },
            };
            StatCounters right = new()
            {
                Deaths = 4,
                TrainValueDeposited = 10,
                MonsterKills = { ["monster:1"] = 1, ["monster:2"] = 2 },
            };

            left.Add(right);

            Assert.Equal(5, left.Deaths);
            Assert.Equal(2, left.ItemsDeposited);
            Assert.Equal(10, left.TrainValueDeposited);
            Assert.Equal(4, left.MonsterKills["monster:1"]);
            Assert.Equal(2, left.MonsterKills["monster:2"]);
        }

        [Fact]
        public void Clone_is_deep_for_dictionaries()
        {
            StatCounters source = new()
            {
                Deaths = 2,
                MonsterKills = { ["monster:1"] = 1 },
            };

            StatCounters clone = source.Clone();
            clone.MonsterKills["monster:1"] = 99;

            Assert.Equal(1, source.MonsterKills["monster:1"]);
        }
    }
}
