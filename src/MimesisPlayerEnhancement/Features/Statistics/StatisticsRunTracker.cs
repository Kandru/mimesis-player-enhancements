namespace MimesisPlayerEnhancement.Features.Statistics
{
    internal static class StatisticsRunTracker
    {
        private const string Feature = "Statistics";
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

        internal static void SyncZoneFromGameSession()
        {
            if (!StatisticsTracker.CanTrack())
            {
                return;
            }

            int stageCount = GameSessionAccess.TryGetGameSessionInfo()?.StageCount ?? 0;
            if (stageCount <= 0)
            {
                return;
            }

            if (StatisticsHistory.SyncCurrentZone(stageCount))
            {
                ModLog.Debug(Feature, $"Zone synced from game session — zone={stageCount}.");
                StatisticsCounterWriter.NotifyChanged();
            }
        }

        internal static void OnRunRestart()
        {
            if (!StatisticsTracker.CanTrack())
            {
                return;
            }

            int sessionSlotId = GameSessionAccess.GetSaveSlotId();
            if (sessionSlotId >= 0
                && PlayerRegistry.LoadedSlotId >= 0
                && sessionSlotId != PlayerRegistry.LoadedSlotId)
            {
                ModLog.Debug(
                    Feature,
                    $"Ignoring run restart during slot transition — loaded={PlayerRegistry.LoadedSlotId}, session={sessionSlotId}.");
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
