using MimesisPlayerEnhancement.Util;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Config
{
    public sealed class SceneScopedConfigGateTests
    {
        [Fact]
        public void TransitionToScene_same_kind_does_not_recapture_snapshot()
        {
            var sentinel = new LootMultiplicatorSceneConfig(
                enableLootMultiplicator: false,
                lootMultiplicatorBaselinePlayerCount: ScalingMath.VanillaPlayerBaseline,
                mapLootMultiplier: 9.5f,
                mapLootPerPlayerMultiplier: ScalingMath.DefaultPerPlayerMultiplier,
                dropLootMultiplier: 1f,
                dropLootPerPlayerMultiplier: ScalingMath.DefaultPerPlayerMultiplier,
                lootItemFilterMode: "All",
                lootAllowlist: "",
                lootBlocklist: "",
                autoScaleMapLootBudgetForFilter: true,
                convertFakeActorDyingDropChancePercent: 30);

            SceneScopedConfigGate.SeedActiveForTests(SceneScopeKind.Dungeon, sentinel);
            SceneScopedConfigGate.TransitionToScene(SceneScopeKind.Dungeon);

            Assert.Equal(9.5f, SceneScopedConfigGate.Loot.MapLootMultiplier);
            Assert.False(SceneScopedConfigGate.Loot.EnableLootMultiplicator);
        }

        [Fact]
        public void TransitionToScene_different_kind_recaptures_snapshot()
        {
            var tramLoot = new LootMultiplicatorSceneConfig(
                enableLootMultiplicator: true,
                lootMultiplicatorBaselinePlayerCount: ScalingMath.VanillaPlayerBaseline,
                mapLootMultiplier: 4f,
                mapLootPerPlayerMultiplier: ScalingMath.DefaultPerPlayerMultiplier,
                dropLootMultiplier: 1f,
                dropLootPerPlayerMultiplier: ScalingMath.DefaultPerPlayerMultiplier,
                lootItemFilterMode: "All",
                lootAllowlist: "",
                lootBlocklist: "",
                autoScaleMapLootBudgetForFilter: true,
                convertFakeActorDyingDropChancePercent: 30);
            var dungeonLoot = new LootMultiplicatorSceneConfig(
                enableLootMultiplicator: true,
                lootMultiplicatorBaselinePlayerCount: ScalingMath.VanillaPlayerBaseline,
                mapLootMultiplier: 1f,
                mapLootPerPlayerMultiplier: ScalingMath.DefaultPerPlayerMultiplier,
                dropLootMultiplier: 1f,
                dropLootPerPlayerMultiplier: ScalingMath.DefaultPerPlayerMultiplier,
                lootItemFilterMode: "All",
                lootAllowlist: "",
                lootBlocklist: "",
                autoScaleMapLootBudgetForFilter: true,
                convertFakeActorDyingDropChancePercent: 30);

            SceneScopedConfigGate.SeedActiveForTests(SceneScopeKind.Tram, tramLoot);
            SceneScopedConfigGate.TransitionToScene(SceneScopeKind.Dungeon, dungeonLoot);

            Assert.NotEqual(4f, SceneScopedConfigGate.Loot.MapLootMultiplier);
            Assert.Equal(1f, SceneScopedConfigGate.Loot.MapLootMultiplier);
        }
    }
}
