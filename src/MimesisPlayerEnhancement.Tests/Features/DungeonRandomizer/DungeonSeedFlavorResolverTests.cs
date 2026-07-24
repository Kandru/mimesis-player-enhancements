using MimesisPlayerEnhancement.Features.DungeonRandomizer;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.DungeonRandomizer
{
    public sealed class DungeonSeedFlavorResolverTests
    {
        [Fact]
        public void ResolveSeedFromPool_returns_matching_pool_seed_when_derived_flow_matches()
        {
            int[] pool = [111, 222, 333];

            int result = DungeonSeedFlavorResolver.ResolveSeedFromPool(
                vanillaSeed: 42,
                flavor: DungeonSeedFlavor.Compact,
                flowId: "flow_a",
                pool,
                maxRate: 1,
                _ => "flow_a",
                logResult: false);

            Assert.Contains(result, pool);
        }

        [Fact]
        public void ResolveSeedFromPool_skips_candidates_with_mismatched_derived_flow()
        {
            int[] pool = [100, 200, 300];

            int result = DungeonSeedFlavorResolver.ResolveSeedFromPool(
                vanillaSeed: 7,
                flavor: DungeonSeedFlavor.Compact,
                flowId: "flow_a",
                pool,
                maxRate: 10,
                candidate => candidate == 200 ? "flow_a" : "flow_b",
                logResult: false);

            Assert.Equal(200, result);
        }

        [Fact]
        public void ResolveSeedFromPool_falls_back_to_vanilla_when_no_candidate_matches_flow()
        {
            int[] pool = [100, 200, 300];

            int result = DungeonSeedFlavorResolver.ResolveSeedFromPool(
                vanillaSeed: 99,
                flavor: DungeonSeedFlavor.Compact,
                flowId: "flow_a",
                pool,
                maxRate: 10,
                _ => "flow_b",
                logResult: false);

            Assert.Equal(99, result);
        }

        [Fact]
        public void ResolveSeedFromPool_returns_vanilla_when_pool_is_empty()
        {
            int result = DungeonSeedFlavorResolver.ResolveSeedFromPool(
                vanillaSeed: 55,
                flavor: DungeonSeedFlavor.Compact,
                flowId: "flow_a",
                ReadOnlySpan<int>.Empty,
                maxRate: 10,
                _ => "flow_a");

            Assert.Equal(55, result);
        }
    }
}
