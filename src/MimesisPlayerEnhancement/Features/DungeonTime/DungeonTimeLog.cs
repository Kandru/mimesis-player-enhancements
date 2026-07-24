namespace MimesisPlayerEnhancement.Features.DungeonTime
{
    internal static class DungeonTimeLog
    {
        private const string Feature = "DungeonTime";

        internal static void InfoApplied(
            int playerCount,
            long bonusMs,
            long newSessionEndTime,
            long vanillaRemainingMs,
            DungeonTimeSceneConfig config)
        {
            double bonusSeconds = bonusMs / 1000d;
            double displayScale = DungeonTimeResolver.GetDisplayScale(vanillaRemainingMs, bonusMs);
            ModLog.Info(
                Feature,
                $"Shift extended — players={playerCount}, baseline={config.DungeonTimeBaselinePlayerCount}, " +
                $"+{bonusSeconds:0.##}s ({config.ExtraShiftSecondsPerPlayerAboveBaseline:0.##}s/player above baseline), " +
                $"displayScale={displayScale:0.####}, newSessionEndTime={newSessionEndTime}");
        }

        internal static void DebugSkipped(string reason)
        {
            ModLog.Debug(Feature, $"Shift extension skipped — {reason}");
        }
    }
}
