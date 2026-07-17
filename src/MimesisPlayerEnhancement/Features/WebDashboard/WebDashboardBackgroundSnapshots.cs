using MimesisPlayerEnhancement.Features.Statistics;
using MimesisPlayerEnhancement.Features.Statistics.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal readonly struct OfflinePlayerRebuildSnapshot
    {
        internal WebDashboardPlayerService.OfflinePlayerBuildContext Context { get; }
        internal List<PlayerStatisticsDocument> Players { get; }
        internal HashSet<ulong> BannedSteamIds { get; }
        internal Dictionary<ulong, string> DisplayNames { get; }

        internal OfflinePlayerRebuildSnapshot(
            WebDashboardPlayerService.OfflinePlayerBuildContext context,
            List<PlayerStatisticsDocument> players,
            HashSet<ulong> bannedSteamIds,
            Dictionary<ulong, string> displayNames)
        {
            Context = context;
            Players = players;
            BannedSteamIds = bannedSteamIds;
            DisplayNames = displayNames;
        }
    }

    internal readonly struct LeaderboardRebuildSnapshot
    {
        internal int SaveSlotId { get; }
        internal int CurrentZone { get; }
        internal List<PlayerStatisticsDocument> Players { get; }
        internal Dictionary<ulong, string> DisplayNames { get; }

        internal LeaderboardRebuildSnapshot(
            int saveSlotId,
            int currentZone,
            List<PlayerStatisticsDocument> players,
            Dictionary<ulong, string> displayNames)
        {
            SaveSlotId = saveSlotId;
            CurrentZone = currentZone;
            Players = players;
            DisplayNames = displayNames;
        }
    }

    /// <summary>
    /// Captures immutable game-thread inputs for dashboard background rebuilds.
    /// </summary>
    internal static class WebDashboardBackgroundSnapshots
    {
        internal static OfflinePlayerRebuildSnapshot CaptureOfflineRebuild()
        {
            WebDashboardPlayerService.OfflinePlayerBuildContext context =
                WebDashboardPlayerService.OfflinePlayerBuildContext.Capture();
            List<PlayerStatisticsDocument> players =
                StatisticsStore.ClonePlayerDocuments(PlayerRegistry.GetAllStatistics());

            HashSet<ulong> bannedSteamIds = [];
            SessionManager? sessionManager = WebDashboardSessionAccess.GetSessionManager();
            if (sessionManager != null)
            {
                foreach (ulong steamId in WebDashboardSessionAccess.EnumerateBannedSteamIds(sessionManager))
                {
                    if (steamId != 0)
                    {
                        _ = bannedSteamIds.Add(steamId);
                    }
                }
            }

            Dictionary<ulong, string> displayNames = new(players.Count);
            if (context.SaveSlotId >= 0)
            {
                foreach (PlayerStatisticsDocument player in players)
                {
                    displayNames[player.SteamId] = SaveSlotDocumentStore.ResolveDisplayName(
                        context.SaveSlotId,
                        player.SteamId,
                        player.DisplayName);
                }
            }

            return new OfflinePlayerRebuildSnapshot(context, players, bannedSteamIds, displayNames);
        }

        internal static LeaderboardRebuildSnapshot CaptureLeaderboardRebuild(int saveSlotId)
        {
            List<PlayerStatisticsDocument> players =
                StatisticsStore.ClonePlayerDocuments(PlayerRegistry.GetAllStatistics());
            Dictionary<ulong, string> displayNames = new(players.Count);
            foreach (PlayerStatisticsDocument player in players)
            {
                displayNames[player.SteamId] = SaveSlotDocumentStore.ResolveDisplayName(
                    saveSlotId,
                    player.SteamId,
                    player.DisplayName);
            }

            return new LeaderboardRebuildSnapshot(
                saveSlotId,
                StatisticsRunTracker.GetCurrentZone(),
                players,
                displayNames);
        }
    }
}
