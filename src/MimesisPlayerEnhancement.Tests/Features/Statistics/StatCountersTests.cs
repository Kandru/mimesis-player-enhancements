using MimesisPlayerEnhancement.Features.Statistics.Models;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Statistics
{
    public sealed class StatCountersTests
    {
        [Fact]
        public void Add_merges_scalar_counters()
        {
            var left = new StatCounters { Revives = 2, TrainValueDeposited = 10 };
            var right = new StatCounters { Revives = 3, TrainValueDeposited = 5 };

            left.Add(right);

            Assert.Equal(5, left.Revives);
            Assert.Equal(15, left.TrainValueDeposited);
        }

        [Fact]
        public void Add_merges_dictionary_counters()
        {
            var left = new StatCounters
            {
                MonsterKills = new Dictionary<string, long> { ["monster:1"] = 2 },
            };
            var right = new StatCounters
            {
                MonsterKills = new Dictionary<string, long>
                {
                    ["monster:1"] = 3,
                    ["monster:2"] = 1,
                },
            };

            left.Add(right);

            Assert.Equal(5, left.MonsterKills["monster:1"]);
            Assert.Equal(1, left.MonsterKills["monster:2"]);
        }

        [Fact]
        public void Add_appends_lifetime_samples()
        {
            var left = new StatCounters { LifetimesOnDeathMs = [100] };
            var right = new StatCounters { LifetimesOnDeathMs = [200, 300] };

            left.Add(right);

            Assert.Equal([100, 200, 300], left.LifetimesOnDeathMs);
        }

        [Fact]
        public void Add_caps_lifetime_samples_at_MaxLifetimeSamples()
        {
            var left = new StatCounters();
            for (int i = 0; i < StatCounters.MaxLifetimeSamples; i++)
            {
                left.LifetimesOnDeathMs.Add(i);
            }

            var right = new StatCounters { LifetimesOnDeathMs = [999, 1000] };
            left.Add(right);

            Assert.Equal(StatCounters.MaxLifetimeSamples, left.LifetimesOnDeathMs.Count);
            Assert.Equal(2, left.LifetimesOnDeathMs[0]);
            Assert.Equal(999, left.LifetimesOnDeathMs[^2]);
            Assert.Equal(1000, left.LifetimesOnDeathMs[^1]);
        }

        [Fact]
        public void Clone_creates_independent_copy()
        {
            var original = new StatCounters
            {
                Revives = 4,
                MonsterKills = new Dictionary<string, long> { ["monster:1"] = 2 },
                LifetimesOnDeathMs = [50],
            };

            StatCounters clone = original.Clone();
            original.Revives = 99;
            original.MonsterKills["monster:1"] = 99;
            original.LifetimesOnDeathMs[0] = 99;

            Assert.Equal(4, clone.Revives);
            Assert.Equal(2, clone.MonsterKills["monster:1"]);
            Assert.Equal(50, clone.LifetimesOnDeathMs[0]);
        }

        [Fact]
        public void HasAnyRunData_returns_false_for_empty_counters()
        {
            Assert.False(new StatCounters().HasAnyRunData());
        }

        [Fact]
        public void HasAnyRunData_returns_true_when_dictionary_has_entries()
        {
            var counters = new StatCounters
            {
                DeathsByTrap = new Dictionary<string, long> { ["trap:1"] = 1 },
            };

            Assert.True(counters.HasAnyRunData());
        }
    }
}
