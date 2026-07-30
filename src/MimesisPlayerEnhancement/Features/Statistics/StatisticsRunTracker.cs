namespace MimesisPlayerEnhancement.Features.Statistics
{
    internal static class StatisticsRunTracker
    {
        private static long _lastRestartMs;

        internal static int GetCurrentZone() => StatisticsHistory.CurrentZone;

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

            if (stageCount > 0 && stageCount != StatisticsHistory.CurrentZone)
            {
                StatisticsHistory.OnZoneAdvanced(stageCount);
                StatisticsCounterWriter.NotifyChanged();
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
            bool hadData = StatisticsHistory.HasHistoryData();
            StatisticsHistory.OnRunRestart();
            StatisticsDeathHandler.ClearDungeonState();
            TrainDepositTracker.ClearDungeonState();

            if (hadData)
            {
                StatisticsCounterWriter.NotifyChanged();
                StatisticsTracker.PersistLoadedSlot();
            }
        }

        internal static void ClearRuntimeState()
        {
            _lastRestartMs = 0;
        }
    }
}
