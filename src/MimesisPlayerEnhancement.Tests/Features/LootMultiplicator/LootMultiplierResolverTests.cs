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
            float scaleRate = ScalingMath.DefaultPlayerCountScaleRate,
            bool autoScaleMap = true,
            float mapMultiplier = 1f,
            bool autoScaleDrop = true,
            float dropMultiplier = 1f,
            string filterMode = "All",
            string allowlist = "",
            string blocklist = "",
            bool autoScaleBudgetForFilter = true,
            int fakeDropChancePercent = 30) =>
            new(
                enabled,
                scaleRate,
                autoScaleMap,
                mapMultiplier,
                autoScaleDrop,
                dropMultiplier,
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
        [InlineData(0, true, false)]
        [InlineData(1, false, true)]
        [InlineData(2, false, false)]
        public void IsAutoScaleEnabled_reflects_config_flags(
            int sourceValue,
            bool autoScaleMap,
            bool autoScaleDrop)
        {
            var source = (LootSource)sourceValue;
            LootMultiplicatorSceneConfig config = Config(autoScaleMap: autoScaleMap, autoScaleDrop: autoScaleDrop);

            bool enabled = LootMultiplierResolver.IsAutoScaleEnabled(source, ItemType.Consumable, config);

            Assert.Equal(source switch
            {
                LootSource.Map => autoScaleMap,
                LootSource.Drop => autoScaleDrop,
                _ => false,
            }, enabled);
        }

        [Theory]
        [InlineData(4, 1f)]
        [InlineData(5, 1.1f)]
        [InlineData(8, 1.4f)]
        public void GetPlayerScale_uses_vanilla_baseline_and_scale_rate(int playerCount, float expectedScale)
        {
            LootMultiplicatorSceneConfig config = Config(scaleRate: 0.10f, autoScaleMap: true);

            float scale = LootMultiplierResolver.GetPlayerScale(LootSource.Map, ItemType.Consumable, playerCount, config);

            Assert.Equal(expectedScale, scale);
        }

        [Fact]
        public void GetPlayerScale_returns_one_when_auto_scale_disabled()
        {
            LootMultiplicatorSceneConfig config = Config(autoScaleMap: false, scaleRate: 0.50f);

            float scale = LootMultiplierResolver.GetPlayerScale(LootSource.Map, ItemType.Consumable, playerCount: 8, config);

            Assert.Equal(1f, scale);
        }

        [Theory]
        [InlineData(8, 2f, 2.8f)]
        [InlineData(4, 1.5f, 1.5f)]
        public void GetEffectiveMultiplier_combines_base_and_player_scale_for_map(
            int playerCount,
            float mapMultiplier,
            float expected)
        {
            LootMultiplicatorSceneConfig config = Config(
                mapMultiplier: mapMultiplier,
                autoScaleMap: true,
                scaleRate: 0.10f);

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
