using MimesisPlayerEnhancement.Features.SpawnScaling;
using MimesisPlayerEnhancement.Util;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.SpawnScaling
{
    public sealed class PeriodicSpawnWaitResolverTests
    {
        private static SpawnScalingSceneConfig Config(
            bool enabled = true,
            string periodicSpawnWaitMode = "Vanilla",
            float initialPeriodicSpawnWaitSeconds = 60f,
            float initialPeriodicSpawnWaitMinSeconds = 30f,
            float initialPeriodicSpawnWaitMaxSeconds = 90f,
            float periodicSpawnIntervalSeconds = 30f,
            float periodicSpawnIntervalMinSeconds = 20f,
            float periodicSpawnIntervalMaxSeconds = 45f) =>
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
                autoScaleOtherSpawnsByPlayerCount: true,
                otherSpawnMultiplier: 1f,
                periodicSpawnWaitMode: periodicSpawnWaitMode,
                initialPeriodicSpawnWaitSeconds: initialPeriodicSpawnWaitSeconds,
                initialPeriodicSpawnWaitMinSeconds: initialPeriodicSpawnWaitMinSeconds,
                initialPeriodicSpawnWaitMaxSeconds: initialPeriodicSpawnWaitMaxSeconds,
                periodicSpawnIntervalSeconds: periodicSpawnIntervalSeconds,
                periodicSpawnIntervalMinSeconds: periodicSpawnIntervalMinSeconds,
                periodicSpawnIntervalMaxSeconds: periodicSpawnIntervalMaxSeconds,
                mapPlacedEncounterDelayMinSeconds: 5f,
                mapPlacedEncounterDelayMaxSeconds: 30f,
                mapPlacedEncounterMinPlayerDistanceMeters: 10f);

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
            var expected = (PeriodicSpawnWaitMode)expectedValue;
            Assert.Equal(expected, PeriodicSpawnWaitResolver.ParseMode(value));
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
            SpawnScalingSceneConfig config = Config(enabled: enabled, periodicSpawnWaitMode: mode);

            Assert.Equal(expected, PeriodicSpawnWaitResolver.IsWaitModeActive(config));
        }

        [Fact]
        public void ResolveInitialWaitSeconds_uses_fixed_value_in_fixed_mode()
        {
            SpawnScalingSceneConfig config = Config(periodicSpawnWaitMode: "Fixed", initialPeriodicSpawnWaitSeconds: 42f);

            float seconds = PeriodicSpawnWaitResolver.ResolveInitialWaitSeconds(config);

            Assert.Equal(42f, seconds);
        }

        [Fact]
        public void ResolveInitialWaitSeconds_returns_zero_in_vanilla_mode()
        {
            SpawnScalingSceneConfig config = Config(periodicSpawnWaitMode: "Vanilla", initialPeriodicSpawnWaitSeconds: 42f);

            float seconds = PeriodicSpawnWaitResolver.ResolveInitialWaitSeconds(config);

            Assert.Equal(0f, seconds);
        }

        [Fact]
        public void ResolveInitialWaitSeconds_uses_min_when_random_range_collapsed()
        {
            SpawnScalingSceneConfig config = Config(
                periodicSpawnWaitMode: "Random",
                initialPeriodicSpawnWaitMinSeconds: 25f,
                initialPeriodicSpawnWaitMaxSeconds: 25f);

            float seconds = PeriodicSpawnWaitResolver.ResolveInitialWaitSeconds(config);

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
                periodicSpawnWaitMode: "Fixed",
                periodicSpawnIntervalSeconds: seconds);

            int intervalMs = PeriodicSpawnWaitResolver.ResolveWaveIntervalMs(config);

            Assert.Equal(expectedMs, intervalMs);
        }

        [Fact]
        public void ResolveWaveIntervalMs_returns_zero_in_vanilla_mode()
        {
            SpawnScalingSceneConfig config = Config(periodicSpawnWaitMode: "Vanilla", periodicSpawnIntervalSeconds: 30f);

            int intervalMs = PeriodicSpawnWaitResolver.ResolveWaveIntervalMs(config);

            Assert.Equal(0, intervalMs);
        }

        [Fact]
        public void ResolveWaveIntervalMs_uses_min_when_random_range_collapsed()
        {
            SpawnScalingSceneConfig config = Config(
                periodicSpawnWaitMode: "Random",
                periodicSpawnIntervalMinSeconds: 22f,
                periodicSpawnIntervalMaxSeconds: 22f);

            int intervalMs = PeriodicSpawnWaitResolver.ResolveWaveIntervalMs(config);

            Assert.Equal(22_000, intervalMs);
        }
    }
}
