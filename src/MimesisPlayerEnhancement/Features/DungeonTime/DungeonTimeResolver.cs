namespace MimesisPlayerEnhancement.Features.DungeonTime
{
    internal static class DungeonTimeResolver
    {
        private const long DaySeconds = 86400L;

        internal static double GetBonusSeconds(int playerCount, DungeonTimeSceneConfig config)
        {
            if (!config.EnableDungeonTime)
            {
                return 0d;
            }

            int baseline = config.DungeonTimeBaselinePlayerCount;
            if (playerCount <= baseline)
            {
                return 0d;
            }

            return (playerCount - baseline) * config.ExtraShiftSecondsPerPlayerAboveBaseline;
        }

        internal static long GetBonusMilliseconds(int playerCount, DungeonTimeSceneConfig config)
        {
            return (long)(GetBonusSeconds(playerCount, config) * 1000d);
        }

        /// <summary>
        /// In-game display span from start→end. Wraps by one day when end is at or before start
        /// (vanilla uses &lt; only; ≤ covers Midnight 00:00→00:00 as a full day).
        /// </summary>
        internal static long GetDisplaySpanSeconds(long startSeconds, long endSeconds)
        {
            long end = endSeconds;
            if (end <= startSeconds)
            {
                end += DaySeconds;
            }

            return end - startSeconds;
        }

        /// <summary>
        /// Real remaining ms sized so effectiveStart→end fills the same way vanillaStart→end did.
        /// </summary>
        internal static long GetPresetAdjustedRemainingMs(
            long vanillaRemainingMs,
            long vanillaStartSeconds,
            long effectiveStartSeconds,
            long endSeconds)
        {
            if (vanillaRemainingMs <= 0)
            {
                return vanillaRemainingMs;
            }

            long vanillaSpan = GetDisplaySpanSeconds(vanillaStartSeconds, endSeconds);
            long effectiveSpan = GetDisplaySpanSeconds(effectiveStartSeconds, endSeconds);
            if (vanillaSpan <= 0 || effectiveSpan <= 0 || vanillaSpan == effectiveSpan)
            {
                return vanillaRemainingMs;
            }

            return (long)(vanillaRemainingMs * (double)effectiveSpan / vanillaSpan);
        }

        /// <summary>
        /// Real-time display-clock scale so Start→end still spans the extended shift:
        /// baseRemaining / (baseRemaining + bonus).
        /// </summary>
        internal static double GetDisplayScale(long baseRemainingMs, long bonusMs)
        {
            if (baseRemainingMs <= 0 || bonusMs <= 0)
            {
                return 1d;
            }

            return (double)baseRemainingMs / (baseRemainingMs + bonusMs);
        }

        /// <summary>
        /// Scales one OnUpdate delta for <c>_elapsedTime</c> while <c>_currentTime</c> stays real-time.
        /// </summary>
        internal static long ScaleElapsedDelta(long deltaMs, long baseRemainingMs, long extendedRemainingMs)
        {
            if (deltaMs <= 0 || baseRemainingMs <= 0 || extendedRemainingMs <= baseRemainingMs)
            {
                return deltaMs;
            }

            return deltaMs * baseRemainingMs / extendedRemainingMs;
        }
    }
}
