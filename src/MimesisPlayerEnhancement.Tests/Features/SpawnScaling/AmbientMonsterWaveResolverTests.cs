using MimesisPlayerEnhancement.Features.SpawnScaling;
using MimesisPlayerEnhancement.Util;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.SpawnScaling
{
    public sealed class AmbientMonsterWaveResolverTests
    {
        private static SpawnScalingSceneConfig Config(
            bool enabled = true,
            string ambientMonsterWaveMode = "Vanilla",
            float ambientMonsterWaveInitialDelaySeconds = 60f,
            float ambientMonsterWaveInitialDelayMinSeconds = 30f,
            float ambientMonsterWaveInitialDelayMaxSeconds = 90f,
            float ambientMonsterWaveIntervalSeconds = 30f,
            float ambientMonsterWaveIntervalMinSeconds = 20f,
            float ambientMonsterWaveIntervalMaxSeconds = 45f,
            string trapRespawnMode = "Vanilla",
            float trapRespawnDelaySeconds = 5f,
            float trapRespawnDelayMinSeconds = 5f,
            float trapRespawnDelayMaxSeconds = 30f,
            float trapRespawnMinPlayerDistanceMeters = 10f) =>
            new(
                enableSpawnScaling: enabled,
                spawnScalingPlayerCountScaleRate: 0.10f,
                autoScaleMimicSpawnsByPlayerCount: true,
                mimicSpawnMultiplier: 1f,
                autoScaleBossSpawnsByPlayerCount: true,
                bossSpawnMultiplier: 1f,
                autoScaleJakoSpawnsByPlayerCount: true,
                jakoSpawnMultiplier: 1f,
                autoScaleSpecialSpawnsByPlayerCount: true,
                specialSpawnMultiplier: 1f,
                autoScaleTrapSpawnsByPlayerCount: true,
                trapSpawnMultiplier: 1f,
                trapRespawnMode: trapRespawnMode,
                trapRespawnDelaySeconds: trapRespawnDelaySeconds,
                trapRespawnDelayMinSeconds: trapRespawnDelayMinSeconds,
                trapRespawnDelayMaxSeconds: trapRespawnDelayMaxSeconds,
                trapRespawnMinPlayerDistanceMeters: trapRespawnMinPlayerDistanceMeters,
                autoScaleOtherSpawnsByPlayerCount: true,
                otherSpawnMultiplier: 1f,
                ambientMonsterWaveMode: ambientMonsterWaveMode,
                ambientMonsterWaveInitialDelaySeconds: ambientMonsterWaveInitialDelaySeconds,
                ambientMonsterWaveInitialDelayMinSeconds: ambientMonsterWaveInitialDelayMinSeconds,
                ambientMonsterWaveInitialDelayMaxSeconds: ambientMonsterWaveInitialDelayMaxSeconds,
                ambientMonsterWaveIntervalSeconds: ambientMonsterWaveIntervalSeconds,
                ambientMonsterWaveIntervalMinSeconds: ambientMonsterWaveIntervalMinSeconds,
                ambientMonsterWaveIntervalMaxSeconds: ambientMonsterWaveIntervalMaxSeconds,
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
            var expected = (AmbientMonsterWaveMode)expectedValue;
            Assert.Equal(expected, AmbientMonsterWaveResolver.ParseMode(value));
        }

        [Theory]
        [InlineData(false, "Fixed", false)]
        [InlineData(true, "Vanilla", false)]
        [InlineData(true, "Fixed", true)]
        [InlineData(true, "Random", true)]
        public void IsWaitModeActive_requires_enabled_non_vanilla_mode(
            bool enabled,
            string mode,
            bool expected)
        {
            SpawnScalingSceneConfig config = Config(enabled: enabled, ambientMonsterWaveMode: mode);

            Assert.Equal(expected, AmbientMonsterWaveResolver.IsWaitModeActive(config));
        }

        [Fact]
        public void ResolveInitialWaitSeconds_uses_fixed_value_in_fixed_mode()
        {
            SpawnScalingSceneConfig config = Config(ambientMonsterWaveMode: "Fixed", ambientMonsterWaveInitialDelaySeconds: 42f);

            float seconds = AmbientMonsterWaveResolver.ResolveInitialWaitSeconds(config);

            Assert.Equal(42f, seconds);
        }

        [Fact]
        public void ResolveInitialWaitSeconds_returns_zero_in_vanilla_mode()
        {
            SpawnScalingSceneConfig config = Config(ambientMonsterWaveMode: "Vanilla", ambientMonsterWaveInitialDelaySeconds: 42f);

            float seconds = AmbientMonsterWaveResolver.ResolveInitialWaitSeconds(config);

            Assert.Equal(0f, seconds);
        }

        [Fact]
        public void ResolveInitialWaitSeconds_uses_min_when_random_range_collapsed()
        {
            SpawnScalingSceneConfig config = Config(
                ambientMonsterWaveMode: "Random",
                ambientMonsterWaveInitialDelayMinSeconds: 25f,
                ambientMonsterWaveInitialDelayMaxSeconds: 25f);

            float seconds = AmbientMonsterWaveResolver.ResolveInitialWaitSeconds(config);

            Assert.Equal(25f, seconds);
        }

        [Theory]
        [InlineData(30f, 30_000)]
        [InlineData(0f, 0)]
        [InlineData(-1f, 0)]
        [InlineData(0.0005f, 1)]
        public void ResolveWaveIntervalMs_converts_fixed_seconds_to_milliseconds(float seconds, int expectedMs)
        {
            SpawnScalingSceneConfig config = Config(
                ambientMonsterWaveMode: "Fixed",
                ambientMonsterWaveIntervalSeconds: seconds);

            int intervalMs = AmbientMonsterWaveResolver.ResolveWaveIntervalMs(config);

            Assert.Equal(expectedMs, intervalMs);
        }

        [Fact]
        public void ResolveWaveIntervalMs_returns_zero_in_vanilla_mode()
        {
            SpawnScalingSceneConfig config = Config(ambientMonsterWaveMode: "Vanilla", ambientMonsterWaveIntervalSeconds: 30f);

            int intervalMs = AmbientMonsterWaveResolver.ResolveWaveIntervalMs(config);

            Assert.Equal(0, intervalMs);
        }

        [Fact]
        public void ResolveWaveIntervalMs_uses_min_when_random_range_collapsed()
        {
            SpawnScalingSceneConfig config = Config(
                ambientMonsterWaveMode: "Random",
                ambientMonsterWaveIntervalMinSeconds: 22f,
                ambientMonsterWaveIntervalMaxSeconds: 22f);

            int intervalMs = AmbientMonsterWaveResolver.ResolveWaveIntervalMs(config);

            Assert.Equal(22_000, intervalMs);
        }
    }
}
