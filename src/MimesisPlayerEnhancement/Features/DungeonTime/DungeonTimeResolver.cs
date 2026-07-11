namespace MimesisPlayerEnhancement.Features.DungeonTime
{
    internal static class DungeonTimeResolver
    {
        internal static double GetBonusSeconds(int playerCount)
        {
            return GetBonusSeconds(playerCount, SceneScopedConfigGate.DungeonTime);
        }

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

        internal static long GetBonusMilliseconds(int playerCount)
        {
            return (long)(GetBonusSeconds(playerCount) * 1000d);
        }
    }
}
