namespace MimesisPlayerEnhancement.Features.Statistics
{
    internal static class StatisticsPatchGuard
    {
        private const string Feature = "Statistics";

        internal static void Run(string context, Action action)
        {
            if (!StatisticsTracker.CanTrack())
            {
                return;
            }

            try
            {
                action();
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"{context} failed — {ex.Message}");
            }
        }
    }
}
