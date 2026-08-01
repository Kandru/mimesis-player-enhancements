using MimesisPlayerEnhancement.Features.SpawnScaling;
using MimesisPlayerEnhancement.Util;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.SpawnScaling
{
    public sealed class AmbientWaveTimingTests
    {
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
            var expected = (AmbientWaveMode)expectedValue;
            Assert.Equal(expected, AmbientWaveTiming.ParseMode(value));
        }

        [Theory]
        [InlineData(false, "Fixed", false)]
        [InlineData(true, "Vanilla", false)]
        [InlineData(true, "Fixed", true)]
        [InlineData(true, "Random", true)]
        public void IsGruntWaitActive_requires_enabled_non_vanilla_mode(
            bool enabled,
            string mode,
            bool expected)
        {
            SpawnScalingSceneConfig config = SpawnScalingSceneConfigTestFactory.Create(
                enableSpawnScaling: enabled,
                gruntWaveMode: mode);

            Assert.Equal(expected, AmbientWaveTiming.IsGruntWaitActive(config));
        }

        [Theory]
        [InlineData(false, "Fixed", false)]
        [InlineData(true, "Vanilla", false)]
        [InlineData(true, "Fixed", true)]
        [InlineData(true, "Random", true)]
        public void IsMimicWaitActive_requires_enabled_non_vanilla_mode(
            bool enabled,
            string mode,
            bool expected)
        {
            SpawnScalingSceneConfig config = SpawnScalingSceneConfigTestFactory.Create(
                enableSpawnScaling: enabled,
                mimicWaveMode: mode);

            Assert.Equal(expected, AmbientWaveTiming.IsMimicWaitActive(config));
        }

        [Fact]
        public void IsGruntWaitActive_ignores_mimic_mode()
        {
            SpawnScalingSceneConfig config = SpawnScalingSceneConfigTestFactory.Create(
                gruntWaveMode: "Vanilla",
                mimicWaveMode: "Fixed");

            Assert.False(AmbientWaveTiming.IsGruntWaitActive(config));
            Assert.True(AmbientWaveTiming.IsMimicWaitActive(config));
        }

        [Fact]
        public void ResolveGruntInitialWaitSeconds_uses_fixed_value_in_fixed_mode()
        {
            SpawnScalingSceneConfig config = SpawnScalingSceneConfigTestFactory.Create(
                gruntWaveMode: "Fixed",
                gruntWaveInitialDelaySeconds: 42f);

            float seconds = AmbientWaveTiming.ResolveGruntInitialWaitSeconds(config);

            Assert.Equal(42f, seconds);
        }

        [Fact]
        public void ResolveMimicInitialWaitSeconds_uses_own_fixed_value()
        {
            SpawnScalingSceneConfig config = SpawnScalingSceneConfigTestFactory.Create(
                mimicWaveMode: "Fixed",
                mimicWaveInitialDelaySeconds: 15f,
                gruntWaveMode: "Fixed",
                gruntWaveInitialDelaySeconds: 42f);

            Assert.Equal(15f, AmbientWaveTiming.ResolveMimicInitialWaitSeconds(config));
            Assert.Equal(42f, AmbientWaveTiming.ResolveGruntInitialWaitSeconds(config));
        }

        [Fact]
        public void ResolveGruntInitialWaitSeconds_returns_zero_in_vanilla_mode()
        {
            SpawnScalingSceneConfig config = SpawnScalingSceneConfigTestFactory.Create(
                gruntWaveMode: "Vanilla",
                gruntWaveInitialDelaySeconds: 42f);

            float seconds = AmbientWaveTiming.ResolveGruntInitialWaitSeconds(config);

            Assert.Equal(0f, seconds);
        }

        [Fact]
        public void ResolveGruntInitialWaitSeconds_uses_min_when_random_range_collapsed()
        {
            SpawnScalingSceneConfig config = SpawnScalingSceneConfigTestFactory.Create(
                gruntWaveMode: "Random",
                gruntWaveInitialDelayMinSeconds: 25f,
                gruntWaveInitialDelayMaxSeconds: 25f);

            float seconds = AmbientWaveTiming.ResolveGruntInitialWaitSeconds(config);

            Assert.Equal(25f, seconds);
        }

        [Theory]
        [InlineData(30f, 30_000)]
        [InlineData(0f, 0)]
        [InlineData(-1f, 0)]
        [InlineData(0.0005f, 1)]
        public void ResolveGruntWaveIntervalMs_converts_fixed_seconds_to_milliseconds(float seconds, int expectedMs)
        {
            SpawnScalingSceneConfig config = SpawnScalingSceneConfigTestFactory.Create(
                gruntWaveMode: "Fixed",
                gruntWaveIntervalSeconds: seconds);

            int intervalMs = AmbientWaveTiming.ResolveGruntWaveIntervalMs(config);

            Assert.Equal(expectedMs, intervalMs);
        }

        [Fact]
        public void ResolveGruntWaveIntervalMs_returns_zero_in_vanilla_mode()
        {
            SpawnScalingSceneConfig config = SpawnScalingSceneConfigTestFactory.Create(
                gruntWaveMode: "Vanilla",
                gruntWaveIntervalSeconds: 30f);

            int intervalMs = AmbientWaveTiming.ResolveGruntWaveIntervalMs(config);

            Assert.Equal(0, intervalMs);
        }

        [Fact]
        public void ResolveMimicWaveIntervalMs_uses_min_when_random_range_collapsed()
        {
            SpawnScalingSceneConfig config = SpawnScalingSceneConfigTestFactory.Create(
                mimicWaveMode: "Random",
                mimicWaveIntervalMinSeconds: 22f,
                mimicWaveIntervalMaxSeconds: 22f);

            int intervalMs = AmbientWaveTiming.ResolveMimicWaveIntervalMs(config);

            Assert.Equal(22_000, intervalMs);
        }
    }
}
