using System.Reflection;
using MimesisPlayerEnhancement.Util;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Config
{
    public sealed class SceneScopedConfigGateTests
    {
        private static readonly FieldInfo ActiveLootField =
            typeof(SceneScopedConfigGate).GetField("_activeLoot", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SceneScopedConfigGate._activeLoot not found");

        [Fact]
        public void TransitionToScene_same_kind_does_not_recapture_snapshot()
        {
            SceneScopedConfigGate.Initialize();
            SceneScopedConfigGate.TransitionToScene(SceneScopeKind.Dungeon);

            var sentinel = new LootMultiplicatorSceneConfig(
                enableLootMultiplicator: false,
                lootMultiplicatorPlayerCountScaleRate: 0.10f,
                autoScaleMapLootByPlayerCount: true,
                mapLootMultiplier: 9.5f,
                autoScaleDropLootByPlayerCount: true,
                dropLootMultiplier: 1f,
                lootItemFilterMode: "All",
                lootAllowlist: "",
                lootBlocklist: "",
                autoScaleMapLootBudgetForFilter: true,
                convertFakeActorDyingDropChancePercent: 30);
            ActiveLootField.SetValue(null, sentinel);

            SceneScopedConfigGate.TransitionToScene(SceneScopeKind.Dungeon);

            Assert.Equal(9.5f, SceneScopedConfigGate.Loot.MapLootMultiplier);
            Assert.False(SceneScopedConfigGate.Loot.EnableLootMultiplicator);
        }

        [Fact]
        public void TransitionToScene_different_kind_recaptures_snapshot()
        {
            SceneScopedConfigGate.Initialize();
            SceneScopedConfigGate.TransitionToScene(SceneScopeKind.Tram);

            var sentinel = new LootMultiplicatorSceneConfig(
                enableLootMultiplicator: true,
                lootMultiplicatorPlayerCountScaleRate: 0.10f,
                autoScaleMapLootByPlayerCount: true,
                mapLootMultiplier: 4f,
                autoScaleDropLootByPlayerCount: true,
                dropLootMultiplier: 1f,
                lootItemFilterMode: "All",
                lootAllowlist: "",
                lootBlocklist: "",
                autoScaleMapLootBudgetForFilter: true,
                convertFakeActorDyingDropChancePercent: 30);
            ActiveLootField.SetValue(null, sentinel);

            SceneScopedConfigGate.TransitionToScene(SceneScopeKind.Dungeon);

            Assert.NotEqual(4f, SceneScopedConfigGate.Loot.MapLootMultiplier);
        }
    }
}
