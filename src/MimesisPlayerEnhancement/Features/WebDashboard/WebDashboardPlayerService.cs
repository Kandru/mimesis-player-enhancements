using System.Collections.Generic;
using System.Reflection;
using MimesisPlayerEnhancement.Features.Statistics;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;
using MimesisPlayerEnhancement.Util;
using Mimic.Actors;
using Mimic.Voice.SpeechSystem;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardPlayerService
    {
        internal static List<WebDashboardPlayerDto> CollectPlayers()
        {
            List<WebDashboardPlayerDto> live = CollectLivePlayers();
            return MergePlayerLists(live, WebDashboardOfflinePlayerCache.GetCached());
        }

        internal static List<WebDashboardPlayerDto> CollectLivePlayers()
        {
            Dictionary<ulong, WebDashboardPlayerDto> playersBySteam = [];
            SessionManager? sessionManager = WebDashboardSessionAccess.GetSessionManager();
            ulong localSteamId = LocalPlayerHelper.TryGetLocalSteamId();
            Dictionary<ulong, string>? nameCache = TryGetSteamNameCache();

            if (sessionManager != null)
            {
                foreach (SessionContext context in WebDashboardSessionAccess.EnumerateSessionContexts(sessionManager))
                {
                    WebDashboardPlayerDto? dto = TryBuildPlayerDto(context, sessionManager, localSteamId, nameCache);
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
                        nameCache);
                    if (fallback != null)
                    {
                        playersBySteam[steamId] = fallback;
                    }
                }
            }

            if (WebDashboardGameState.IsHost() && localSteamId != 0 && !playersBySteam.ContainsKey(localSteamId))
            {
                WebDashboardPlayerDto? hostFallback = BuildFallbackPlayerDto(
                    localSteamId,
                    sessionManager,
                    localSteamId,
                    nameCache,
                    forceHost: true);
                if (hostFallback != null)
                {
                    playersBySteam[localSteamId] = hostFallback;
                }
            }

            if (WebDashboardGameState.IsHost() && sessionManager != null)
            {
                foreach (ulong bannedSteamId in WebDashboardSessionAccess.EnumerateBannedSteamIds(sessionManager))
                {
                    if (bannedSteamId == 0 || playersBySteam.ContainsKey(bannedSteamId))
                    {
                        continue;
                    }

                    WebDashboardPlayerDto? bannedOffline = BuildFallbackPlayerDto(
                        bannedSteamId,
                        sessionManager,
                        localSteamId,
                        nameCache);
                    if (bannedOffline != null)
                    {
                        bannedOffline.IsBanned = true;
                        playersBySteam[bannedSteamId] = bannedOffline;
                    }
                }
            }

            List<WebDashboardPlayerDto> players = [.. playersBySteam.Values];
            SortPlayers(players);
            return players;
        }

        internal static List<WebDashboardPlayerDto> BuildOfflineStatisticsPlayers()
        {
            if (!WebDashboardGameState.IsHost() || !ModConfig.EnableStatistics.Value)
            {
                return [];
            }

            int saveSlotId = WebDashboardGameState.GetSaveSlotId();
            if (saveSlotId < 0)
            {
                return [];
            }

            Dictionary<ulong, WebDashboardPlayerDto> playersBySteam = [];
            ulong localSteamId = LocalPlayerHelper.TryGetLocalSteamId();
            Dictionary<ulong, string>? nameCache = TryGetSteamNameCache();
            MergeOfflineStatisticsPlayers(playersBySteam, localSteamId, nameCache, saveSlotId);
            List<WebDashboardPlayerDto> players = [.. playersBySteam.Values];
            SortPlayers(players);
            return players;
        }

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

        private static void SortPlayers(List<WebDashboardPlayerDto> players)
        {
            players.Sort((a, b) =>
            {
                int hostCmp = b.IsHost.CompareTo(a.IsHost);
                return hostCmp != 0 ? hostCmp : string.Compare(a.DisplayName, b.DisplayName, System.StringComparison.OrdinalIgnoreCase);
            });
        }

        internal static string ResolveDisplayNameForSteamId(ulong steamId, int saveSlotId = -1)
        {
            return steamId == 0
                ? ""
                : ResolveDisplayNameCore(null, steamId, 0, TryGetSteamNameCache(), saveSlotId);
        }

        private static WebDashboardPlayerDto? BuildFallbackPlayerDto(
            ulong steamId,
            SessionManager? sessionManager,
            ulong localSteamId,
            Dictionary<ulong, string>? nameCache,
            bool forceHost = false)
        {
            long playerUid = 0;
            SessionContext? matchedContext = null;
            if (sessionManager != null)
            {
                foreach (SessionContext context in WebDashboardSessionAccess.EnumerateSessionContexts(sessionManager))
                {
                    if (context.SteamID != steamId)
                    {
                        continue;
                    }

                    matchedContext = context;
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
            bool isHost = forceHost || (WebDashboardGameState.IsHost() && isLocal);

            WebDashboardPlayerDto dto = new()
            {
                SteamId = steamId,
                PlayerUid = playerUid,
                DisplayName = ResolveDisplayNameCore(null, steamId, playerUid, nameCache),
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

            EnrichPlayerDto(dto, sessionManager, matchedContext);
            return dto;
        }

        private static void MergeOfflineStatisticsPlayers(
            Dictionary<ulong, WebDashboardPlayerDto> playersBySteam,
            ulong localSteamId,
            Dictionary<ulong, string>? nameCache,
            int saveSlotId)
        {
            if (saveSlotId < 0)
            {
                return;
            }

            if (!ModConfig.EnableStatistics.Value)
            {
                return;
            }

            foreach (PlayerStatisticsDocument player in StatisticsTracker.GetCachedPlayerDocumentsView())
            {
                if (player.SteamId == 0 || playersBySteam.ContainsKey(player.SteamId))
                {
                    continue;
                }

                bool isLocal = localSteamId != 0 && player.SteamId == localSteamId;
                WebDashboardPlayerDto dto = new()
                {
                    SteamId = player.SteamId,
                    DisplayName = IsUsableName(player.DisplayName, player.SteamId)
                        ? player.DisplayName
                        : ResolveDisplayNameCore(null, player.SteamId, 0, nameCache, saveSlotId),
                    IsHost = WebDashboardGameState.IsHost() && isLocal,
                    IsLocal = isLocal,
                };

                EnrichStoredPlayerDto(dto, saveSlotId);
                playersBySteam[player.SteamId] = dto;
            }
        }

        private static void EnrichStoredPlayerDto(WebDashboardPlayerDto dto, int saveSlotId)
        {
            if (dto.SteamId == 0 || saveSlotId < 0 || !ModConfig.EnableStatistics.Value)
            {
                return;
            }

            if (StatisticsTracker.TryGetPlayerDocument(dto.SteamId) is not PlayerStatisticsDocument doc)
            {
                return;
            }

            dto.CurrentSession = BuildSessionStatsFromDocument(doc);
        }

        private static WebDashboardPlayerDto? TryBuildPlayerDto(
            SessionContext context,
            SessionManager sessionManager,
            ulong localSteamId,
            Dictionary<ulong, string>? nameCache)
        {
            try
            {
                ulong steamId = context.SteamID;
                if (steamId == 0 && WebDashboardGameState.IsHost() && LocalPlayerHelper.IsLocalSteamId(localSteamId))
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
                bool isLocal = localSteamId != 0 && steamId == localSteamId;
                bool isHost = vPlayer?.IsHost ?? false;
                if (!isHost && isLocal)
                {
                    isHost = WebDashboardGameState.IsHost();
                }

                WebDashboardPlayerDto dto = new()
                {
                    SteamId = steamId,
                    PlayerUid = playerUid,
                    DisplayName = ResolveDisplayNameCore(context, steamId, playerUid, nameCache),
                    IsHost = isHost,
                    IsLocal = isLocal,
                    IsBanned = WebDashboardSessionAccess.IsBanned(sessionManager, steamId),
                };

                if (playerUid != 0
                    && (WebDashboardSessionAccess.TryGetNetworkGrade(sessionManager, playerUid, out int grade)
                        || WebDashboardPatches.TryGetCachedGrade(playerUid, out grade)))
                {
                    dto.NetworkGrade = grade;
                }

                EnrichPlayerDto(dto, sessionManager, context);
                return dto;
            }
            catch
            {
                return null;
            }
        }

        private static void ApplyAliveStatus(WebDashboardPlayerDto dto, SessionContext? context)
        {
            VPlayer? vPlayer = context != null ? WebDashboardSessionAccess.GetVPlayer(context) : null;
            if (vPlayer != null)
            {
                dto.IsAlive = vPlayer.IsAliveStatus();
                return;
            }

            if (TryGetAliveFromProtoActor(dto.PlayerUid, dto.SteamId, out bool isAlive))
            {
                dto.IsAlive = isAlive;
            }
        }

        private static void ApplyVitals(WebDashboardPlayerDto dto, SessionContext? context)
        {
            if (dto.PlayerUid == 0)
            {
                return;
            }

            VPlayer? vPlayer = context != null ? WebDashboardSessionAccess.GetVPlayer(context) : null;
            if (vPlayer?.StatControlUnit != null)
            {
                StatController stats = vPlayer.StatControlUnit;
                dto.Health = stats.GetCurrentHP();
                dto.MaxHealth = stats.GetSpecificStatValue(StatType.HP);
                dto.ToxicPercent = ComputeVitalPercent(
                    stats.GetCurrentConta(),
                    stats.GetSpecificStatValue(StatType.Conta));
                return;
            }

            if (TryGetVitalsFromProtoActor(dto.PlayerUid, dto.SteamId, out long health, out long maxHealth, out double toxicPercent))
            {
                dto.Health = health;
                dto.MaxHealth = maxHealth;
                dto.ToxicPercent = toxicPercent;
            }
        }

        private static double? ComputeVitalPercent(long current, long max)
        {
            if (max <= 0)
            {
                return null;
            }

            return System.Math.Clamp((double)current / max * 100.0, 0.0, 100.0);
        }

        private static bool TryGetVitalsFromProtoActor(
            long playerUid,
            ulong steamId,
            out long health,
            out long maxHealth,
            out double toxicPercent)
        {
            health = 0;
            maxHealth = 0;
            toxicPercent = 0;
            try
            {
                Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
                GameMainBase? main = pdata?.main;
                if (main == null)
                {
                    return false;
                }

                Dictionary<int, ProtoActor>? map = main.GetProtoActorMap();
                if (map == null)
                {
                    return false;
                }

                List<ProtoActor?> actors = [.. map.Values];
                foreach (ProtoActor? actor in actors)
                {
                    if (actor == null || actor.ActorType != ActorType.Player)
                    {
                        continue;
                    }

                    bool match = (playerUid != 0 && actor.UID == playerUid)
                        || (steamId != 0 && StatisticsTracker.TryResolveSteamId(actor) == steamId);
                    if (!match)
                    {
                        continue;
                    }

                    health = actor.netSyncActorData.hp;
                    maxHealth = actor.netSyncActorData.maxHP;
                    long conta = actor.netSyncActorData.conta;
                    long maxConta = actor.netSyncActorData.maxConta;
                    toxicPercent = ComputeVitalPercent(conta, maxConta) ?? 0;
                    return true;
                }
            }
            catch
            {
                /* scene may be transitioning */
            }

            return false;
        }

        private static bool TryGetAliveFromProtoActor(long playerUid, ulong steamId, out bool isAlive)
        {
            isAlive = true;
            try
            {
                Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
                GameMainBase? main = pdata?.main;
                if (main == null)
                {
                    return false;
                }

                Dictionary<int, ProtoActor>? map = main.GetProtoActorMap();
                if (map == null)
                {
                    return false;
                }

                List<ProtoActor?> actors = [.. map.Values];
                foreach (ProtoActor? actor in actors)
                {
                    if (actor == null || actor.ActorType != ActorType.Player)
                    {
                        continue;
                    }

                    if (playerUid != 0 && actor.UID == playerUid)
                    {
                        isAlive = !actor.dead;
                        return true;
                    }

                    if (steamId != 0 && StatisticsTracker.TryResolveSteamId(actor) == steamId)
                    {
                        isAlive = !actor.dead;
                        return true;
                    }
                }
            }
            catch
            {
                /* scene may be transitioning */
            }

            return false;
        }

        private static bool IsUsableName(string? name, ulong steamId)
        {
            return !string.IsNullOrWhiteSpace(name) && name != steamId.ToString();
        }

        // Single fallback chain for every payload: live session nick → Steam name cache → actor nick
        // → live statistics doc → stored statistics doc → leaderboard entry → local nick → Steam ID.
        private static string ResolveDisplayNameCore(
            SessionContext? context,
            ulong steamId,
            long playerUid,
            Dictionary<ulong, string>? nameCache,
            int saveSlotId = -1)
        {
            if (IsUsableName(context?.NickName, steamId))
            {
                return context!.NickName;
            }

            if (nameCache != null
                && nameCache.TryGetValue(steamId, out string? cached)
                && IsUsableName(cached, steamId))
            {
                return cached;
            }

            string? fromActor = ResolveNickNameFromActorMap(playerUid, steamId);
            if (IsUsableName(fromActor, steamId))
            {
                return fromActor!;
            }

            if (StatisticsTracker.TryGetPlayerDocument(steamId) is PlayerStatisticsDocument live
                && IsUsableName(live.DisplayName, steamId))
            {
                return live.DisplayName;
            }

            if (saveSlotId < 0)
            {
                saveSlotId = WebDashboardGameState.GetSaveSlotId();
            }

            string? remembered = WebDashboardPlayerNameStore.TryGetName(saveSlotId, steamId);
            if (IsUsableName(remembered, steamId))
            {
                return remembered!;
            }

            string? localNick = TryGetLocalNickName();
            return localNick != null && LocalPlayerHelper.IsLocalSteamId(steamId) ? localNick : steamId.ToString();
        }

        // Keeps the live statistics document and the name sidecar current so offline views
        // and later sessions show the latest known name after a player reconnects.
        private static void PersistDisplayName(WebDashboardPlayerDto dto)
        {
            if (!IsUsableName(dto.DisplayName, dto.SteamId))
            {
                return;
            }

            WebDashboardPlayerNameStore.RememberName(
                WebDashboardGameState.GetSaveSlotId(),
                dto.SteamId,
                dto.DisplayName);

            if (ModConfig.EnableStatistics.Value
                && StatisticsTracker.TryGetPlayerDocument(dto.SteamId) is PlayerStatisticsDocument doc
                && doc.DisplayName != dto.DisplayName)
            {
                doc.DisplayName = dto.DisplayName;
            }
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

                List<ProtoActor?> actors = [.. map.Values];
                foreach (ProtoActor? actor in actors)
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

        private static void EnrichPlayerDto(
            WebDashboardPlayerDto dto,
            SessionManager? sessionManager,
            SessionContext? context)
        {
            ApplyConnectionInfo(dto, context);

            PersistDisplayName(dto);

            ApplyAliveStatus(dto, context);

            if (WebDashboardGameState.IsHost())
            {
                ApplyVitals(dto, context);
            }

            if (WebDashboardGameState.IsHost() && ModConfig.EnableStatistics.Value)
            {
                dto.CurrentSession = BuildSessionStats(dto.SteamId);
            }
        }

        private static void ApplyConnectionInfo(WebDashboardPlayerDto dto, SessionContext? context)
        {
            SpeechEventArchive? archive = FindArchive(dto.PlayerUid, dto.SteamId);
            PlayerConnectionInfo? merged = null;

            if (archive != null
                && VoiceEventStats.TryGetConnectionInfo(archive, context, dto.SteamId, out PlayerConnectionInfo fromArchive))
            {
                merged = fromArchive;
            }

            if (context != null
                && VoiceEventStats.TryGetConnectionInfo(
                    context,
                    dto.PlayerUid,
                    dto.SteamId,
                    dto.IsLocal,
                    out PlayerConnectionInfo fromContext))
            {
                if (merged == null)
                {
                    merged = fromContext;
                }
                else
                {
                    if (IsUnavailableConnectionAddress(merged.ConnectionAddress)
                        && !IsUnavailableConnectionAddress(fromContext.ConnectionAddress))
                    {
                        merged.ConnectionAddress = fromContext.ConnectionAddress;
                    }

                    if (merged.PlayerUid == 0 && fromContext.PlayerUid != 0)
                    {
                        merged.PlayerUid = fromContext.PlayerUid;
                    }

                    if (merged.SteamId == 0 && fromContext.SteamId != 0)
                    {
                        merged.SteamId = fromContext.SteamId;
                    }
                }
            }

            if (merged != null)
            {
                ApplyConnectionFields(dto, merged);
                return;
            }

            dto.ConnectionRole = dto.IsLocal && WebDashboardGameState.IsHost() ? "host" : "client";
            dto.ConnectionAddress = dto.IsLocal ? "local" : "(unavailable)";
            dto.VoiceLineCount = archive != null ? VoiceEventStats.GetVoiceLineCount(archive) : 0;
        }

        private static bool IsUnavailableConnectionAddress(string address)
        {
            return string.IsNullOrEmpty(address) || address == "(unavailable)";
        }

        private static void ApplyConnectionFields(WebDashboardPlayerDto dto, PlayerConnectionInfo info)
        {
            if (info.PlayerUid != 0)
            {
                dto.PlayerUid = info.PlayerUid;
            }

            if (!string.IsNullOrWhiteSpace(info.DisplayName) && info.DisplayName != "(pending)")
            {
                dto.DisplayName = info.DisplayName;
            }

            dto.ConnectionRole = info.ConnectionRole;
            dto.ConnectionAddress = info.ConnectionAddress;
            dto.VoiceLineCount = info.VoiceLineCount;

            if (info.SteamId != 0)
            {
                dto.SteamId = info.SteamId;
            }
        }

        private static SpeechEventArchive? FindArchive(long playerUid, ulong steamId)
        {
            foreach (SpeechEventArchive archive in SpeechEventArchiveRegistry.EnumerateActive())
            {
                if (archive == null)
                {
                    continue;
                }

                long archiveUid;
                try
                {
                    archiveUid = archive.PlayerUID;
                }
                catch
                {
                    continue;
                }

                if (playerUid != 0 && archiveUid == playerUid)
                {
                    return archive;
                }

                if (steamId != 0 && VoiceEventStats.TryGetConnectionInfo(archive, out PlayerConnectionInfo info)
                    && info.SteamId == steamId)
                {
                    return archive;
                }
            }

            return null;
        }

        private static WebDashboardSessionStatsDto? BuildSessionStats(ulong steamId)
        {
            if (steamId == 0)
            {
                return null;
            }

            if (StatisticsTracker.TryGetPlayerDocument(steamId) is not PlayerStatisticsDocument doc
                || doc.CurrentSession?.Counters == null)
            {
                return null;
            }

            return BuildSessionStatsFromDocument(doc);
        }

        private static WebDashboardSessionStatsDto? BuildSessionStatsFromDocument(PlayerStatisticsDocument doc)
        {
            if (doc.CurrentSession?.Counters == null)
            {
                return null;
            }

            StatCounters c = doc.CurrentSession.Counters;
            return new WebDashboardSessionStatsDto
            {
                CurrencyEarned = c.CurrencyEarned,
                SurvivalDeaths = c.SurvivalDeaths,
                SurvivalWins = c.SurvivalWins,
                SurvivalLeftBehind = c.SurvivalLeftBehind,
                DeathmatchDeaths = c.DeathmatchDeaths,
                DeathmatchWins = c.DeathmatchWins,
                Revives = c.Revives,
                MimicEncounterCount = c.MimicEncounterCount,
                ItemCarryCount = c.ItemCarryCount,
                DamageToAlly = c.DamageToAlly,
                TotalConnectedSeconds = c.TotalConnectedSeconds,
                MonsterKillsByMasterId = new Dictionary<string, long>(c.MonsterKillsByMasterId ?? []),
                DeathsByTrapType = new Dictionary<string, long>(c.DeathsByTrapType ?? []),
            };
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
