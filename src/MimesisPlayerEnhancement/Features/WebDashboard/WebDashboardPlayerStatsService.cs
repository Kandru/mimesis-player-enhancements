using MimesisPlayerEnhancement.Features.Statistics.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardPlayerStatsService
    {
        internal static PlayerStatisticsDocument? TryGetStats(ulong steamId)
        {
            return StatisticsTracker.TryGetPlayerDocument(steamId);
        }
    }
}
