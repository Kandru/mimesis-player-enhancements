using MimesisPlayerEnhancement.Features.SpawnScaling;
using MimesisPlayerEnhancement.Util;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.SpawnScaling
{
    public sealed class SpawnMultiplierResolverTests
    {
        private static SpawnScalingSceneConfig Config(
            bool enabled = true,
            int baseline = ScalingMath.VanillaPlayerBaseline,
            float mimicMultiplier = 1f,
            float mimicPerPlayer = ScalingMath.DefaultPerPlayerMultiplier,
            float bossMultiplier = 1f,
            float bossPerPlayer = ScalingMath.DefaultPerPlayerMultiplier,
            float jakoMultiplier = 1f,
            float jakoPerPlayer = ScalingMath.DefaultPerPlayerMultiplier,
            float specialMultiplier = 1f,
            float specialPerPlayer = ScalingMath.DefaultPerPlayerMultiplier,
            float trapMultiplier = 1f,
            float trapPerPlayer = ScalingMath.DefaultPerPlayerMultiplier,
            string trapRespawnMode = "Vanilla",
            float trapRespawnDelaySeconds = 5f,
            float trapRespawnDelayMinSeconds = 5f,
            float trapRespawnDelayMaxSeconds = 30f,
            float trapRespawnMinPlayerDistanceMeters = 10f,
            float otherMultiplier = 1f,
            float otherPerPlayer = ScalingMath.DefaultPerPlayerMultiplier,
            string ambientMonsterWaveMode = "Vanilla",
            float ambientMonsterWaveInitialDelaySeconds = 60f,
            float ambientMonsterWaveInitialDelayMinSeconds = 30f,
            float ambientMonsterWaveInitialDelayMaxSeconds = 90f,
            float ambientMonsterWaveIntervalSeconds = 30f,
            float ambientMonsterWaveIntervalMinSeconds = 20f,
            float ambientMonsterWaveIntervalMaxSeconds = 45f,
            float bonusEncounterDelayMinSeconds = 5f,
            float bonusEncounterDelayMaxSeconds = 30f,
            float bonusEncounterMinPlayerDistanceMeters = 10f) =>
            new(
                enabled,
                baseline,
                mimicMultiplier,
                mimicPerPlayer,
                bossMultiplier,
                bossPerPlayer,
                jakoMultiplier,
                jakoPerPlayer,
                specialMultiplier,
                specialPerPlayer,
                trapMultiplier,
                trapPerPlayer,
                trapRespawnMode,
                trapRespawnDelaySeconds,
                trapRespawnDelayMinSeconds,
                trapRespawnDelayMaxSeconds,
                trapRespawnMinPlayerDistanceMeters,
                otherMultiplier,
                otherPerPlayer,
                ambientMonsterWaveMode,
                ambientMonsterWaveInitialDelaySeconds,
                ambientMonsterWaveInitialDelayMinSeconds,
                ambientMonsterWaveInitialDelayMaxSeconds,
                ambientMonsterWaveIntervalSeconds,
                ambientMonsterWaveIntervalMinSeconds,
                ambientMonsterWaveIntervalMaxSeconds,
                bonusEncounterDelayMinSeconds,
                bonusEncounterDelayMaxSeconds,
                bonusEncounterMinPlayerDistanceMeters);

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
        [InlineData(0, 0.15f)]
        [InlineData(1, 0.20f)]
        [InlineData(2, 0.05f)]
        [InlineData(3, 0.12f)]
        [InlineData(4, 0.25f)]
        [InlineData(5, 0.08f)]
        public void GetPerPlayerMultiplier_returns_configured_value(int categoryValue, float configured)
        {
            var category = (SpawnCategory)categoryValue;
            SpawnScalingSceneConfig config = Config(
                mimicPerPlayer: 0.15f,
                bossPerPlayer: 0.20f,
                jakoPerPlayer: 0.05f,
                specialPerPlayer: 0.12f,
                trapPerPlayer: 0.25f,
                otherPerPlayer: 0.08f);

            float multiplier = SpawnMultiplierResolver.GetPerPlayerMultiplier(category, config);

            Assert.Equal(configured, multiplier);
        }

        [Theory]
        [InlineData(4, 1f)]
        [InlineData(5, 1.1f)]
        [InlineData(8, 1.4f)]
        public void GetEffectiveMultiplier_uses_additive_scaling_at_default_baseline(
            int playerCount,
            float expected)
        {
            SpawnScalingSceneConfig config = Config(
                mimicMultiplier: 1f,
                mimicPerPlayer: 0.10f);

            float multiplier = SpawnMultiplierResolver.GetEffectiveMultiplier(
                SpawnCategory.Mimic,
                playerCount,
                config);

            Assert.Equal(expected, multiplier);
        }

        [Fact]
        public void GetEffectiveMultiplier_returns_general_when_per_player_is_zero()
        {
            SpawnScalingSceneConfig config = Config(mimicMultiplier: 1.5f, mimicPerPlayer: 0f);

            float multiplier = SpawnMultiplierResolver.GetEffectiveMultiplier(
                SpawnCategory.Mimic,
                playerCount: 8,
                config);

            Assert.Equal(1.5f, multiplier);
        }

        [Theory]
        [InlineData(0, 8, 2f, 2.4f)]
        [InlineData(1, 4, 1.5f, 1.5f)]
        public void GetEffectiveMultiplier_combines_general_and_per_player_additive(
            int categoryValue,
            int playerCount,
            float categoryMultiplier,
            float expected)
        {
            var category = (SpawnCategory)categoryValue;
            SpawnScalingSceneConfig config = category switch
            {
                SpawnCategory.Mimic => Config(mimicMultiplier: categoryMultiplier, mimicPerPlayer: 0.10f),
                SpawnCategory.Boss => Config(bossMultiplier: categoryMultiplier, bossPerPlayer: 0.10f),
                _ => Config(otherMultiplier: categoryMultiplier, otherPerPlayer: 0.10f),
            };

            float multiplier = SpawnMultiplierResolver.GetEffectiveMultiplier(category, playerCount, config);

            Assert.Equal(expected, multiplier);
        }
    }
}
