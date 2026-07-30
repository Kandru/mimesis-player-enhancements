using MimesisPlayerEnhancement.Features.Statistics;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Statistics
{
    public sealed class StatisticsHistoryTests
    {
        [Fact]
        public void OpenRun_and_CloseRun_track_players()
        {
            StatisticsHistory.Load(new SlotStatisticsDocument());

            StatisticsHistory.OpenRun(new DungeonRunIdentity(1, 3, 42, 100, 200));
            StatisticsHistory.Apply(100UL, counters => counters.ItemsDeposited++, CounterScope.All);
            StatisticsHistory.CloseRun(DungeonRunOutcome.Success);

            SlotStatisticsDocument document = StatisticsHistory.Document;
            Assert.Single(document.History.Zones[0].Runs);
            Assert.Equal(DungeonRunOutcome.Success, document.History.Zones[0].Runs[0].Outcome);
            Assert.Equal(1, document.Globals[100].Counters.ItemsDeposited);
            Assert.Equal(1, document.Globals[100].DungeonRunsPlayed);
        }

        [Fact]
        public void OnRunRestart_wipes_history_but_keeps_globals()
        {
            StatisticsHistory.Load(new SlotStatisticsDocument());
            StatisticsHistory.OpenRun(new DungeonRunIdentity(2, 1, 7, 10, 20));
            StatisticsHistory.Apply(50UL, counters => counters.Deaths++, CounterScope.All);
            StatisticsHistory.OnZoneAdvanced(2);
            StatisticsHistory.EnsureGlobal(50).HighestZoneReached = 2;

            StatisticsHistory.OnRunRestart();

            Assert.Single(StatisticsHistory.Document.History.Zones);
            Assert.Equal(1, StatisticsHistory.Document.History.CurrentZone);
            Assert.Equal(1, StatisticsHistory.Document.Globals[50].Counters.Deaths);
            Assert.Equal(2, StatisticsHistory.Document.Globals[50].HighestZoneReached);
            Assert.Equal(1, StatisticsHistory.Document.Globals[50].RunRestarts);
        }
    }
}
