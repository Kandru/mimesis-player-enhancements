namespace MimesisPlayerEnhancement.Features.DungeonTime
{
    internal static class DungeonTimeLog
    {
        private const string Feature = "DungeonTime";

        internal static void InfoApplied(
            int playerCount,
            StartTimePreset preset,
            long baseRemainingMs,
            long bonusMs,
            long newSessionEndTime,
            long vanillaRemainingMs,
            DungeonTimeSceneConfig config)
        {
            double bonusSeconds = bonusMs / 1000d;
            double presetDeltaSeconds = (baseRemainingMs - vanillaRemainingMs) / 1000d;
            double displayScale = DungeonTimeResolver.GetDisplayScale(baseRemainingMs, bonusMs);
            ModLog.Info(
                Feature,
                $"Shift adjusted — players={playerCount}, baseline={config.DungeonTimeBaselinePlayerCount}, " +
                $"preset={preset} ({presetDeltaSeconds:+0.##;-0.##;0}s), " +
                $"+{bonusSeconds:0.##}s ({config.ExtraShiftSecondsPerPlayerAboveBaseline:0.##}s/player above baseline), " +
                $"displayScale={displayScale:0.####}, newSessionEndTime={newSessionEndTime}");
        }

        internal static void DebugSkipped(string reason)
        {
            ModLog.Debug(Feature, $"Shift adjustment skipped — {reason}");
        }
    }
}
