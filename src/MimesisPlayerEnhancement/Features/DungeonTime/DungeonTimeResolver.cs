namespace MimesisPlayerEnhancement.Features.DungeonTime
{
    internal static class DungeonTimeResolver
    {
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
        /// Real-time display-clock scale so Start→end still spans the extended shift:
        /// vanillaRemaining / (vanillaRemaining + bonus).
        /// </summary>
        internal static double GetDisplayScale(long vanillaRemainingMs, long bonusMs)
        {
            if (vanillaRemainingMs <= 0 || bonusMs <= 0)
            {
                return 1d;
            }

            return (double)vanillaRemainingMs / (vanillaRemainingMs + bonusMs);
        }

        /// <summary>
        /// Scales one OnUpdate delta for <c>_elapsedTime</c> while <c>_currentTime</c> stays real-time.
        /// </summary>
        internal static long ScaleElapsedDelta(long deltaMs, long vanillaRemainingMs, long extendedRemainingMs)
        {
            if (deltaMs <= 0 || vanillaRemainingMs <= 0 || extendedRemainingMs <= vanillaRemainingMs)
            {
                return deltaMs;
            }

            return deltaMs * vanillaRemainingMs / extendedRemainingMs;
        }
    }
}
