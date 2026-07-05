using System.Reflection;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;
using Mimic.Voice.SpeechSystem;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardPlayerService
    {
        internal static List<WebDashboardPlayerDto> CollectPlayers()
        {
            List<WebDashboardPlayerDto> live = CollectLivePlayers();
            return WebDashboardPlayerListMerger.MergePlayerLists(live, WebDashboardOfflinePlayerCache.GetCached());
        }

        internal static List<WebDashboardPlayerDto> CollectLivePlayers()
        {
            Dictionary<ulong, WebDashboardPlayerDto> playersBySteam = [];
            SessionManager? sessionManager = WebDashboardSessionAccess.GetSessionManager();
            ulong localSteamId = LocalPlayerHelper.TryGetLocalSteamId();
            Dictionary<ulong, string>? nameCache = TryGetSteamNameCache();
            ProtoActorLookup actorLookup = ProtoActorLookup.Capture();

            if (sessionManager != null)
            {
                foreach (SessionContext context in WebDashboardSessionAccess.EnumerateSessionContexts(sessionManager))
                {
                    WebDashboardPlayerDto? dto = TryBuildPlayerDto(
                        context,
                        sessionManager,
                        localSteamId,
                        nameCache,
                        actorLookup);
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
                        actorLookup);
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
                    actorLookup,
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
                        nameCache,
                        actorLookup);
                    if (bannedOffline != null)
                    {
                        bannedOffline.IsBanned = true;
                        playersBySteam[bannedSteamId] = bannedOffline;
                    }
                }
            }

            List<WebDashboardPlayerDto> players = [.. playersBySteam.Values];
            WebDashboardPlayerListMerger.SortPlayers(players);
            return players;
        }

        private readonly struct ProtoActorLookup
        {
            private readonly Dictionary<long, ProtoActor> _byUid;
            private readonly Dictionary<ulong, ProtoActor> _bySteamId;

            private ProtoActorLookup(
                Dictionary<long, ProtoActor> byUid,
                Dictionary<ulong, ProtoActor> bySteamId)
            {
                _byUid = byUid;
                _bySteamId = bySteamId;
            }

            internal static ProtoActorLookup Capture()
            {
                Dictionary<long, ProtoActor> byUid = [];
                Dictionary<ulong, ProtoActor> bySteamId = [];
                try
                {
                    Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
                    GameMainBase? main = pdata?.main;
                    Dictionary<int, ProtoActor>? map = main?.GetProtoActorMap();
                    if (map == null)
                    {
                        return new ProtoActorLookup(byUid, bySteamId);
                    }

                    foreach (ProtoActor? actor in map.Values)
                    {
                        if (actor == null || actor.ActorType != ActorType.Player)
                        {
                            continue;
                        }

                        if (actor.UID != 0)
                        {
                            byUid[actor.UID] = actor;
                        }

                        if (StatisticsTracker.TryResolveSteamId(actor) is ulong steamId && steamId != 0)
                        {
                            bySteamId[steamId] = actor;
                        }
                    }
                }
                catch
                {
                    /* scene may be transitioning */
                }

                return new ProtoActorLookup(byUid, bySteamId);
            }

            internal bool TryGetByUid(long playerUid, out ProtoActor actor)
            {
                return _byUid.TryGetValue(playerUid, out actor!);
            }

            internal bool TryGetBySteamId(ulong steamId, out ProtoActor actor)
            {
                return _bySteamId.TryGetValue(steamId, out actor!);
            }

            internal string? ResolveNickName(long playerUid, ulong steamId)
            {
                if (playerUid != 0 && TryGetByUid(playerUid, out ProtoActor actor) && IsUsableNick(actor.nickName))
                {
                    return actor.nickName;
                }

                if (steamId != 0 && TryGetBySteamId(steamId, out actor) && IsUsableNick(actor.nickName))
                {
                    return actor.nickName;
                }

                return null;
            }

            internal bool TryGetAlive(long playerUid, ulong steamId, out bool isAlive)
            {
                isAlive = true;
                if (playerUid != 0 && TryGetByUid(playerUid, out ProtoActor actor))
                {
                    isAlive = !actor.dead;
                    return true;
                }

                if (steamId != 0 && TryGetBySteamId(steamId, out actor))
                {
                    isAlive = !actor.dead;
                    return true;
                }

                return false;
            }

            internal bool TryGetVitals(
                long playerUid,
                ulong steamId,
                out long health,
                out long maxHealth,
                out double toxicPercent)
            {
                health = 0;
                maxHealth = 0;
                toxicPercent = 0;
                ProtoActor? actor = null;
                if (playerUid != 0 && TryGetByUid(playerUid, out ProtoActor byUid))
                {
                    actor = byUid;
                }
                else if (steamId != 0 && TryGetBySteamId(steamId, out ProtoActor bySteam))
                {
                    actor = bySteam;
                }

                if (actor == null)
                {
                    return false;
                }

                health = actor.netSyncActorData.hp;
                maxHealth = actor.netSyncActorData.maxHP;
                long conta = actor.netSyncActorData.conta;
                long maxConta = actor.netSyncActorData.maxConta;
                toxicPercent = ComputeVitalPercent(conta, maxConta) ?? 0;
                return true;
            }

            private static bool IsUsableNick(string? nickName)
            {
                return !string.IsNullOrWhiteSpace(nickName);
            }
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
            ProtoActorLookup actorLookup = ProtoActorLookup.Capture();
            MergeOfflineStatisticsPlayers(playersBySteam, localSteamId, nameCache, saveSlotId, actorLookup);
            List<WebDashboardPlayerDto> players = [.. playersBySteam.Values];
            WebDashboardPlayerListMerger.SortPlayers(players);
            return players;
        }

        internal static string ResolveDisplayNameForSteamId(ulong steamId, int saveSlotId = -1)
        {
            return steamId == 0
                ? ""
                : ResolveDisplayNameCore(
                    null,
                    steamId,
                    0,
                    TryGetSteamNameCache(),
                    ProtoActorLookup.Capture(),
                    saveSlotId);
        }

        private static WebDashboardPlayerDto? BuildFallbackPlayerDto(
            ulong steamId,
            SessionManager? sessionManager,
            ulong localSteamId,
            Dictionary<ulong, string>? nameCache,
            ProtoActorLookup actorLookup,
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
                DisplayName = ResolveDisplayNameCore(null, steamId, playerUid, nameCache, actorLookup),
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

            EnrichPlayerDto(dto, sessionManager, matchedContext, actorLookup);
            return dto;
        }

        private static void MergeOfflineStatisticsPlayers(
            Dictionary<ulong, WebDashboardPlayerDto> playersBySteam,
            ulong localSteamId,
            Dictionary<ulong, string>? nameCache,
            int saveSlotId,
            ProtoActorLookup actorLookup)
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
                        : ResolveDisplayNameCore(
                            null,
                            player.SteamId,
                            0,
                            nameCache,
                            actorLookup,
                            saveSlotId),
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
            Dictionary<ulong, string>? nameCache,
            ProtoActorLookup actorLookup)
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
                    DisplayName = ResolveDisplayNameCore(context, steamId, playerUid, nameCache, actorLookup),
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

                EnrichPlayerDto(dto, sessionManager, context, actorLookup);
                return dto;
            }
            catch
            {
                return null;
            }
        }

        private static void ApplyAliveStatus(
            WebDashboardPlayerDto dto,
            SessionContext? context,
            ProtoActorLookup actorLookup)
        {
            VPlayer? vPlayer = context != null ? WebDashboardSessionAccess.GetVPlayer(context) : null;
            if (vPlayer != null)
            {
                dto.IsAlive = vPlayer.IsAliveStatus();
                return;
            }

            if (actorLookup.TryGetAlive(dto.PlayerUid, dto.SteamId, out bool isAlive))
            {
                dto.IsAlive = isAlive;
            }
        }

        private static void ApplyVitals(
            WebDashboardPlayerDto dto,
            SessionContext? context,
            ProtoActorLookup actorLookup)
        {
            if (dto.PlayerUid == 0 && dto.SteamId == 0)
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

            if (actorLookup.TryGetVitals(dto.PlayerUid, dto.SteamId, out long health, out long maxHealth, out double toxicPercent))
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
            ProtoActorLookup actorLookup,
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

            string? fromActor = actorLookup.ResolveNickName(playerUid, steamId);
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

        private static void EnrichPlayerDto(
            WebDashboardPlayerDto dto,
            SessionManager? sessionManager,
            SessionContext? context,
            ProtoActorLookup actorLookup)
        {
            ApplyConnectionInfo(dto, context);

            PersistDisplayName(dto);

            ApplyAliveStatus(dto, context, actorLookup);

            if (WebDashboardGameState.IsHost())
            {
                ApplyVitals(dto, context, actorLookup);
            }

            if (WebDashboardGameState.IsHost() && ModConfig.EnableStatistics.Value)
            {
                dto.CurrentSession = BuildSessionStats(dto.SteamId);
            }

            if (WebDashboardGameState.IsHost() && ModConfig.EnableJoinAnytime.Value)
            {
                LateJoinRouteTracker.ApplyDashboardFields(dto, context);
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
                DamageToFriend = c.DamageToFriend,
                FriendsKilled = c.FriendsKilled,
                TotalConnectedSeconds = c.TotalConnectedSeconds,
                MonsterKills = new Dictionary<string, long>(c.MonsterKills ?? []),
                DeathsByMonster = new Dictionary<string, long>(c.DeathsByMonster ?? []),
                DeathsByTrap = new Dictionary<string, long>(c.DeathsByTrap ?? []),
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
