using MimesisPlayerEnhancement.Features.SpawnScaling;
using MimesisPlayerEnhancement.Util;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.SpawnScaling
{
    public sealed class SpawnMultiplierResolverTests
    {
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
            SpawnScalingSceneConfig config = SpawnScalingSceneConfigTestFactory.Create(
                enableSpawnScaling: false,
                mimicSpawnMultiplier: 2f,
                bossSpawnMultiplier: 2f);

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
            SpawnScalingSceneConfig config = SpawnScalingSceneConfigTestFactory.Create(
                mimicSpawnMultiplier: 1.5f,
                bossSpawnMultiplier: 2f,
                gruntSpawnMultiplier: 0.5f,
                specialSpawnMultiplier: 1.25f,
                trapSpawnMultiplier: 3f,
                otherSpawnMultiplier: 1.75f);

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
            SpawnScalingSceneConfig config = SpawnScalingSceneConfigTestFactory.Create(
                mimicSpawnPerPlayerMultiplier: 0.15f,
                bossSpawnPerPlayerMultiplier: 0.20f,
                gruntSpawnPerPlayerMultiplier: 0.05f,
                specialSpawnPerPlayerMultiplier: 0.12f,
                trapSpawnPerPlayerMultiplier: 0.25f,
                otherSpawnPerPlayerMultiplier: 0.08f);

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
            SpawnScalingSceneConfig config = SpawnScalingSceneConfigTestFactory.Create(
                mimicSpawnMultiplier: 1f,
                mimicSpawnPerPlayerMultiplier: 0.10f);

            float multiplier = SpawnMultiplierResolver.GetEffectiveMultiplier(
                SpawnCategory.Mimic,
                playerCount,
                config);

            Assert.Equal(expected, multiplier);
        }

        [Fact]
        public void GetEffectiveMultiplier_returns_general_when_per_player_is_zero()
        {
            SpawnScalingSceneConfig config = SpawnScalingSceneConfigTestFactory.Create(
                mimicSpawnMultiplier: 1.5f,
                mimicSpawnPerPlayerMultiplier: 0f);

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
                SpawnCategory.Mimic => SpawnScalingSceneConfigTestFactory.Create(
                    mimicSpawnMultiplier: categoryMultiplier,
                    mimicSpawnPerPlayerMultiplier: 0.10f),
                SpawnCategory.Boss => SpawnScalingSceneConfigTestFactory.Create(
                    bossSpawnMultiplier: categoryMultiplier,
                    bossSpawnPerPlayerMultiplier: 0.10f),
                _ => SpawnScalingSceneConfigTestFactory.Create(
                    otherSpawnMultiplier: categoryMultiplier,
                    otherSpawnPerPlayerMultiplier: 0.10f),
            };

            float multiplier = SpawnMultiplierResolver.GetEffectiveMultiplier(category, playerCount, config);

            Assert.Equal(expected, multiplier);
        }
    }
}
