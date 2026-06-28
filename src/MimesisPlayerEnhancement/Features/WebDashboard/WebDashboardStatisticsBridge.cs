using System;
using System.Collections.Generic;
using MimesisPlayerEnhancement.Features.Statistics.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardStatisticsBridge
    {
        internal static string? BuildLeaderboardJson(int saveSlotId, IReadOnlyCollection<ulong> connectedSteamIds)
        {
            LeaderboardDocument? leaderboard = GetLeaderboardDocument(saveSlotId);
            return leaderboard == null ? null : WebDashboardJson.SerializeLeaderboardResponse(leaderboard, connectedSteamIds);
        }

        internal static LeaderboardDocument? GetLeaderboardDocument(int saveSlotId)
        {
            if (!WebDashboardGameState.IsHost() || saveSlotId < 0)
            {
                return null;
            }

            LeaderboardDocument? leaderboard = TryBuildLiveLeaderboard(saveSlotId);
            return leaderboard ?? TryLoadLeaderboardFromDisk(saveSlotId);
        }

        internal static string? TryGetDisplayNameFromLeaderboard(LeaderboardDocument? leaderboard, ulong steamId)
        {
            if (leaderboard?.Entries == null || steamId == 0)
            {
                return null;
            }

            string steamIdText = steamId.ToString();
            foreach (LeaderboardEntry entry in leaderboard.Entries)
            {
                if (entry.SteamId != steamId || string.IsNullOrWhiteSpace(entry.DisplayName))
                {
                    continue;
                }

                if (string.Equals(entry.DisplayName, steamIdText, StringComparison.Ordinal))
                {
                    continue;
                }

                return entry.DisplayName;
            }

            return null;
        }

        internal static string? BuildPlayerStatsJson(int saveSlotId, ulong steamId, LeaderboardDocument? leaderboard = null)
        {
            if (!WebDashboardGameState.IsHost() || steamId == 0 || saveSlotId < 0)
            {
                return null;
            }

            PlayerStatisticsDocument doc = StatisticsTracker.TryGetPlayerDocument(steamId) is PlayerStatisticsDocument live
                ? live
                : StatisticsStore.LoadPlayer(saveSlotId, steamId);

            leaderboard ??= GetLeaderboardDocument(saveSlotId);
            string? displayName = TryGetDisplayNameFromLeaderboard(leaderboard, steamId)
                ?? WebDashboardPlayerService.ResolveDisplayNameForSteamId(steamId, saveSlotId);
            if (!string.IsNullOrWhiteSpace(displayName) && !string.Equals(displayName, steamId.ToString(), StringComparison.Ordinal))
            {
                doc.DisplayName = displayName;
            }

            return StatisticsJson.SerializePlayer(doc);
        }

        internal static List<ulong> GetConnectedSteamIds()
        {
            return !ModConfig.EnableStatistics.Value ? CollectSteamIdsFromPlayers() : [.. StatisticsTracker.GetConnectedSteamIds()];
        }

        private static List<ulong> CollectSteamIdsFromPlayers()
        {
            List<ulong> ids = [];
            foreach (Models.WebDashboardPlayerDto player in WebDashboardPlayerService.CollectPlayers())
            {
                if (player.SteamId != 0)
                {
                    ids.Add(player.SteamId);
                }
            }

            return ids;
        }

        private static LeaderboardDocument? TryBuildLiveLeaderboard(int saveSlotId)
        {
            if (!ModConfig.EnableStatistics.Value)
            {
                return null;
            }

            List<PlayerStatisticsDocument> players = [.. StatisticsTracker.GetCachedPlayerDocuments()];
            return players.Count == 0 ? null : LeaderboardBuilder.Build(saveSlotId, players);
        }

        private static LeaderboardDocument? TryLoadLeaderboardFromDisk(int saveSlotId)
        {
            string? path = StatisticsStore.GetLeaderboardFilePath(saveSlotId);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            string? json = StatisticsStore.SafeReadText(path);
            if (string.IsNullOrEmpty(json))
            {
                Dictionary<ulong, PlayerStatisticsDocument> players = [];
                StatisticsStore.LoadAllPlayersForSlot(saveSlotId, players);
                return players.Count == 0 ? null : LeaderboardBuilder.Build(saveSlotId, players.Values);
            }

            return StatisticsJson.DeserializeLeaderboard(json);
        }
    }
}
