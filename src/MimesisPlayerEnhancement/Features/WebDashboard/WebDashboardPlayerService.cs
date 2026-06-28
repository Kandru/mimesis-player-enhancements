using System.Collections.Generic;
using System.Reflection;
using Mimic.Actors;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardPlayerService
    {
        internal static List<WebDashboardPlayerDto> CollectPlayers()
        {
            Dictionary<ulong, WebDashboardPlayerDto> playersBySteam = [];
            SessionManager? sessionManager = WebDashboardSessionAccess.GetSessionManager();
            ulong localSteamId = LocalPlayerHelper.TryGetLocalSteamId();
            Dictionary<ulong, string>? nameCache = TryGetSteamNameCache();
            bool dashboardIsHost = WebDashboardGameState.IsHost();

            if (sessionManager != null)
            {
                foreach (SessionContext context in WebDashboardSessionAccess.EnumerateSessionContexts(sessionManager))
                {
                    WebDashboardPlayerDto? dto = TryBuildPlayerDto(context, sessionManager, localSteamId, nameCache, dashboardIsHost);
                    if (dto != null)
                    {
                        playersBySteam[dto.SteamId] = dto;
                    }
                }
            }

            if (ModConfig.EnableStatistics.Value)
            {
                foreach (ulong steamId in StatisticsTracker.GetConnectedSteamIds())
                {
                    if (steamId == 0 || playersBySteam.ContainsKey(steamId))
                    {
                        continue;
                    }

                    WebDashboardPlayerDto? fallback = BuildFallbackPlayerDto(
                        steamId,
                        sessionManager,
                        localSteamId,
                        nameCache,
                        dashboardIsHost);
                    if (fallback != null)
                    {
                        playersBySteam[steamId] = fallback;
                    }
                }
            }

            if (dashboardIsHost && localSteamId != 0 && !playersBySteam.ContainsKey(localSteamId))
            {
                WebDashboardPlayerDto? hostFallback = BuildFallbackPlayerDto(
                    localSteamId,
                    sessionManager,
                    localSteamId,
                    nameCache,
                    dashboardIsHost,
                    forceHost: true);
                if (hostFallback != null)
                {
                    playersBySteam[localSteamId] = hostFallback;
                }
            }

            List<WebDashboardPlayerDto> players = [.. playersBySteam.Values];
            players.Sort((a, b) =>
            {
                int hostCmp = b.IsHost.CompareTo(a.IsHost);
                return hostCmp != 0 ? hostCmp : string.Compare(a.DisplayName, b.DisplayName, System.StringComparison.OrdinalIgnoreCase);
            });

            return players;
        }

        internal static bool TryFindPlayer(ulong steamId, out WebDashboardPlayerDto? player)
        {
            player = null;
            if (steamId == 0)
            {
                return false;
            }

            foreach (WebDashboardPlayerDto candidate in CollectPlayers())
            {
                if (candidate.SteamId == steamId)
                {
                    player = candidate;
                    return true;
                }
            }

            return false;
        }

        internal static string ResolveDisplayNameForSteamId(ulong steamId, int saveSlotId = -1)
        {
            if (steamId == 0)
            {
                return "";
            }

            if (TryFindPlayer(steamId, out WebDashboardPlayerDto? connected)
                && connected != null
                && !string.IsNullOrWhiteSpace(connected.DisplayName)
                && connected.DisplayName != steamId.ToString())
            {
                return connected.DisplayName;
            }

            if (StatisticsTracker.TryGetPlayerDocument(steamId) is PlayerStatisticsDocument live
                && !string.IsNullOrWhiteSpace(live.DisplayName)
                && live.DisplayName != steamId.ToString())
            {
                return live.DisplayName;
            }

            if (saveSlotId >= 0)
            {
                PlayerStatisticsDocument stored = StatisticsStore.LoadPlayer(saveSlotId, steamId);
                if (!string.IsNullOrWhiteSpace(stored.DisplayName) && stored.DisplayName != steamId.ToString())
                {
                    return stored.DisplayName;
                }
            }

            string? fromLeaderboard = TryGetLeaderboardDisplayName(saveSlotId, steamId);
            if (!string.IsNullOrWhiteSpace(fromLeaderboard))
            {
                return fromLeaderboard;
            }

            Dictionary<ulong, string>? nameCache = TryGetSteamNameCache();
            if (nameCache != null
                && nameCache.TryGetValue(steamId, out string? cached)
                && !string.IsNullOrWhiteSpace(cached))
            {
                return cached;
            }

            string? fromActor = ResolveNickNameFromActorMap(0, steamId);
            if (!string.IsNullOrWhiteSpace(fromActor))
            {
                return fromActor;
            }

            string? localNick = TryGetLocalNickName();
            return localNick != null && LocalPlayerHelper.IsLocalSteamId(steamId) ? localNick : steamId.ToString();
        }

        private static WebDashboardPlayerDto? BuildFallbackPlayerDto(
            ulong steamId,
            SessionManager? sessionManager,
            ulong localSteamId,
            Dictionary<ulong, string>? nameCache,
            bool dashboardIsHost,
            bool forceHost = false)
        {
            long playerUid = 0;
            if (sessionManager != null)
            {
                foreach (SessionContext context in WebDashboardSessionAccess.EnumerateSessionContexts(sessionManager))
                {
                    if (context.SteamID != steamId)
                    {
                        continue;
                    }

                    try
                    {
                        playerUid = context.GetPlayerUID();
                    }
                    catch
                    {
                        /* player may still be spawning */
                    }

                    break;
                }
            }

            bool isLocal = localSteamId != 0 && steamId == localSteamId;
            bool isHost = forceHost || (dashboardIsHost && isLocal);

            WebDashboardPlayerDto dto = new()
            {
                SteamId = steamId,
                PlayerUid = playerUid,
                DisplayName = ResolveDisplayName(null, steamId, playerUid, nameCache),
                IsHost = isHost,
                IsLocal = isLocal,
                IsBanned = sessionManager != null && WebDashboardSessionAccess.IsBanned(sessionManager, steamId),
            };

            if (sessionManager != null
                && playerUid != 0
                && (WebDashboardSessionAccess.TryGetNetworkGrade(sessionManager, playerUid, out int grade)
                    || WebDashboardPatches.TryGetCachedGrade(playerUid, out grade)))
            {
                dto.NetworkGrade = grade;
            }

            return dto;
        }

        private static WebDashboardPlayerDto? TryBuildPlayerDto(
            SessionContext context,
            SessionManager sessionManager,
            ulong localSteamId,
            Dictionary<ulong, string>? nameCache,
            bool dashboardIsHost)
        {
            try
            {
                ulong steamId = context.SteamID;
                if (steamId == 0 && dashboardIsHost && LocalPlayerHelper.IsLocalSteamId(localSteamId))
                {
                    steamId = localSteamId;
                }

                if (steamId == 0)
                {
                    return null;
                }

                long playerUid = 0;
                try
                {
                    playerUid = context.GetPlayerUID();
                }
                catch
                {
                    /* player may still be spawning */
                }

                VPlayer? vPlayer = WebDashboardSessionAccess.GetVPlayer(context);
                bool isHost = vPlayer?.IsHost ?? false;
                if (!isHost)
                {
                    if (WebDashboardSessionAccess.IsHostSessionContext(sessionManager, context))
                    {
                        isHost = true;
                    }
                    else if (dashboardIsHost && localSteamId != 0 && steamId == localSteamId)
                    {
                        isHost = true;
                    }
                }

                WebDashboardPlayerDto dto = new()
                {
                    SteamId = steamId,
                    PlayerUid = playerUid,
                    DisplayName = ResolveDisplayName(context, steamId, playerUid, nameCache),
                    IsHost = isHost,
                    IsLocal = localSteamId != 0 && steamId == localSteamId,
                    IsBanned = WebDashboardSessionAccess.IsBanned(sessionManager, steamId),
                };

                if (playerUid != 0
                    && (WebDashboardSessionAccess.TryGetNetworkGrade(sessionManager, playerUid, out int grade)
                        || WebDashboardPatches.TryGetCachedGrade(playerUid, out grade)))
                {
                    dto.NetworkGrade = grade;
                }

                return dto;
            }
            catch
            {
                return null;
            }
        }

        private static string ResolveDisplayName(
            SessionContext? context,
            ulong steamId,
            long playerUid,
            Dictionary<ulong, string>? nameCache)
        {
            if (!string.IsNullOrWhiteSpace(context?.NickName))
            {
                return context.NickName;
            }

            if (nameCache != null && nameCache.TryGetValue(steamId, out string? cached) && !string.IsNullOrWhiteSpace(cached))
            {
                return cached;
            }

            string? fromActor = ResolveNickNameFromActorMap(playerUid, steamId);
            if (!string.IsNullOrWhiteSpace(fromActor))
            {
                return fromActor;
            }

            if (StatisticsTracker.TryGetPlayerDocument(steamId) is PlayerStatisticsDocument live
                && !string.IsNullOrWhiteSpace(live.DisplayName)
                && live.DisplayName != steamId.ToString())
            {
                return live.DisplayName;
            }

            string? localNick = TryGetLocalNickName();
            return localNick != null && LocalPlayerHelper.IsLocalSteamId(steamId) ? localNick : steamId.ToString();
        }

        private static string? ResolveNickNameFromActorMap(long playerUid, ulong steamId = 0)
        {
            try
            {
                Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
                GameMainBase? main = pdata?.main;
                if (main == null)
                {
                    return null;
                }

                Dictionary<int, ProtoActor>? map = main.GetProtoActorMap();
                if (map == null)
                {
                    return null;
                }

                foreach (ProtoActor? actor in map.Values)
                {
                    if (actor == null || string.IsNullOrWhiteSpace(actor.nickName))
                    {
                        continue;
                    }

                    if (playerUid != 0 && actor.UID == playerUid)
                    {
                        return actor.nickName;
                    }

                    if (steamId != 0 && StatisticsTracker.TryResolveSteamId(actor) == steamId)
                    {
                        return actor.nickName;
                    }
                }
            }
            catch
            {
                /* scene may be transitioning */
            }

            return null;
        }

        private static string? TryGetLeaderboardDisplayName(int saveSlotId, ulong steamId)
        {
            if (saveSlotId < 0)
            {
                saveSlotId = WebDashboardGameState.GetSaveSlotId();
            }

            return saveSlotId < 0
                ? null
                : WebDashboardStatisticsBridge.TryGetDisplayNameFromLeaderboard(
                WebDashboardStatisticsBridge.GetLeaderboardDocument(saveSlotId),
                steamId);
        }

        private static string? TryGetLocalNickName()
        {
            try
            {
                Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
                GameMainBase? main = pdata?.main;
                if (main != null)
                {
                    MethodInfo? getHostNick = main.GetType().GetMethod(
                        "GetHostActorNickName",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (getHostNick?.Invoke(main, null) is string hostNick && !string.IsNullOrWhiteSpace(hostNick))
                    {
                        return hostNick;
                    }
                }

                FieldInfo? myNickField = typeof(Hub.PersistentData).GetField(
                    "MyNickName",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (myNickField?.GetValue(pdata) is string myNick && !string.IsNullOrWhiteSpace(myNick))
                {
                    return myNick;
                }
            }
            catch
            {
                /* hub may be unavailable */
            }

            return null;
        }

        private static Dictionary<ulong, string>? TryGetSteamNameCache()
        {
            try
            {
                Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
                GameMainBase? main = pdata?.main;
                if (main == null)
                {
                    return null;
                }

                FieldInfo? cacheField = main.GetType().GetField(
                    "steamIDToNameCache",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return cacheField?.GetValue(main) as Dictionary<ulong, string>;
            }
            catch
            {
                return null;
            }
        }
    }
}
