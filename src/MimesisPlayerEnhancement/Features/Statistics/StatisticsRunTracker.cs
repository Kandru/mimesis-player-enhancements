using MimesisPlayerEnhancement.Features.Statistics.Models;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    internal static class StatisticsRunTracker
    {
        private const string Feature = "Statistics";

        private static int _currentZone = 1;
        private static long _lastRestartMs;

        internal static int GetCurrentZone() => _currentZone > 0 ? _currentZone : 1;

        internal static void OnStageChanged(int stageCount, bool reset)
        {
            if (!StatisticsTracker.CanTrack())
            {
                return;
            }

            if (reset && stageCount <= 1)
            {
                OnRunRestart();
            }

            if (stageCount > 0)
            {
                _currentZone = stageCount;
            }
        }

        internal static void OnRunRestart()
        {
            if (!StatisticsTracker.CanTrack())
            {
                return;
            }

            long now = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
            if (now - _lastRestartMs < 1000)
            {
                return;
            }

            _lastRestartMs = now;
            _currentZone = 1;
            bool changed = false;

            foreach (PlayerStatisticsDocument doc in PlayerRegistry.GetAllStatistics())
            {
                if (doc.CurrentRun.Zones.Count > 0 || doc.CurrentRun.Counters.HasAnyRunData())
                {
                    doc.Global.RunRestarts++;
                    changed = true;
                }

                doc.CurrentRun = new RunStats
                {
                    StartedAtUtc = DateTime.UtcNow,
                };
            }

            StatisticsDeathHandler.ClearDungeonState();
            TrainDepositTracker.ClearDungeonState();

            if (changed)
            {
                StatisticsCounterWriter.NotifyChanged();
                StatisticsTracker.PersistLoadedSlot();
                ModLog.Info(Feature, "Run statistics reset — zone restart recorded.");
            }
        }

        internal static void ClearRuntimeState()
        {
            _currentZone = 1;
            _lastRestartMs = 0;
        }
    }
}
