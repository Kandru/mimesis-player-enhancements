using MimesisPlayerEnhancement.Features.LootMultiplicator;
using ReluProtocol.Enum;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.LootMultiplicator
{
    public sealed class LootSourceResolverTests
    {
        [Fact]
        public void TryResolveLootSource_returns_map_when_spawn_context_is_active()
        {
            MapLootSpawnContext.Enter();
            try
            {
                bool resolved = LootSourceResolver.TryResolveLootSource(
                    ReasonOfSpawn.EventAction,
                    spawnPointIndex: 0,
                    out LootSource source);

                Assert.True(resolved);
                Assert.Equal(LootSource.Map, source);
            }
            finally
            {
                MapLootSpawnContext.Exit();
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(42)]
        public void TryResolveLootSource_returns_map_when_spawn_point_index_is_non_zero(int spawnPointIndex)
        {
            bool resolved = LootSourceResolver.TryResolveLootSource(
                ReasonOfSpawn.Admin,
                spawnPointIndex,
                out LootSource source);

            Assert.True(resolved);
            Assert.Equal(LootSource.Map, source);
        }

        [Theory]
        [InlineData(ReasonOfSpawn.Spawn)]
        [InlineData(ReasonOfSpawn.ItemSpawn)]
        public void TryResolveLootSource_returns_map_for_spawn_reasons(ReasonOfSpawn reason)
        {
            bool resolved = LootSourceResolver.TryResolveLootSource(reason, spawnPointIndex: 0, out LootSource source);

            Assert.True(resolved);
            Assert.Equal(LootSource.Map, source);
        }

        [Fact]
        public void TryResolveLootSource_returns_drop_for_actor_dying()
        {
            bool resolved = LootSourceResolver.TryResolveLootSource(
                ReasonOfSpawn.ActorDying,
                spawnPointIndex: 0,
                out LootSource source);

            Assert.True(resolved);
            Assert.Equal(LootSource.Drop, source);
        }

        [Theory]
        [InlineData(ReasonOfSpawn.EventAction)]
        [InlineData(ReasonOfSpawn.Reinforce)]
        [InlineData(ReasonOfSpawn.Gamble)]
        [InlineData(ReasonOfSpawn.Linked)]
        public void TryResolveLootSource_returns_trigger_for_event_reasons(ReasonOfSpawn reason)
        {
            bool resolved = LootSourceResolver.TryResolveLootSource(reason, spawnPointIndex: 0, out LootSource source);

            Assert.True(resolved);
            Assert.Equal(LootSource.Trigger, source);
        }

        [Fact]
        public void TryResolveLootSource_returns_false_for_unlisted_reason()
        {
            bool resolved = LootSourceResolver.TryResolveLootSource(
                ReasonOfSpawn.Admin,
                spawnPointIndex: 0,
                out LootSource source);

            Assert.False(resolved);
            Assert.Equal(default, source);
        }

        [Fact]
        public void ShouldScaleSpawn_returns_false_when_spawn_is_restored()
        {
            Assert.False(LootSourceResolver.ShouldScaleSpawn(ReasonOfSpawn.Spawn, isRestored: true));
        }

        [Theory]
        [InlineData(ReasonOfSpawn.Spawn)]
        [InlineData(ReasonOfSpawn.ActorDying)]
        public void ShouldScaleSpawn_returns_true_for_resolvable_non_restored_reasons(ReasonOfSpawn reason)
        {
            Assert.True(LootSourceResolver.ShouldScaleSpawn(reason, isRestored: false));
        }

        [Fact]
        public void ShouldScaleSpawn_returns_false_for_unlisted_reason()
        {
            Assert.False(LootSourceResolver.ShouldScaleSpawn(ReasonOfSpawn.Admin, isRestored: false));
        }
    }
}
