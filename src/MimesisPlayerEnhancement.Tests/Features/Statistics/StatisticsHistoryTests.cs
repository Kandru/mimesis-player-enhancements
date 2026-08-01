using System.Linq;
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

            Assert.Empty(StatisticsHistory.Document.History.Zones);
            Assert.Equal(1, StatisticsHistory.Document.History.CurrentZone);
            Assert.Equal(1, StatisticsHistory.Document.Globals[50].Counters.Deaths);
            Assert.Equal(2, StatisticsHistory.Document.Globals[50].HighestZoneReached);
            Assert.Equal(1, StatisticsHistory.Document.Globals[50].RunRestarts);
        }

        [Fact]
        public void Load_does_not_create_placeholder_zone()
        {
            StatisticsHistory.Load(new SlotStatisticsDocument());

            Assert.Empty(StatisticsHistory.Document.History.Zones);
            Assert.Equal(1, StatisticsHistory.Document.History.CurrentZone);
            Assert.False(StatisticsHistory.HasHistoryData());
        }

        [Fact]
        public void SyncCurrentZone_adopts_game_zone_on_empty_document()
        {
            StatisticsHistory.Load(new SlotStatisticsDocument());

            Assert.True(StatisticsHistory.SyncCurrentZone(6));
            Assert.Equal(6, StatisticsHistory.CurrentZone);
            Assert.Empty(StatisticsHistory.Document.History.Zones);
        }

        [Fact]
        public void SyncCurrentZone_creates_only_target_zone_on_first_write()
        {
            StatisticsHistory.Load(new SlotStatisticsDocument());
            Assert.True(StatisticsHistory.SyncCurrentZone(6));

            StatisticsHistory.Apply(1UL, counters => counters.Deaths++, CounterScope.Zone);

            Assert.Single(StatisticsHistory.Document.History.Zones);
            Assert.Equal(6, StatisticsHistory.Document.History.Zones[0].Zone);
            Assert.Equal(1, StatisticsHistory.Document.History.Zones[0].Players[1].Deaths);
        }

        [Fact]
        public void SyncCurrentZone_fills_gap_on_existing_document()
        {
            StatisticsHistory.Load(new SlotStatisticsDocument());
            StatisticsHistory.Apply(1UL, counters => counters.Deaths++, CounterScope.Zone);
            StatisticsHistory.OnZoneAdvanced(2);

            Assert.True(StatisticsHistory.SyncCurrentZone(5));

            int[] zones = StatisticsHistory.Document.History.Zones.Select(z => z.Zone).ToArray();
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, zones);
            Assert.NotNull(StatisticsHistory.Document.History.Zones.First(z => z.Zone == 2).EndedAtUtc);
            Assert.NotNull(StatisticsHistory.Document.History.Zones.First(z => z.Zone == 3).EndedAtUtc);
            Assert.NotNull(StatisticsHistory.Document.History.Zones.First(z => z.Zone == 4).EndedAtUtc);
            Assert.Null(StatisticsHistory.Document.History.Zones.First(z => z.Zone == 5).EndedAtUtc);
        }

        [Fact]
        public void SyncCurrentZone_ignores_noop_and_invalid_input()
        {
            StatisticsHistory.Load(new SlotStatisticsDocument());
            StatisticsHistory.Apply(1UL, counters => counters.Deaths++, CounterScope.Zone);

            Assert.False(StatisticsHistory.SyncCurrentZone(0));
            Assert.False(StatisticsHistory.SyncCurrentZone(-1));
            Assert.False(StatisticsHistory.SyncCurrentZone(1));
            Assert.Single(StatisticsHistory.Document.History.Zones);
        }

        [Fact]
        public void OnZoneAdvanced_fills_small_gap()
        {
            StatisticsHistory.Load(new SlotStatisticsDocument());
            StatisticsHistory.Apply(1UL, counters => counters.Deaths++, CounterScope.Zone);

            StatisticsHistory.OnZoneAdvanced(4);

            int[] zones = StatisticsHistory.Document.History.Zones.Select(z => z.Zone).ToArray();
            Assert.Equal(new[] { 1, 2, 3, 4 }, zones);
            Assert.Equal(4, StatisticsHistory.CurrentZone);
        }

        [Fact]
        public void OnZoneAdvanced_skips_fill_for_large_jump()
        {
            StatisticsHistory.Load(new SlotStatisticsDocument());
            StatisticsHistory.Apply(1UL, counters => counters.Deaths++, CounterScope.Zone);

            StatisticsHistory.OnZoneAdvanced(30);

            int[] zones = StatisticsHistory.Document.History.Zones.Select(z => z.Zone).ToArray();
            Assert.Equal(new[] { 1, 30 }, zones);
            Assert.Equal(30, StatisticsHistory.CurrentZone);
        }

        [Fact]
        public void OnZoneAdvanced_backwards_reopens_target_zone()
        {
            StatisticsHistory.Load(new SlotStatisticsDocument());
            StatisticsHistory.Apply(1UL, counters => counters.Deaths++, CounterScope.Zone);
            StatisticsHistory.OnZoneAdvanced(3);

            StatisticsHistory.OnZoneAdvanced(2);

            Assert.Equal(2, StatisticsHistory.CurrentZone);
            Assert.Null(StatisticsHistory.Document.History.Zones.First(z => z.Zone == 2).EndedAtUtc);
        }

        [Fact]
        public void OnZoneAdvanced_files_runs_under_synced_zone()
        {
            StatisticsHistory.Load(new SlotStatisticsDocument());
            Assert.True(StatisticsHistory.SyncCurrentZone(7));

            StatisticsHistory.OpenRun(new DungeonRunIdentity(StatisticsHistory.CurrentZone, 1, 99, 10, 20));
            StatisticsHistory.CloseRun(DungeonRunOutcome.Success);

            Assert.Single(StatisticsHistory.Document.History.Zones);
            Assert.Equal(7, StatisticsHistory.Document.History.Zones[0].Zone);
            Assert.Single(StatisticsHistory.Document.History.Zones[0].Runs);
        }
    }
}
