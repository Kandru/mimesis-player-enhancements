using MimesisPlayerEnhancement.Features.WebDashboard;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.WebDashboard
{
    public sealed class WebDashboardPendingKickSchedulerTests
    {
        [Fact]
        public void CollectDueSessionIds_returns_only_sessions_past_due_tick()
        {
            IReadOnlyDictionary<long, long> pending = new Dictionary<long, long>
            {
                [1] = 100,
                [2] = 200,
                [3] = 150,
            };

            List<long> due = WebDashboardPendingKickScheduler
                .CollectDueSessionIds(pending, 175)
                .OrderBy(sessionId => sessionId)
                .ToList();

            Assert.Equal([1L, 3L], due);
        }

        [Fact]
        public void CollectDueSessionIds_returns_empty_when_nothing_due()
        {
            IReadOnlyDictionary<long, long> pending = new Dictionary<long, long>
            {
                [1] = 500,
            };

            Assert.Empty(WebDashboardPendingKickScheduler.CollectDueSessionIds(pending, 100));
        }

        [Fact]
        public void CollectDueSessionIds_includes_sessions_exactly_at_due_tick()
        {
            IReadOnlyDictionary<long, long> pending = new Dictionary<long, long>
            {
                [42] = 1000,
            };

            Assert.Equal([42L], WebDashboardPendingKickScheduler.CollectDueSessionIds(pending, 1000).ToList());
        }

        [Fact]
        public void ForceRemoveDelayMs_matches_vanilla_kick_delay()
        {
            Assert.Equal(5000L, WebDashboardPendingKickScheduler.ForceRemoveDelayMs);
        }
    }
}
