using MimesisPlayerEnhancement;
using MimesisPlayerEnhancement.Features.Persistence;
using MimesisPlayerEnhancement.Features.Persistence.Patches;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Persistence
{
    public sealed class SpeechEventArchivePatchesTests
    {
        [Fact]
        public void BuildConnectOutcome_returns_connected_when_events_restored_from_pool()
        {
            var result = new SpeechEventInjector.RestoreResult(fromPool: 1, fromReconnect: 0);

            PersistenceConnectOutcome outcome = SpeechEventArchivePatches.BuildConnectOutcome(
                result,
                hasPending: true,
                disconnectedCacheCount: 5);

            Assert.Equal(PersistenceConnectPhase.Connected, outcome.Phase);
            Assert.Null(outcome.Detail);
        }

        [Fact]
        public void BuildConnectOutcome_returns_connected_when_events_restored_from_reconnect()
        {
            var result = new SpeechEventInjector.RestoreResult(fromPool: 0, fromReconnect: 2);

            PersistenceConnectOutcome outcome = SpeechEventArchivePatches.BuildConnectOutcome(
                result,
                hasPending: false,
                disconnectedCacheCount: 0);

            Assert.Equal(PersistenceConnectPhase.Connected, outcome.Phase);
            Assert.Null(outcome.Detail);
        }

        [Theory]
        [InlineData(true, 0)]
        [InlineData(false, 3)]
        public void BuildConnectOutcome_reports_unmatched_when_pool_or_cache_remain(
            bool hasPending,
            int disconnectedCacheCount)
        {
            var result = new SpeechEventInjector.RestoreResult(fromPool: 0, fromReconnect: 0);

            PersistenceConnectOutcome outcome = SpeechEventArchivePatches.BuildConnectOutcome(
                result,
                hasPending,
                disconnectedCacheCount);

            Assert.Equal(PersistenceConnectPhase.Connected, outcome.Phase);
            Assert.Equal("unmatched saved voices", outcome.Detail);
        }

        [Fact]
        public void BuildConnectOutcome_returns_connected_without_detail_when_nothing_pending()
        {
            var result = new SpeechEventInjector.RestoreResult(fromPool: 0, fromReconnect: 0);

            PersistenceConnectOutcome outcome = SpeechEventArchivePatches.BuildConnectOutcome(
                result,
                hasPending: false,
                disconnectedCacheCount: 0);

            Assert.Equal(PersistenceConnectPhase.Connected, outcome.Phase);
            Assert.Null(outcome.Detail);
        }
    }
}
