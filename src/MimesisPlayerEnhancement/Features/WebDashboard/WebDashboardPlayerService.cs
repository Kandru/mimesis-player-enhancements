using System.Reflection;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardPlayerService
    {
        internal static List<WebDashboardPlayerDto> CollectLivePlayers()
        {
            Dictionary<ulong, WebDashboardPlayerDto> playersBySteam = [];
            SessionManager? sessionManager = WebDashboardSessionAccess.GetSessionManager();
            ulong localSteamId = LocalPlayerHelper.TryGetLocalSteamId();
            Dictionary<ulong, string>? nameCache = TryGetSteamNameCache();
            WebDashboardLiveRoster roster = WebDashboardLiveRoster.Capture();

            foreach (WebDashboardLivePlayer entry in roster.Enumerate())
            {
                SessionContext? context = FindSessionContext(sessionManager, entry.SteamId, entry.PlayerUid);
                WebDashboardPlayerDto? dto = BuildLivePlayerDto(
                    entry.Actor,
                    entry.SteamId,
                    entry.PlayerUid,
                    sessionManager,
                    context,
                    localSteamId,
                    nameCache,
                    roster);
                if (dto != null)
                {
                    playersBySteam[dto.SteamId] = dto;
                }
            }

            if (ModConfig.EnableStatistics.Value)
            {
                foreach (ulong steamId in PlayerRegistry.GetConnectedSteamIds())
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
                        roster);
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
                    roster,
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
                        roster);
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

        internal static List<WebDashboardPlayerDto> BuildOfflineStatisticsPlayers(OfflinePlayerBuildContext context)
        {
            if (!context.IsHost || !context.EnableStatistics || context.SaveSlotId < 0)
            {
                return [];
            }

            Dictionary<ulong, WebDashboardPlayerDto> playersBySteam = [];
            MergeOfflineStatisticsPlayers(
                playersBySteam,
                context.LocalSteamId,
                context.SaveSlotId,
                context.IsHost);
            List<WebDashboardPlayerDto> players = [.. playersBySteam.Values];
            WebDashboardPlayerListMerger.SortPlayers(players);
            return players;
        }

        internal readonly struct OfflinePlayerBuildContext
        {
            internal int SaveSlotId { get; }
            internal ulong LocalSteamId { get; }
            internal bool IsHost { get; }
            internal bool EnableStatistics { get; }

            internal OfflinePlayerBuildContext(
                int saveSlotId,
                ulong localSteamId,
                bool isHost,
                bool enableStatistics)
            {
                SaveSlotId = saveSlotId;
                LocalSteamId = localSteamId;
                IsHost = isHost;
                EnableStatistics = enableStatistics;
            }

            internal static OfflinePlayerBuildContext Capture()
            {
                return new OfflinePlayerBuildContext(
                    WebDashboardGameState.GetSaveSlotId(),
                    LocalPlayerHelper.TryGetLocalSteamId(),
                    WebDashboardGameState.IsHost(),
                    ModConfig.EnableStatistics.Value);
            }
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
                    WebDashboardLiveRoster.Capture(),
                    saveSlotId);
        }

        private static WebDashboardPlayerDto? BuildFallbackPlayerDto(
            ulong steamId,
            SessionManager? sessionManager,
            ulong localSteamId,
            Dictionary<ulong, string>? nameCache,
            WebDashboardLiveRoster roster,
            bool forceHost = false)
        {
            SessionContext? matchedContext = FindSessionContext(sessionManager, steamId, 0);
            long playerUid = 0;
            if (matchedContext != null)
            {
                try
                {
                    playerUid = matchedContext.GetPlayerUID();
                }
                catch
                {
                    /* player may still be spawning */
                }
            }

            if (playerUid == 0 && roster.TryGetBySteamId(steamId, out ProtoActor actor))
            {
                playerUid = actor.UID;
            }

            bool isLocal = localSteamId != 0 && steamId == localSteamId;
            bool isHost = forceHost || (WebDashboardGameState.IsHost() && isLocal);
            if (!forceHost && roster.TryGetBySteamId(steamId, out ProtoActor hostActor))
            {
                isHost = hostActor.IsHost;
                if (!isHost && isLocal && WebDashboardGameState.IsHost())
                {
                    isHost = true;
                }
            }

            WebDashboardPlayerDto dto = new()
            {
                SteamId = steamId,
                PlayerUid = playerUid,
                DisplayName = ResolveDisplayNameCore(matchedContext, steamId, playerUid, nameCache, roster),
                IsHost = isHost,
                IsLocal = isLocal,
                IsBanned = sessionManager != null && WebDashboardSessionAccess.IsBanned(sessionManager, steamId),
            };

            if (sessionManager != null
                && playerUid != 0
                && (WebDashboardSessionAccess.TryGetNetworkGrade(sessionManager, playerUid, out int grade)
                    || WebDashboardPatchHelpers.TryGetCachedGrade(playerUid, out grade)))
            {
                dto.NetworkGrade = grade;
            }

            EnrichPlayerDto(dto, sessionManager, matchedContext, roster);
            return ShouldIncludeLivePlayer(dto) ? dto : null;
        }

        private static bool ShouldIncludeLivePlayer(WebDashboardPlayerDto dto)
        {
            if (IsUsableName(dto.DisplayName, dto.SteamId))
            {
                return true;
            }

            if (GameSessionAccess.IsSteamIdRegisteredInSession(dto.SteamId))
            {
                return true;
            }

            int saveSlotId = WebDashboardGameState.GetSaveSlotId();
            return SaveSlotDocumentStore.TryGetName(saveSlotId, dto.SteamId, out _);
        }

        private static void MergeOfflineStatisticsPlayers(
            Dictionary<ulong, WebDashboardPlayerDto> playersBySteam,
            ulong localSteamId,
            int saveSlotId,
            bool isHost)
        {
            if (saveSlotId < 0)
            {
                return;
            }

            foreach (PlayerStatisticsDocument player in PlayerRegistry.GetAllStatistics())
            {
                if (player.SteamId == 0 || playersBySteam.ContainsKey(player.SteamId))
                {
                    continue;
                }

                bool isLocal = localSteamId != 0 && player.SteamId == localSteamId;
                WebDashboardPlayerDto dto = new()
                {
                    SteamId = player.SteamId,
                    DisplayName = SaveSlotDocumentStore.ResolveDisplayName(
                        saveSlotId,
                        player.SteamId,
                        player.DisplayName),
                    IsHost = isHost && isLocal,
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
            dto.TotalStats = BuildTotalStatsFromDocument(doc);
            dto.RunStats = BuildRunStatsFromDocument(doc);
            dto.ActivityState = "offline";
            dto.ActivityDetail = string.Empty;

            SessionManager? sessionManager = WebDashboardSessionAccess.GetSessionManager();
            if (sessionManager != null)
            {
                dto.IsBanned = WebDashboardSessionAccess.IsBanned(sessionManager, dto.SteamId);
            }
        }

        private static SessionContext? FindSessionContext(
            SessionManager? sessionManager,
            ulong steamId,
            long playerUid)
        {
            if (sessionManager == null)
            {
                return null;
            }

            foreach (SessionContext context in WebDashboardSessionAccess.EnumerateSessionContexts(sessionManager))
            {
                try
                {
                    if (steamId != 0 && context.SteamID == steamId)
                    {
                        return context;
                    }

                    if (playerUid != 0 && context.GetPlayerUID() == playerUid)
                    {
                        return context;
                    }
                }
                catch
                {
                    /* context may be mid-setup or disposed */
                }
            }

            return null;
        }

        private static WebDashboardPlayerDto? BuildLivePlayerDto(
            ProtoActor actor,
            ulong steamId,
            long playerUid,
            SessionManager? sessionManager,
            SessionContext? context,
            ulong localSteamId,
            Dictionary<ulong, string>? nameCache,
            WebDashboardLiveRoster roster)
        {
            try
            {
                if (steamId == 0)
                {
                    return null;
                }

                bool isLocal = localSteamId != 0 && steamId == localSteamId;
                bool isHost = actor.IsHost;
                if (!isHost && isLocal && WebDashboardGameState.IsHost())
                {
                    isHost = true;
                }

                VPlayer? vPlayer = context != null ? WebDashboardSessionAccess.GetVPlayer(context) : null;
                if (vPlayer != null && vPlayer.IsHost)
                {
                    isHost = true;
                }

                WebDashboardPlayerDto dto = new()
                {
                    SteamId = steamId,
                    PlayerUid = playerUid,
                    DisplayName = ResolveDisplayNameCore(context, steamId, playerUid, nameCache, roster),
                    IsHost = isHost,
                    IsLocal = isLocal,
                    IsBanned = sessionManager != null && WebDashboardSessionAccess.IsBanned(sessionManager, steamId),
                };

                if (sessionManager != null
                    && playerUid != 0
                    && (WebDashboardSessionAccess.TryGetNetworkGrade(sessionManager, playerUid, out int grade)
                        || WebDashboardPatchHelpers.TryGetCachedGrade(playerUid, out grade)))
                {
                    dto.NetworkGrade = grade;
                }

                EnrichPlayerDto(dto, sessionManager, context, roster);
                return ShouldIncludeLivePlayer(dto) ? dto : null;
            }
            catch
            {
                return null;
            }
        }

        private static void ApplyAliveStatus(
            WebDashboardPlayerDto dto,
            SessionContext? context,
            WebDashboardLiveRoster roster)
        {
            VPlayer? vPlayer = context != null ? WebDashboardSessionAccess.GetVPlayer(context) : null;
            if (vPlayer != null)
            {
                dto.IsAlive = vPlayer.IsAliveStatus();
                return;
            }

            if (roster.TryGetAlive(dto.PlayerUid, dto.SteamId, out bool isAlive))
            {
                dto.IsAlive = isAlive;
            }
        }

        private static void ApplyVitals(
            WebDashboardPlayerDto dto,
            SessionContext? context,
            WebDashboardLiveRoster roster)
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

            if (roster.TryGetVitals(dto.PlayerUid, dto.SteamId, out long health, out long maxHealth, out double toxicPercent))
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

        // Single fallback chain for live payloads: session nick → Steam name cache → actor nick
        // → live statistics doc → name sidecar → local nick → Steam ID.
        private static string ResolveDisplayNameCore(
            SessionContext? context,
            ulong steamId,
            long playerUid,
            Dictionary<ulong, string>? nameCache,
            WebDashboardLiveRoster roster,
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

            string? fromActor = roster.ResolveNickName(playerUid, steamId);
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

            string? remembered = SaveSlotDocumentStore.TryGetName(saveSlotId, steamId, out string? fromDoc)
                ? fromDoc
                : null;
            if (IsUsableName(remembered, steamId))
            {
                return remembered!;
            }

            string? localNick = TryGetLocalNickName();
            return localNick != null && LocalPlayerHelper.IsLocalSteamId(steamId) ? localNick : steamId.ToString();
        }

        // Keeps the slot document and live statistics document current after a player reconnects.
        private static void PersistDisplayName(WebDashboardPlayerDto dto)
        {
            if (!IsUsableName(dto.DisplayName, dto.SteamId))
            {
                return;
            }

            if (JoinAnytime.JoinAnytimePlayerRegistration.ShouldDeferRegistration(dto.PlayerUid))
            {
                return;
            }

            if (PlayerRegistry.UpdateDisplayName(dto.SteamId, dto.DisplayName))
            {
                WebDashboardSnapshotCache.MarkDirty();
            }
        }

        private static void EnrichPlayerDto(
            WebDashboardPlayerDto dto,
            SessionManager? sessionManager,
            SessionContext? context,
            WebDashboardLiveRoster roster)
        {
            ApplyConnectionInfo(dto, context);

            PersistDisplayName(dto);

            ApplyAliveStatus(dto, context, roster);

            if (WebDashboardGameState.IsHost())
            {
                ApplyVitals(dto, context, roster);
            }

            if (WebDashboardGameState.IsHost() && ModConfig.EnableStatistics.Value)
            {
                dto.CurrentSession = BuildSessionStats(dto.SteamId);
                dto.TotalStats = BuildTotalStats(dto.SteamId);
                dto.RunStats = BuildRunStats(dto.SteamId);
            }

            if (WebDashboardGameState.IsHost() && ModConfig.EnableJoinAnytime.Value)
            {
                LateJoinRouteTracker.ApplyDashboardFields(dto, context);
            }

            WebDashboardPlayerStateResolver.ApplyActivityState(dto, context);

            if (WebDashboardGameState.IsHost())
            {
                long cheatUid = ResolveCheatPlayerUid(dto, context);
                if (cheatUid != 0)
                {
                    if (dto.PlayerUid == 0)
                    {
                        dto.PlayerUid = cheatUid;
                    }

                    dto.GodMode = WebDashboardHostCheatsRuntime.IsGodModeEnabled(cheatUid);
                    dto.NoClip = WebDashboardHostCheatsRuntime.IsNoClipEnabled(cheatUid);
                }
            }
        }

        private static long ResolveCheatPlayerUid(WebDashboardPlayerDto dto, SessionContext? context)
        {
            if (dto.PlayerUid != 0)
            {
                return dto.PlayerUid;
            }

            VPlayer? vPlayer = context != null ? WebDashboardSessionAccess.GetVPlayer(context) : null;
            if (vPlayer != null)
            {
                return vPlayer.UID;
            }

            if (dto.SteamId == 0)
            {
                return 0;
            }

            SessionManager? sessionManager = WebDashboardSessionAccess.GetSessionManager();
            if (sessionManager == null)
            {
                return 0;
            }

            foreach (SessionContext sessionContext in WebDashboardSessionAccess.EnumerateSessionContexts(sessionManager))
            {
                if (sessionContext.SteamID != dto.SteamId)
                {
                    continue;
                }

                vPlayer = WebDashboardSessionAccess.GetVPlayer(sessionContext);
                return vPlayer?.UID ?? 0;
            }

            return 0;
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

        private static WebDashboardSessionStatsDto? BuildRunStats(ulong steamId)
        {
            if (steamId == 0)
            {
                return null;
            }

            if (StatisticsTracker.TryGetPlayerDocument(steamId) is not PlayerStatisticsDocument doc)
            {
                return null;
            }

            return BuildRunStatsFromDocument(doc);
        }

        private static WebDashboardSessionStatsDto? BuildTotalStats(ulong steamId)
        {
            if (steamId == 0)
            {
                return null;
            }

            if (StatisticsTracker.TryGetPlayerDocument(steamId) is not PlayerStatisticsDocument doc)
            {
                return null;
            }

            return BuildTotalStatsFromDocument(doc);
        }

        private static WebDashboardSessionStatsDto? BuildSessionStatsFromDocument(PlayerStatisticsDocument doc)
        {
            if (doc.CurrentSession?.Counters == null)
            {
                return null;
            }

            return MapCounters(doc.CurrentSession.Counters);
        }

        private static WebDashboardSessionStatsDto? BuildRunStatsFromDocument(PlayerStatisticsDocument doc)
        {
            if (doc.CurrentRun?.Counters == null)
            {
                return null;
            }

            return MapCounters(doc.CurrentRun.Counters, includeScore: true);
        }

        private static WebDashboardSessionStatsDto? BuildTotalStatsFromDocument(PlayerStatisticsDocument doc)
        {
            if (doc.Global?.Counters == null)
            {
                return null;
            }

            return MapCounters(doc.Global.Counters);
        }

        private static WebDashboardSessionStatsDto MapCounters(StatCounters c, bool includeScore = false)
        {
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
                TrainValueDeposited = c.TrainValueDeposited,
                TrapDeaths = c.TrapDeaths,
                KilledByPlayers = c.KilledByPlayers,
                DungeonExitsAlive = c.DungeonExitsAlive,
                DungeonExitsDead = c.DungeonExitsDead,
                MedianLifetimeMs = TeamValueScore.ComputeMedianLifetimeMs(c.LifetimesOnDeathMs),
                Score = includeScore ? TeamValueScore.Compute(c) : 0,
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
