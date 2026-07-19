using MimesisPlayerEnhancement.Features.SpawnScaling;
using MimesisPlayerEnhancement.Util;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.SpawnScaling
{
    public sealed class SpawnMultiplierResolverTests
    {
        private static SpawnScalingSceneConfig Config(
            bool enabled = true,
            float scaleRate = ScalingMath.DefaultPlayerCountScaleRate,
            bool autoScaleMimic = true,
            float mimicMultiplier = 1f,
            bool autoScaleBoss = true,
            float bossMultiplier = 1f,
            bool autoScaleJako = true,
            float jakoMultiplier = 1f,
            bool autoScaleSpecial = true,
            float specialMultiplier = 1f,
            bool autoScaleTrap = true,
            float trapMultiplier = 1f,
            bool autoScaleOther = true,
            float otherMultiplier = 1f,
            string periodicSpawnWaitMode = "Vanilla",
            float initialPeriodicSpawnWaitSeconds = 60f,
            float initialPeriodicSpawnWaitMinSeconds = 30f,
            float initialPeriodicSpawnWaitMaxSeconds = 90f,
            float periodicSpawnIntervalSeconds = 30f,
            float periodicSpawnIntervalMinSeconds = 20f,
            float periodicSpawnIntervalMaxSeconds = 45f,
            float mapPlacedEncounterDelayMinSeconds = 5f,
            float mapPlacedEncounterDelayMaxSeconds = 30f,
            float mapPlacedEncounterMinPlayerDistanceMeters = 10f) =>
            new(
                enabled,
                scaleRate,
                autoScaleMimic,
                mimicMultiplier,
                autoScaleBoss,
                bossMultiplier,
                autoScaleJako,
                jakoMultiplier,
                autoScaleSpecial,
                specialMultiplier,
                autoScaleTrap,
                trapMultiplier,
                autoScaleOther,
                otherMultiplier,
                periodicSpawnWaitMode,
                initialPeriodicSpawnWaitSeconds,
                initialPeriodicSpawnWaitMinSeconds,
                initialPeriodicSpawnWaitMaxSeconds,
                periodicSpawnIntervalSeconds,
                periodicSpawnIntervalMinSeconds,
                periodicSpawnIntervalMaxSeconds,
                mapPlacedEncounterDelayMinSeconds,
                mapPlacedEncounterDelayMaxSeconds,
                mapPlacedEncounterMinPlayerDistanceMeters);

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public void GetEffectiveMultiplier_returns_neutral_when_feature_disabled(int categoryValue)
        {
            var category = (SpawnCategory)categoryValue;
            SpawnScalingSceneConfig config = Config(enabled: false, mimicMultiplier: 2f, bossMultiplier: 2f);

            float multiplier = SpawnMultiplierResolver.GetEffectiveMultiplier(category, playerCount: 8, config);

            Assert.Equal(FeatureToggleGate.NeutralMultiplier, multiplier);
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(1, true)]
        [InlineData(2, true)]
        [InlineData(3, true)]
        [InlineData(4, true)]
        [InlineData(5, true)]
        public void IsAutoScaleEnabled_reflects_config_flags(int categoryValue, bool expected)
        {
            var category = (SpawnCategory)categoryValue;
            SpawnScalingSceneConfig config = Config(
                autoScaleMimic: true,
                autoScaleBoss: true,
                autoScaleJako: true,
                autoScaleSpecial: true,
                autoScaleTrap: true,
                autoScaleOther: true);

            Assert.Equal(expected, SpawnMultiplierResolver.IsAutoScaleEnabled(category, config));
        }

        [Theory]
        [InlineData(0, 1.5f)]
        [InlineData(1, 2f)]
        [InlineData(2, 0.5f)]
        [InlineData(3, 1.25f)]
        [InlineData(4, 3f)]
        [InlineData(5, 1.75f)]
        public void GetPerCategoryMultiplier_returns_configured_value(int categoryValue, float configured)
        {
            var category = (SpawnCategory)categoryValue;
            SpawnScalingSceneConfig config = Config(
                mimicMultiplier: 1.5f,
                bossMultiplier: 2f,
                jakoMultiplier: 0.5f,
                specialMultiplier: 1.25f,
                trapMultiplier: 3f,
                otherMultiplier: 1.75f);

            float multiplier = SpawnMultiplierResolver.GetPerCategoryMultiplier(category, config);

            Assert.Equal(configured, multiplier);
        }

        [Theory]
        [InlineData(0, 4, 1f)]
        [InlineData(1, 5, 1.1f)]
        [InlineData(2, 8, 1.4f)]
        public void GetPlayerScale_uses_vanilla_baseline_and_scale_rate(
            int categoryValue,
            int playerCount,
            float expectedScale)
        {
            var category = (SpawnCategory)categoryValue;
            SpawnScalingSceneConfig config = Config(scaleRate: 0.10f, autoScaleMimic: true, autoScaleBoss: true, autoScaleJako: true);

            float scale = SpawnMultiplierResolver.GetPlayerScale(category, playerCount, config);

            Assert.Equal(expectedScale, scale);
        }

        [Fact]
        public void GetPlayerScale_returns_one_when_auto_scale_disabled()
        {
            SpawnScalingSceneConfig config = Config(autoScaleMimic: false, scaleRate: 0.50f);

            float scale = SpawnMultiplierResolver.GetPlayerScale(SpawnCategory.Mimic, playerCount: 8, config);

            Assert.Equal(1f, scale);
        }

        [Theory]
        [InlineData(0, 8, 2f, 2.8f)]
        [InlineData(1, 4, 1.5f, 1.5f)]
        public void GetEffectiveMultiplier_combines_per_category_and_player_scale(
            int categoryValue,
            int playerCount,
            float categoryMultiplier,
            float expected)
        {
            var category = (SpawnCategory)categoryValue;
            SpawnScalingSceneConfig config = category switch
            {
                SpawnCategory.Mimic => Config(mimicMultiplier: categoryMultiplier, autoScaleMimic: true, scaleRate: 0.10f),
                SpawnCategory.Boss => Config(bossMultiplier: categoryMultiplier, autoScaleBoss: true, scaleRate: 0.10f),
                _ => Config(otherMultiplier: categoryMultiplier, autoScaleOther: true, scaleRate: 0.10f),
            };

            float multiplier = SpawnMultiplierResolver.GetEffectiveMultiplier(category, playerCount, config);

            Assert.Equal(expected, multiplier);
        }
    }
}
