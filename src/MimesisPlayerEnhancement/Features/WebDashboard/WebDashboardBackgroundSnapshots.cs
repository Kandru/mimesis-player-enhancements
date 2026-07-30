using MimesisPlayerEnhancement.Features.Statistics.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal readonly struct OfflinePlayerRebuildSnapshot
    {
        internal WebDashboardPlayerService.OfflinePlayerBuildContext Context { get; }
        internal SlotStatisticsDocument Document { get; }
        internal HashSet<ulong> BannedSteamIds { get; }
        internal Dictionary<ulong, string> DisplayNames { get; }

        internal OfflinePlayerRebuildSnapshot(
            WebDashboardPlayerService.OfflinePlayerBuildContext context,
            SlotStatisticsDocument document,
            HashSet<ulong> bannedSteamIds,
            Dictionary<ulong, string> displayNames)
        {
            Context = context;
            Document = document;
            BannedSteamIds = bannedSteamIds;
            DisplayNames = displayNames;
        }
    }

    internal readonly struct LeaderboardRebuildSnapshot
    {
        internal int SaveSlotId { get; }
        internal SlotStatisticsDocument Document { get; }
        internal Dictionary<ulong, string> DisplayNames { get; }

        internal LeaderboardRebuildSnapshot(
            int saveSlotId,
            SlotStatisticsDocument document,
            Dictionary<ulong, string> displayNames)
        {
            SaveSlotId = saveSlotId;
            Document = document;
            DisplayNames = displayNames;
        }
    }

    internal readonly struct HistoryRebuildSnapshot
    {
        internal int SaveSlotId { get; }
        internal SlotStatisticsDocument Document { get; }
        internal Dictionary<ulong, string> DisplayNames { get; }

        internal HistoryRebuildSnapshot(
            int saveSlotId,
            SlotStatisticsDocument document,
            Dictionary<ulong, string> displayNames)
        {
            SaveSlotId = saveSlotId;
            Document = document;
            DisplayNames = displayNames;
        }
    }

    internal static class WebDashboardBackgroundSnapshots
    {
        internal static OfflinePlayerRebuildSnapshot CaptureOfflineRebuild()
        {
            WebDashboardPlayerService.OfflinePlayerBuildContext context =
                WebDashboardPlayerService.OfflinePlayerBuildContext.Capture();
            SlotStatisticsDocument document = StatisticsHistory.CloneDocument();

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

            Dictionary<ulong, string> displayNames = new(document.Globals.Count);
            foreach (KeyValuePair<ulong, PlayerGlobalStats> player in document.Globals)
            {
                displayNames[player.Key] = WebDashboardPlayerService.ResolveDisplayNameForSteamId(
                    player.Key,
                    context.SaveSlotId);
            }

            return new OfflinePlayerRebuildSnapshot(context, document, bannedSteamIds, displayNames);
        }

        internal static LeaderboardRebuildSnapshot CaptureLeaderboardRebuild(int saveSlotId)
        {
            SlotStatisticsDocument document = StatisticsHistory.CloneDocument();
            Dictionary<ulong, string> displayNames = new(document.Globals.Count);
            foreach (KeyValuePair<ulong, PlayerGlobalStats> player in document.Globals)
            {
                displayNames[player.Key] = WebDashboardPlayerService.ResolveDisplayNameForSteamId(
                    player.Key,
                    saveSlotId);
            }

            return new LeaderboardRebuildSnapshot(saveSlotId, document, displayNames);
        }

        internal static HistoryRebuildSnapshot CaptureHistoryRebuild(int saveSlotId)
        {
            SlotStatisticsDocument document = StatisticsHistory.CloneDocument();
            Dictionary<ulong, string> displayNames = new(document.Globals.Count);
            foreach (KeyValuePair<ulong, PlayerGlobalStats> player in document.Globals)
            {
                displayNames[player.Key] = WebDashboardPlayerService.ResolveDisplayNameForSteamId(
                    player.Key,
                    saveSlotId);
            }

            return new HistoryRebuildSnapshot(saveSlotId, document, displayNames);
        }
    }
}
