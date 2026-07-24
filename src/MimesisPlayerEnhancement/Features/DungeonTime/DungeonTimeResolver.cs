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
    }
}
