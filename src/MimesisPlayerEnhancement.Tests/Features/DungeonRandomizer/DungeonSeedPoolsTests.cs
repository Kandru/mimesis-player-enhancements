using MimesisPlayerEnhancement.Features.DungeonRandomizer;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.DungeonRandomizer
{
    public sealed class DungeonSeedPoolsTests
    {
        private const string KnownFlowId = "dungeon_A_factory_lv1";

        [Fact]
        public void GetPool_returns_empty_for_Vanilla_flavor()
        {
            ReadOnlySpan<int> pool = DungeonSeedPools.GetPool(KnownFlowId, DungeonSeedFlavor.Vanilla);

            Assert.True(pool.IsEmpty);
        }

        [Fact]
        public void GetPool_returns_seeds_for_known_flow_and_curated_flavor()
        {
            ReadOnlySpan<int> pool = DungeonSeedPools.GetPool(KnownFlowId, DungeonSeedFlavor.Compact);

            Assert.False(pool.IsEmpty);
            Assert.All(pool.ToArray(), seed => Assert.True(seed > 0));
        }

        [Fact]
        public void GetPool_returns_empty_for_unknown_flow()
        {
            ReadOnlySpan<int> pool = DungeonSeedPools.GetPool("unknown_flow_id", DungeonSeedFlavor.Compact);

            Assert.True(pool.IsEmpty);
        }
    }
}
