using Bifrost.ConstEnum;
using MimesisPlayerEnhancement.Features.LootMultiplicator;
using MimesisPlayerEnhancement.Util;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.LootMultiplicator
{
    public sealed class LootMultiplierResolverTests
    {
        private static LootMultiplicatorSceneConfig Config(
            bool enabled = true,
            int baseline = ScalingMath.VanillaPlayerBaseline,
            float mapMultiplier = 1f,
            float mapPerPlayer = ScalingMath.DefaultPerPlayerMultiplier,
            float dropMultiplier = 1f,
            float dropPerPlayer = ScalingMath.DefaultPerPlayerMultiplier,
            string filterMode = "All",
            string allowlist = "",
            string blocklist = "",
            bool autoScaleBudgetForFilter = true,
            int fakeDropChancePercent = 30) =>
            new(
                enabled,
                baseline,
                mapMultiplier,
                mapPerPlayer,
                dropMultiplier,
                dropPerPlayer,
                filterMode,
                allowlist,
                blocklist,
                autoScaleBudgetForFilter,
                fakeDropChancePercent);

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void GetEffectiveMultiplier_returns_neutral_when_feature_disabled(int sourceValue)
        {
            var source = (LootSource)sourceValue;
            LootMultiplicatorSceneConfig config = Config(enabled: false, mapMultiplier: 2f, dropMultiplier: 2f);

            float multiplier = LootMultiplierResolver.GetEffectiveMultiplier(
                source,
                ItemType.Consumable,
                playerCount: 8,
                masterId: 0,
                config);

            Assert.Equal(FeatureToggleGate.NeutralMultiplier, multiplier);
        }

        [Fact]
        public void GetEffectiveMultiplier_returns_neutral_for_trigger_source()
        {
            LootMultiplicatorSceneConfig config = Config(mapMultiplier: 2f, dropMultiplier: 2f);

            float multiplier = LootMultiplierResolver.GetEffectiveMultiplier(
                LootSource.Trigger,
                ItemType.Consumable,
                playerCount: 8,
                masterId: 0,
                config);

            Assert.Equal(FeatureToggleGate.NeutralMultiplier, multiplier);
        }

        [Theory]
        [InlineData(0, 1.5f)]
        [InlineData(1, 2f)]
        public void GetBaseMultiplier_returns_configured_value_for_map_and_drop(int sourceValue, float configured)
        {
            var source = (LootSource)sourceValue;
            LootMultiplicatorSceneConfig config = Config(mapMultiplier: 1.5f, dropMultiplier: 2f);

            float multiplier = LootMultiplierResolver.GetBaseMultiplier(source, ItemType.Consumable, config);

            Assert.Equal(configured, multiplier);
        }

        [Fact]
        public void GetBaseMultiplier_returns_neutral_for_trigger_source()
        {
            LootMultiplicatorSceneConfig config = Config(mapMultiplier: 2f);

            float multiplier = LootMultiplierResolver.GetBaseMultiplier(LootSource.Trigger, ItemType.Consumable, config);

            Assert.Equal(FeatureToggleGate.NeutralMultiplier, multiplier);
        }

        [Theory]
        [InlineData(0, 0.15f)]
        [InlineData(1, 0.20f)]
        public void GetPerPlayerMultiplier_returns_configured_value_for_map_and_drop(int sourceValue, float configured)
        {
            var source = (LootSource)sourceValue;
            LootMultiplicatorSceneConfig config = Config(mapPerPlayer: 0.15f, dropPerPlayer: 0.20f);

            float multiplier = LootMultiplierResolver.GetPerPlayerMultiplier(source, ItemType.Consumable, config);

            Assert.Equal(configured, multiplier);
        }

        [Fact]
        public void GetPerPlayerMultiplier_returns_zero_for_trigger_source()
        {
            LootMultiplicatorSceneConfig config = Config(mapPerPlayer: 0.15f, dropPerPlayer: 0.20f);

            float multiplier = LootMultiplierResolver.GetPerPlayerMultiplier(LootSource.Trigger, ItemType.Consumable, config);

            Assert.Equal(0f, multiplier);
        }

        [Theory]
        [InlineData(4, 1f)]
        [InlineData(5, 1.1f)]
        [InlineData(8, 1.4f)]
        public void GetEffectiveMultiplier_uses_additive_scaling_at_default_baseline(int playerCount, float expectedScale)
        {
            LootMultiplicatorSceneConfig config = Config(mapMultiplier: 1f, mapPerPlayer: 0.10f);

            float scale = LootMultiplierResolver.GetEffectiveMultiplier(
                LootSource.Map,
                ItemType.Consumable,
                playerCount,
                masterId: 0,
                config);

            Assert.Equal(expectedScale, scale);
        }

        [Fact]
        public void GetEffectiveMultiplier_returns_general_when_per_player_is_zero()
        {
            LootMultiplicatorSceneConfig config = Config(mapMultiplier: 1.5f, mapPerPlayer: 0f);

            float multiplier = LootMultiplierResolver.GetEffectiveMultiplier(
                LootSource.Map,
                ItemType.Consumable,
                playerCount: 8,
                masterId: 0,
                config);

            Assert.Equal(1.5f, multiplier);
        }

        [Theory]
        [InlineData(8, 2f, 2.4f)]
        [InlineData(4, 1.5f, 1.5f)]
        public void GetEffectiveMultiplier_combines_general_and_per_player_additive_for_map(
            int playerCount,
            float mapMultiplier,
            float expected)
        {
            LootMultiplicatorSceneConfig config = Config(
                mapMultiplier: mapMultiplier,
                mapPerPlayer: 0.10f);

            float multiplier = LootMultiplierResolver.GetEffectiveMultiplier(
                LootSource.Map,
                ItemType.Consumable,
                playerCount,
                masterId: 0,
                config);

            Assert.Equal(expected, multiplier);
        }

        [Theory]
        [InlineData(ItemType.Consumable, ItemType.Consumable)]
        [InlineData(ItemType.Equipment, ItemType.Equipment)]
        [InlineData(ItemType.Miscellany, ItemType.Miscellany)]
        public void NormalizeItemType_preserves_known_item_types(ItemType input, ItemType expected)
        {
            ItemType normalized = ItemTypeLookup.NormalizeItemType(input);

            Assert.Equal(expected, normalized);
        }
    }
}
