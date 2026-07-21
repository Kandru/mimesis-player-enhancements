using MimesisPlayerEnhancement.Features.MimicTuning.MimicPossession;
using MimesisPlayerEnhancement.Tests.Infrastructure;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.MimicTuning
{
    [Collection(nameof(MimicPossessionSessionsCollection))]
    public sealed class MimicPossessionSessionsTests : IDisposable
    {
        public MimicPossessionSessionsTests()
        {
            MimicPossessionSessions.ClearAll();
        }

        public void Dispose()
        {
            MimicPossessionSessions.ClearAll();
        }

        [Fact]
        public void SetSessionDurationMs_stores_positive_duration_for_valid_actor()
        {
            MimicPossessionSessions.SetSessionDurationMs(10, 5000);

            Assert.True(MimicPossessionSessions.TryGetSessionDurationMs(10, out long durationMs));
            Assert.Equal(5000, durationMs);
        }

        [Theory]
        [InlineData(0, 5000)]
        [InlineData(-1, 5000)]
        [InlineData(10, 0)]
        [InlineData(10, -100)]
        public void SetSessionDurationMs_ignores_invalid_inputs(int mimicActorId, long durationMs)
        {
            MimicPossessionSessions.SetSessionDurationMs(mimicActorId, durationMs);

            Assert.False(MimicPossessionSessions.TryGetSessionDurationMs(mimicActorId, out _));
        }

        [Fact]
        public void ClearSession_removes_stored_duration()
        {
            MimicPossessionSessions.SetSessionDurationMs(20, 8000);

            MimicPossessionSessions.ClearSession(20);

            Assert.False(MimicPossessionSessions.TryGetSessionDurationMs(20, out _));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void ClearSession_ignores_invalid_actor_id(int mimicActorId)
        {
            MimicPossessionSessions.SetSessionDurationMs(20, 8000);

            MimicPossessionSessions.ClearSession(mimicActorId);

            Assert.True(MimicPossessionSessions.TryGetSessionDurationMs(20, out _));
        }

        [Fact]
        public void ClearAll_removes_all_sessions()
        {
            MimicPossessionSessions.SetSessionDurationMs(1, 1000);
            MimicPossessionSessions.SetSessionDurationMs(2, 2000);

            MimicPossessionSessions.ClearAll();

            Assert.False(MimicPossessionSessions.TryGetSessionDurationMs(1, out _));
            Assert.False(MimicPossessionSessions.TryGetSessionDurationMs(2, out _));
        }
    }
}
