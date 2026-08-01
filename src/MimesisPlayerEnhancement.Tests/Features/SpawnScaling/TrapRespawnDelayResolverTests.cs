using MimesisPlayerEnhancement.Features.SpawnScaling;
using MimesisPlayerEnhancement.Util;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.SpawnScaling
{
    public sealed class TrapRespawnDelayResolverTests
    {
        private static SpawnScalingSceneConfig Config(
            bool enabled = true,
            string trapRespawnMode = "Vanilla",
            float trapRespawnDelaySeconds = 5f,
            float trapRespawnDelayMinSeconds = 5f,
            float trapRespawnDelayMaxSeconds = 30f,
            float trapRespawnMinPlayerDistanceMeters = 10f) =>
            new(
                enableSpawnScaling: enabled,
                spawnScalingBaselinePlayerCount: ScalingMath.VanillaPlayerBaseline,
                mimicSpawnMultiplier: 1f,
                mimicSpawnPerPlayerMultiplier: ScalingMath.DefaultPerPlayerMultiplier,
                bossSpawnMultiplier: 1f,
                bossSpawnPerPlayerMultiplier: ScalingMath.DefaultPerPlayerMultiplier,
                jakoSpawnMultiplier: 1f,
                jakoSpawnPerPlayerMultiplier: ScalingMath.DefaultPerPlayerMultiplier,
                specialSpawnMultiplier: 1f,
                specialSpawnPerPlayerMultiplier: ScalingMath.DefaultPerPlayerMultiplier,
                trapSpawnMultiplier: 1f,
                trapSpawnPerPlayerMultiplier: ScalingMath.DefaultPerPlayerMultiplier,
                trapRespawnMode: trapRespawnMode,
                trapRespawnDelaySeconds: trapRespawnDelaySeconds,
                trapRespawnDelayMinSeconds: trapRespawnDelayMinSeconds,
                trapRespawnDelayMaxSeconds: trapRespawnDelayMaxSeconds,
                trapRespawnMinPlayerDistanceMeters: trapRespawnMinPlayerDistanceMeters,
                otherSpawnMultiplier: 1f,
                otherSpawnPerPlayerMultiplier: ScalingMath.DefaultPerPlayerMultiplier,
                ambientMonsterWaveMode: "Vanilla",
                ambientMonsterWaveInitialDelaySeconds: 60f,
                ambientMonsterWaveInitialDelayMinSeconds: 30f,
                ambientMonsterWaveInitialDelayMaxSeconds: 90f,
                ambientMonsterWaveIntervalSeconds: 30f,
                ambientMonsterWaveIntervalMinSeconds: 20f,
                ambientMonsterWaveIntervalMaxSeconds: 45f,
                bonusEncounterDelayMinSeconds: 5f,
                bonusEncounterDelayMaxSeconds: 30f,
                bonusEncounterMinPlayerDistanceMeters: 10f);

        [Theory]
        [InlineData("Vanilla", 0)]
        [InlineData("vanilla", 0)]
        [InlineData("Fixed", 1)]
        [InlineData("fixed", 1)]
        [InlineData("Random", 2)]
        [InlineData("random", 2)]
        [InlineData("bogus", 0)]
        [InlineData(null, 0)]
        public void ParseMode_maps_known_values(string? value, int expectedValue)
        {
            var expected = (TrapRespawnMode)expectedValue;
            Assert.Equal(expected, TrapRespawnDelayResolver.ParseMode(value));
        }

        [Theory]
        [InlineData(false, "Fixed", false)]
        [InlineData(true, "Vanilla", false)]
        [InlineData(true, "Fixed", true)]
        [InlineData(true, "Random", true)]
        public void IsForceRespawnActive_requires_enabled_non_vanilla_mode(
            bool enabled,
            string mode,
            bool expected)
        {
            SpawnScalingSceneConfig config = Config(enabled: enabled, trapRespawnMode: mode);

            Assert.Equal(expected, TrapRespawnDelayResolver.IsForceRespawnActive(config));
        }

        [Fact]
        public void ResolveDelaySeconds_uses_fixed_value_in_fixed_mode()
        {
            SpawnScalingSceneConfig config = Config(trapRespawnMode: "Fixed", trapRespawnDelaySeconds: 12f);

            float seconds = TrapRespawnDelayResolver.ResolveDelaySeconds(config);

            Assert.Equal(12f, seconds);
        }

        [Fact]
        public void ResolveDelaySeconds_returns_zero_in_vanilla_mode()
        {
            SpawnScalingSceneConfig config = Config(trapRespawnMode: "Vanilla", trapRespawnDelaySeconds: 12f);

            float seconds = TrapRespawnDelayResolver.ResolveDelaySeconds(config);

            Assert.Equal(0f, seconds);
        }

        [Fact]
        public void ResolveMinPlayerDistanceMeters_returns_zero_in_vanilla_mode()
        {
            SpawnScalingSceneConfig config = Config(trapRespawnMode: "Vanilla", trapRespawnMinPlayerDistanceMeters: 12f);

            Assert.Equal(0f, TrapRespawnDelayResolver.ResolveMinPlayerDistanceMeters(config));
        }

        [Fact]
        public void ResolveMinPlayerDistanceMeters_uses_config_in_fixed_mode()
        {
            SpawnScalingSceneConfig config = Config(trapRespawnMode: "Fixed", trapRespawnMinPlayerDistanceMeters: 12f);

            Assert.Equal(12f, TrapRespawnDelayResolver.ResolveMinPlayerDistanceMeters(config));
        }

        [Fact]
        public void ResolveDelaySeconds_uses_min_when_random_range_collapsed()
        {
            SpawnScalingSceneConfig config = Config(
                trapRespawnMode: "Random",
                trapRespawnDelayMinSeconds: 18f,
                trapRespawnDelayMaxSeconds: 18f);

            float seconds = TrapRespawnDelayResolver.ResolveDelaySeconds(config);

            Assert.Equal(18f, seconds);
        }
    }
}
