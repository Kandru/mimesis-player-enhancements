using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    /// <summary>
    /// Merges live and offline player DTO lists for dashboard snapshots,
    /// including incremental patching of a cached merge.
    /// </summary>
    internal static class WebDashboardPlayerListMerger
    {
        internal static List<WebDashboardPlayerDto> MergePlayerLists(
            IReadOnlyList<WebDashboardPlayerDto> live,
            IReadOnlyList<WebDashboardPlayerDto> offline)
        {
            if (offline.Count == 0)
            {
                return live.Count == 0 ? [] : [.. live];
            }

            Dictionary<ulong, WebDashboardPlayerDto> merged = [];
            foreach (WebDashboardPlayerDto player in offline)
            {
                if (player.SteamId != 0)
                {
                    merged[player.SteamId] = player;
                }
            }

            foreach (WebDashboardPlayerDto player in live)
            {
                if (player.SteamId != 0)
                {
                    merged[player.SteamId] = player;
                }
            }

            List<WebDashboardPlayerDto> players = [.. merged.Values];
            SortPlayers(players);
            return players;
        }

        internal static Dictionary<ulong, int> BuildMergedIndex(IReadOnlyList<WebDashboardPlayerDto> merged)
        {
            Dictionary<ulong, int> indexBySteam = new(merged.Count);
            for (int i = 0; i < merged.Count; i++)
            {
                ulong steamId = merged[i].SteamId;
                if (steamId != 0)
                {
                    indexBySteam[steamId] = i;
                }
            }

            return indexBySteam;
        }

        internal static bool ApplyLiveToMerged(
            List<WebDashboardPlayerDto> merged,
            Dictionary<ulong, int> indexBySteam,
            IReadOnlyList<WebDashboardPlayerDto> live,
            IReadOnlyList<WebDashboardPlayerDto> offline,
            IReadOnlyCollection<ulong> previousLiveSteamIds)
        {
            if (live.Count == 0 && previousLiveSteamIds.Count == 0)
            {
                return false;
            }

            HashSet<ulong> liveIds = new(live.Count);
            foreach (WebDashboardPlayerDto player in live)
            {
                if (player.SteamId != 0)
                {
                    _ = liveIds.Add(player.SteamId);
                }
            }

            bool sortNeeded = false;
            foreach (WebDashboardPlayerDto player in live)
            {
                if (player.SteamId == 0)
                {
                    continue;
                }

                if (indexBySteam.TryGetValue(player.SteamId, out int index))
                {
                    merged[index] = player;
                }
                else
                {
                    merged.Add(player);
                    indexBySteam[player.SteamId] = merged.Count - 1;
                    sortNeeded = true;
                }
            }

            Dictionary<ulong, WebDashboardPlayerDto>? offlineById = null;
            foreach (ulong steamId in previousLiveSteamIds)
            {
                if (steamId == 0 || liveIds.Contains(steamId))
                {
                    continue;
                }

                offlineById ??= BuildOfflineLookup(offline);
                if (!offlineById.TryGetValue(steamId, out WebDashboardPlayerDto? offlinePlayer))
                {
                    continue;
                }

                if (indexBySteam.TryGetValue(steamId, out int index))
                {
                    merged[index] = offlinePlayer;
                }
            }

            if (sortNeeded)
            {
                SortPlayers(merged);
                RebuildMergedIndex(merged, indexBySteam);
            }

            return true;
        }

        internal static void RebuildMergedIndex(
            IReadOnlyList<WebDashboardPlayerDto> merged,
            Dictionary<ulong, int> indexBySteam)
        {
            indexBySteam.Clear();
            for (int i = 0; i < merged.Count; i++)
            {
                ulong steamId = merged[i].SteamId;
                if (steamId != 0)
                {
                    indexBySteam[steamId] = i;
                }
            }
        }

        private static Dictionary<ulong, WebDashboardPlayerDto> BuildOfflineLookup(
            IReadOnlyList<WebDashboardPlayerDto> offline)
        {
            Dictionary<ulong, WebDashboardPlayerDto> lookup = new(offline.Count);
            foreach (WebDashboardPlayerDto player in offline)
            {
                if (player.SteamId != 0)
                {
                    lookup[player.SteamId] = player;
                }
            }

            return lookup;
        }

        internal static void SortPlayers(List<WebDashboardPlayerDto> players)
        {
            players.Sort((a, b) =>
            {
                int hostCmp = b.IsHost.CompareTo(a.IsHost);
                return hostCmp != 0 ? hostCmp : string.Compare(a.DisplayName, b.DisplayName, System.StringComparison.OrdinalIgnoreCase);
            });
        }
    }
}
