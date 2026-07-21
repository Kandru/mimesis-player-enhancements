using MimesisPlayerEnhancement.Features.MimicTuning.MimicPossession;
using MimesisPlayerEnhancement.Tests.Infrastructure;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.MimicTuning
{
    [Collection(nameof(MimicPossessionSessionsCollection))]
    public sealed class MimicPossessionResolverTests : IDisposable
    {
        public MimicPossessionResolverTests()
        {
            MimicPossessionSessions.ClearAll();
        }

        public void Dispose()
        {
            MimicPossessionSessions.ClearAll();
        }

        [Theory]
        [InlineData(1f, true)]
        [InlineData(0.999f, true)]
        [InlineData(1.0005f, true)]
        [InlineData(1.002f, false)]
        [InlineData(2f, false)]
        [InlineData(0.5f, false)]
        public void IsVanillaMultiplier_detects_near_one(float multiplier, bool expected)
        {
            Assert.Equal(expected, MimicPossessionResolver.IsVanillaMultiplier(multiplier));
        }

        [Theory]
        [InlineData(5000, true, true, 2f, 10_000)]
        [InlineData(5000, true, false, 2f, 5000)]
        [InlineData(5000, false, true, 2f, 5000)]
        [InlineData(0, true, true, 2f, 0)]
        public void ScalePossessionCooltimeMs_scales_only_when_enabled_and_should_scale(
            long vanillaMs,
            bool enabled,
            bool shouldScale,
            float multiplier,
            long expected)
        {
            long result = MimicPossessionResolver.ScalePossessionCooltimeMs(
                vanillaMs,
                enabled,
                shouldScale,
                multiplier);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void RollPossessionDurationMs_returns_vanilla_when_randomize_disabled()
        {
            long result = MimicPossessionResolver.RollPossessionDurationMs(
                vanillaMs: 12_000,
                mimicActorId: 42,
                enabled: true,
                randomize: false,
                minDurationSeconds: 5f,
                maxDurationSeconds: 20f);

            Assert.Equal(12_000, result);
            Assert.False(MimicPossessionSessions.TryGetSessionDurationMs(42, out _));
        }

        [Fact]
        public void RollPossessionDurationMs_writes_session_when_min_equals_max()
        {
            long result = MimicPossessionResolver.RollPossessionDurationMs(
                vanillaMs: 12_000,
                mimicActorId: 7,
                enabled: true,
                randomize: true,
                minDurationSeconds: 8f,
                maxDurationSeconds: 8f);

            Assert.Equal(8000, result);
            Assert.True(MimicPossessionSessions.TryGetSessionDurationMs(7, out long sessionMs));
            Assert.Equal(8000, sessionMs);
        }

        [Fact]
        public void GetProgressBarTotalSeconds_returns_vanilla_when_randomize_disabled()
        {
            float result = MimicPossessionResolver.GetProgressBarTotalSeconds(
                mimicActorId: 1,
                serverLeftTimeMs: 5000f,
                shouldRandomize: false,
                vanillaSeconds: 12f);

            Assert.Equal(12f, result);
        }

        [Fact]
        public void GetProgressBarTotalSeconds_uses_session_duration_when_present()
        {
            MimicPossessionSessions.SetSessionDurationMs(3, 9000);

            float result = MimicPossessionResolver.GetProgressBarTotalSeconds(
                mimicActorId: 3,
                serverLeftTimeMs: 0f,
                shouldRandomize: true,
                vanillaSeconds: 12f);

            Assert.Equal(9f, result);
        }

        [Fact]
        public void GetProgressBarTotalSeconds_falls_back_to_server_left_time()
        {
            float result = MimicPossessionResolver.GetProgressBarTotalSeconds(
                mimicActorId: 5,
                serverLeftTimeMs: 6500f,
                shouldRandomize: true,
                vanillaSeconds: 12f);

            Assert.Equal(6.5f, result, precision: 3);
            Assert.True(MimicPossessionSessions.TryGetSessionDurationMs(5, out long sessionMs));
            Assert.Equal(6500, sessionMs);
        }
    }
}
