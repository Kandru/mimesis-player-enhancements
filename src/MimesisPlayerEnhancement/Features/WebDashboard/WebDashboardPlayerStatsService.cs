using MimesisPlayerEnhancement.Features.Statistics.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardPlayerStatsService
    {
        internal static (PlayerStatisticsDocument? Document, string? DisplayName) TryGetStats(ulong steamId, int slotId)
        {
            if (StatisticsTracker.TryGetPlayerDocument(steamId) is not PlayerStatisticsDocument doc)
            {
                return (null, null);
            }

            string? displayName = WebDashboardPlayerService.ResolveDisplayNameForSteamId(steamId, slotId);
            return (doc, displayName);
        }
    }
}
