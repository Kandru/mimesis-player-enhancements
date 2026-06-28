using System;
using System.Collections.Generic;
using System.Reflection;
using Mimic.Actors;
using Mimic.Voice.SpeechSystem;
using MimesisPlayerEnhancement.Features.Persistence;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using ReluProtocol;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.Statistics;

public static class StatisticsTracker
{
    private const string Feature = "Statistics";
    private const BindingFlags InstanceMemberFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly Dictionary<ulong, PlayerStatisticsDocument> _players = new();
    private static readonly Dictionary<ulong, DateTime> _connectedSince = new();
    private static readonly Dictionary<ulong, PlayReportSnapshot> _cycleBaselines = new();
    private static readonly Dictionary<ulong, int> _voiceEventBaselines = new();
    private static readonly Dictionary<ulong, int> _lastKillCounts = new();
    private static readonly Dictionary<int, ulong> _actorToSteam = new();

    private static int _loadedSlotId = -999;
    private static long _lastCurrencyBaseline;
    private static int _lastCycleCount;
    private static Dictionary<ulong, int>? _voiceCountCache;

    private sealed class PlayReportSnapshot
    {
        public long ItemCarryCount;
        public long DamageToAlly;
        public long MimicEncounterCount;
        public long TimeInStartingVolumeMs;
    }

    internal static void ClearRuntimeState()
    {
        _players.Clear();
        _connectedSince.Clear();
        _cycleBaselines.Clear();
        _voiceEventBaselines.Clear();
        _lastKillCounts.Clear();
        _actorToSteam.Clear();
        _loadedSlotId = -999;
        _lastCurrencyBaseline = 0;
        _lastCycleCount = 0;
    }

    public static void LoadForSlot(int slotId)
    {
        if (!MimesisSaveManager.IsValidSaveSlotId(slotId)) return;
        if (slotId == _loadedSlotId) return;
        _loadedSlotId = slotId;
        _players.Clear();
        _connectedSince.Clear();
        _cycleBaselines.Clear();
        _voiceEventBaselines.Clear();
        _lastKillCounts.Clear();
        _actorToSteam.Clear();
        StatisticsStore.LoadAllPlayersForSlot(slotId, _players);
        ResetCycleBaselines();
        ModLog.Info(Feature, $"Loaded statistics for save slot {slotId} ({_players.Count} players).");
    }

    internal static void HandleArchiveStarted(SpeechEventArchive archive, int slotId)
    {
        if (!ModConfig.EnableStatistics.Value) return;
        if (!MimesisSaveManager.IsValidSaveSlotId(slotId)) return;

        if (slotId != _loadedSlotId)
            LoadForSlot(slotId);

        if (!IsArchiveIdentityReady(archive))
            return;

        ulong steamId = ResolveSteamIdFromArchive(archive);
        if (steamId == 0) return;

        OnPlayerRegistered(steamId, slotId);
    }

    public static void OnPlayerRegistered(ulong steamId, int slotId)
    {
        if (!ModConfig.EnableStatistics.Value) return;
        if (steamId == 0) return;
        if (!MimesisSaveManager.IsValidSaveSlotId(slotId)) return;
        if (_connectedSince.ContainsKey(steamId)) return;

        LoadForSlot(slotId);

        var doc = GetOrCreatePlayer(steamId);
        doc.DisplayName = ResolveDisplayName(steamId, doc.DisplayName);
        var now = DateTime.UtcNow;
        int graceMinutes = ModConfig.SessionReconnectGraceMinutes.Value;

        bool resumeSession = doc.CurrentSession != null
                             && doc.CurrentSession.IsOpen
                             && doc.CurrentSession.LastDisconnectedAtUtc.HasValue
                             && now - doc.CurrentSession.LastDisconnectedAtUtc.Value <= TimeSpan.FromMinutes(graceMinutes);

        if (resumeSession && doc.CurrentSession != null)
        {
            doc.CurrentSession.ReconnectCount++;
            doc.CurrentSession.LastConnectedAtUtc = now;
            doc.CurrentSession.LastDisconnectedAtUtc = null;
            ModLog.Info(Feature, $"Player resumed session — steamId={steamId} session={doc.CurrentSession.SessionId} reconnects={doc.CurrentSession.ReconnectCount}");
        }
        else
        {
            FinalizeOpenSession(doc, countAsCompleted: true);
            doc.CurrentSession = NewSession(now);
            ModLog.Info(Feature, $"Player started session — steamId={steamId} session={doc.CurrentSession.SessionId}");
        }

        _connectedSince[steamId] = now;
        EnsureCycleBaseline(steamId);
        EnsureVoiceBaseline(steamId);
        StatisticsStore.SavePlayer(slotId, doc);
        PersistLeaderboard(slotId);

        bool isNewSession = !resumeSession;
        int reconnectCount = doc.CurrentSession?.ReconnectCount ?? 0;
        StatisticsMessages.OnPlayerJoinedSession(steamId, doc.DisplayName, doc, isNewSession, reconnectCount);
    }

    public static void OnPlayerUnregistered(ulong steamId)
    {
        if (!CanTrack()) return;
        if (steamId == 0) return;

        if (!_players.TryGetValue(steamId, out var doc))
            doc = GetOrCreatePlayer(steamId);

        FlushConnectedTime(steamId, doc);
        _connectedSince.Remove(steamId);
        if (doc.CurrentSession != null)
        {
            doc.CurrentSession.LastDisconnectedAtUtc = DateTime.UtcNow;
            doc.CurrentSession.IsOpen = true;
        }

        ModLog.Info(Feature, $"Player disconnected — steamId={steamId} displayName={doc.DisplayName}");
        StatisticsStore.SavePlayer(_loadedSlotId, doc);
        PersistLeaderboard(_loadedSlotId);
        StatisticsMessages.OnPlayerLeftSession(steamId, doc.DisplayName, doc);
    }

    public static void ProcessDeferred()
    {
        if (!CanTrack()) return;

        int graceMinutes = ModConfig.SessionReconnectGraceMinutes.Value;
        var now = DateTime.UtcNow;
        int slotId = _loadedSlotId;
        bool changed = false;

        foreach (var kvp in _players)
        {
            var doc = kvp.Value;
            var session = doc.CurrentSession;
            if (session == null || !session.IsOpen || !session.LastDisconnectedAtUtc.HasValue)
                continue;

            if (now - session.LastDisconnectedAtUtc.Value <= TimeSpan.FromMinutes(graceMinutes))
                continue;

            ModLog.Info(Feature, $"Session finalized — steamId={kvp.Key} session={session.SessionId} after grace period");
            FinalizeOpenSession(doc, countAsCompleted: true);
            StatisticsStore.SavePlayer(slotId, doc);
            changed = true;
        }

        var connectedSteamIds = new List<ulong>();
        foreach (var kvp in _connectedSince)
            connectedSteamIds.Add(kvp.Key);

        foreach (ulong steamId in connectedSteamIds)
        {
            if (!_players.TryGetValue(steamId, out var doc)) continue;
            if (doc.CurrentSession?.LastDisconnectedAtUtc.HasValue == true)
                continue;
            FlushConnectedTime(steamId, doc);
        }

        if (changed)
            PersistLeaderboard(slotId);
    }

    public static void OnCycleCompleted(PlayReportManager? manager)
    {
        if (!CanTrack()) return;

        int slotId = _loadedSlotId;

        long currencyNow = manager?.AccumulatedCurrency ?? 0;
        long currencyDelta = Math.Max(0, currencyNow - _lastCurrencyBaseline);
        _lastCurrencyBaseline = currencyNow;

        int cycleNumber = manager?.AccumulatedCycleCount ?? ++_lastCycleCount;
        _lastCycleCount = cycleNumber;

        if (currencyDelta > 0)
        {
            ulong hostSteam = ResolveSteamIdFromUid(0, isLocal: true);
            if (hostSteam != 0)
            {
                var hostDoc = GetOrCreatePlayer(hostSteam);
                hostDoc.CurrentSession ??= NewSession(DateTime.UtcNow);
                hostDoc.CurrentSession.Counters.CurrencyEarned += currencyDelta;
                hostDoc.Global.Counters.CurrencyEarned += currencyDelta;
            }
        }

        var playReports = manager?.CurrentReportDict;
        var affected = new HashSet<ulong>();

        if (playReports != null)
        {
            foreach (var kvp in playReports)
            {
                ulong steamId = kvp.Key;
                if (steamId == 0) continue;
                affected.Add(steamId);
                ApplyPlayReportDelta(steamId, kvp.Value, cycleNumber);
            }
        }

        foreach (ulong steamId in _connectedSince.Keys)
            affected.Add(steamId);

        var connectedSteamIds = new List<ulong>();
        foreach (ulong steamId in _connectedSince.Keys)
            connectedSteamIds.Add(steamId);

        foreach (ulong steamId in connectedSteamIds)
        {
            if (_players.TryGetValue(steamId, out var connectedDoc))
                FlushConnectedTime(steamId, connectedDoc);
        }

        _voiceCountCache = BuildVoiceCountCache();
        foreach (ulong steamId in affected)
        {
            var doc = GetOrCreatePlayer(steamId);
            doc.DisplayName = ResolveDisplayName(steamId, doc.DisplayName);
            ApplyVoiceDelta(steamId, doc);
            StatisticsStore.SavePlayer(slotId, doc);
        }
        _voiceCountCache = null;

        ResetCycleBaselines(manager);
        PersistLeaderboard(slotId);

        if (ModConfig.ShowStatisticsToasts.Value)
            StatisticsMessages.OnCycleCompleted(cycleNumber);

        ModLog.Info(Feature, $"Cycle {cycleNumber} statistics saved for slot {slotId} ({affected.Count} players).");
    }

    public static void OnGameSaved(int slotId)
    {
        if (!MimesisSaveManager.IsHost()) return;
        if (!ModConfig.EnableStatistics.Value) return;
        if (!MimesisSaveManager.IsValidSaveSlotId(slotId)) return;

        LoadForSlot(slotId);
        foreach (var doc in _players.Values)
            StatisticsStore.SavePlayer(slotId, doc);
        PersistLeaderboard(slotId);
        ModLog.Debug(Feature, $"Statistics persisted on game save for slot {slotId}.");
    }

    public static void OnPlayerDeath(ProtoActor actor)
    {
        if (!CanTrack() || actor == null) return;
        ulong steamId = ResolveSteamIdFromActor(actor);
        if (steamId == 0) return;

        var doc = GetOrCreatePlayer(steamId);
        doc.CurrentSession ??= NewSession(DateTime.UtcNow);
        doc.CurrentSession.Counters.Deaths++;
        doc.Global.Counters.Deaths++;
        RegisterActorMapping(actor, steamId);
        PersistCombatStats(steamId, doc);
    }

    public static void OnPlayerRevive(ProtoActor actor)
    {
        if (!CanTrack() || actor == null) return;
        ulong steamId = ResolveSteamIdFromActor(actor);
        if (steamId == 0) return;

        var doc = GetOrCreatePlayer(steamId);
        doc.CurrentSession ??= NewSession(DateTime.UtcNow);
        doc.CurrentSession.Counters.Revives++;
        doc.Global.Counters.Revives++;
        RegisterActorMapping(actor, steamId);
        PersistCombatStats(steamId, doc);
    }

    public static void OnKillCountChanged(ProtoActor actor, int killCount)
    {
        if (!CanTrack() || actor == null) return;
        ulong steamId = ResolveSteamIdFromActor(actor);
        if (steamId == 0) return;

        RegisterActorMapping(actor, steamId);
        var doc = GetOrCreatePlayer(steamId);
        int previous = _lastKillCounts.TryGetValue(steamId, out int prev) ? prev : 0;

        if (previous == 0 && killCount <= doc.Global.Counters.Kills)
        {
            _lastKillCounts[steamId] = killCount;
            return;
        }

        if (killCount <= previous)
        {
            _lastKillCounts[steamId] = killCount;
            return;
        }

        int delta = killCount - previous;
        _lastKillCounts[steamId] = killCount;

        doc.CurrentSession ??= NewSession(DateTime.UtcNow);
        doc.CurrentSession.Counters.Kills += delta;
        doc.Global.Counters.Kills += delta;
        PersistCombatStats(steamId, doc);
    }

    public static void OnUpdate()
    {
        if (!CanTrack()) return;
        ProcessDeferred();
    }

    private static void PersistCombatStats(ulong steamId, PlayerStatisticsDocument doc)
    {
        StatisticsStore.SavePlayer(_loadedSlotId, doc);
    }

    private static bool CanTrack() =>
        ModConfig.EnableStatistics.Value
        && MimesisSaveManager.IsHost()
        && _loadedSlotId >= 0
        && MimesisSaveManager.IsValidSaveSlotId(_loadedSlotId);

    private static PlayerStatisticsDocument GetOrCreatePlayer(ulong steamId)
    {
        if (!_players.TryGetValue(steamId, out var doc))
        {
            doc = StatisticsStore.LoadPlayer(_loadedSlotId, steamId);
            doc.SteamId = steamId;
            _players[steamId] = doc;
        }
        return doc;
    }

    internal static PlayerStatisticsDocument? TryGetPlayerDocument(ulong steamId) =>
        _players.TryGetValue(steamId, out var doc) ? doc : null;

    private static SessionStats NewSession(DateTime now) => new()
    {
        SessionId = Guid.NewGuid().ToString("N"),
        StartedAtUtc = now,
        LastConnectedAtUtc = now,
        IsOpen = true,
        Counters = new StatCounters(),
    };

    private static void FinalizeOpenSession(PlayerStatisticsDocument doc, bool countAsCompleted)
    {
        if (doc.CurrentSession == null || !doc.CurrentSession.IsOpen)
            return;

        doc.CurrentSession.IsOpen = false;
        doc.RecentSessions.Add(CloneSession(doc.CurrentSession));
        while (doc.RecentSessions.Count > StatisticsStore.MaxRecentSessionsPerPlayer)
            doc.RecentSessions.RemoveAt(0);

        if (countAsCompleted)
            doc.Global.SessionsCompleted++;

        doc.CurrentSession = null;
    }

    private static SessionStats CloneSession(SessionStats session) => new()
    {
        SessionId = session.SessionId,
        StartedAtUtc = session.StartedAtUtc,
        LastConnectedAtUtc = session.LastConnectedAtUtc,
        LastDisconnectedAtUtc = session.LastDisconnectedAtUtc,
        ReconnectCount = session.ReconnectCount,
        IsOpen = false,
        Counters = session.Counters.Clone(),
    };

    private static void FlushConnectedTime(ulong steamId, PlayerStatisticsDocument doc)
    {
        if (!_connectedSince.TryGetValue(steamId, out DateTime since)) return;
        long seconds = (long)Math.Max(0, (DateTime.UtcNow - since).TotalSeconds);
        if (seconds <= 0) return;

        doc.CurrentSession ??= NewSession(since);
        doc.CurrentSession.Counters.TotalConnectedSeconds += seconds;
        doc.Global.Counters.TotalConnectedSeconds += seconds;
        _connectedSince[steamId] = DateTime.UtcNow;
    }

    private static void ApplyPlayReportDelta(ulong steamId, PlayReportData report, int cycleNumber)
    {
        var doc = GetOrCreatePlayer(steamId);
        doc.CurrentSession ??= NewSession(DateTime.UtcNow);

        if (!_cycleBaselines.TryGetValue(steamId, out var baseline))
        {
            baseline = SnapshotReport(report);
            _cycleBaselines[steamId] = baseline;
        }

        var delta = new StatCounters
        {
            ItemCarryCount = Math.Max(0, report.TotalItemCarryCount - baseline.ItemCarryCount),
            DamageToAlly = Math.Max(0, report.TotalDamageToAlly - baseline.DamageToAlly),
            MimicEncounterCount = Math.Max(0, report.TotalMimicEncounterCount - baseline.MimicEncounterCount),
            TimeInStartingVolumeMs = Math.Max(0, report.TotalTimeInStartingVolume - baseline.TimeInStartingVolumeMs),
            CyclesCompleted = 1,
        };

        MergeDelta(doc, delta);
        _cycleBaselines[steamId] = SnapshotReport(report);
    }

    private static void ApplyVoiceDelta(ulong steamId, PlayerStatisticsDocument doc)
    {
        int current = CountVoiceEventsForSteamId(steamId);
        int baseline = _voiceEventBaselines.TryGetValue(steamId, out int b) ? b : current;
        int delta = Math.Max(0, current - baseline);
        if (delta == 0) return;

        doc.CurrentSession ??= NewSession(DateTime.UtcNow);
        doc.CurrentSession.Counters.VoiceEvents += delta;
        doc.Global.Counters.VoiceEvents += delta;
        _voiceEventBaselines[steamId] = current;
    }

    private static void MergeDelta(PlayerStatisticsDocument doc, StatCounters delta)
    {
        doc.CurrentSession ??= NewSession(DateTime.UtcNow);
        doc.CurrentSession.Counters.Add(delta);
        doc.Global.Counters.Add(delta);
    }

    private static PlayReportSnapshot SnapshotReport(PlayReportData report) => new()
    {
        ItemCarryCount = report.TotalItemCarryCount,
        DamageToAlly = report.TotalDamageToAlly,
        MimicEncounterCount = report.TotalMimicEncounterCount,
        TimeInStartingVolumeMs = report.TotalTimeInStartingVolume,
    };

    private static void EnsureCycleBaseline(ulong steamId)
    {
        if (_cycleBaselines.ContainsKey(steamId)) return;
        var report = TryGetPlayReport(steamId);
        if (report != null)
            _cycleBaselines[steamId] = SnapshotReport(report);
    }

    private static void EnsureVoiceBaseline(ulong steamId)
    {
        if (_voiceEventBaselines.ContainsKey(steamId)) return;
        _voiceEventBaselines[steamId] = CountVoiceEventsForSteamId(steamId);
    }

    private static void ResetCycleBaselines(PlayReportManager? manager = null)
    {
        _cycleBaselines.Clear();
        _voiceEventBaselines.Clear();
        manager ??= TryGetPlayReportManager();
        _lastCurrencyBaseline = manager?.AccumulatedCurrency ?? _lastCurrencyBaseline;
        _lastCycleCount = manager?.AccumulatedCycleCount ?? _lastCycleCount;

        var dict = manager?.CurrentReportDict;
        if (dict == null) return;
        foreach (var kvp in dict)
        {
            if (kvp.Key == 0) continue;
            _cycleBaselines[kvp.Key] = SnapshotReport(kvp.Value);
            _voiceEventBaselines[kvp.Key] = CountVoiceEventsForSteamId(kvp.Key);
        }
    }

    private static PlayReportData? TryGetPlayReport(ulong steamId)
    {
        var dict = TryGetPlayReportManager()?.CurrentReportDict;
        if (dict == null) return null;
        return dict.TryGetValue(steamId, out var report) ? report : null;
    }

    private static PlayReportManager? TryGetPlayReportManager()
    {
        try
        {
            object? vworld = typeof(Hub).GetField("vworld", InstanceMemberFlags)?.GetValue(Hub.s)
                             ?? typeof(Hub).GetProperty("vworld", InstanceMemberFlags)?.GetValue(Hub.s);
            if (vworld == null) return null;

            object? roomManager = vworld.GetType().GetField("_vRoomManager", InstanceMemberFlags)?.GetValue(vworld);
            if (roomManager == null) return null;

            object? sessionInfo = roomManager.GetType().GetField("_gameSessionInfo", InstanceMemberFlags)?.GetValue(roomManager);
            if (sessionInfo == null) return null;

            return sessionInfo.GetType().GetProperty("PlayReportManager", InstanceMemberFlags)?.GetValue(sessionInfo) as PlayReportManager;
        }
        catch
        {
            return null;
        }
    }

    private static Dictionary<ulong, int> BuildVoiceCountCache()
    {
        var cache = new Dictionary<ulong, int>();
        try
        {
            var archives = UnityEngine.Object.FindObjectsByType<SpeechEventArchive>(FindObjectsSortMode.None);
            if (archives == null) return cache;

            foreach (var archive in archives)
            {
                if (archive == null) continue;
                long playerUid = 0;
                bool isLocal = false;
                try
                {
                    playerUid = archive.PlayerUID;
                    isLocal = archive.IsLocal;
                }
                catch { /* not ready */ }

                ulong archiveSteam = ResolveSteamIdFromUid(playerUid, isLocal);
                if (archiveSteam == 0) continue;
                cache.TryGetValue(archiveSteam, out int current);
                cache[archiveSteam] = current + VoiceEventStats.GetEventCount(archive);
            }
        }
        catch { /* ignore */ }

        return cache;
    }

    private static int CountVoiceEventsForSteamId(ulong steamId)
    {
        if (_voiceCountCache != null && _voiceCountCache.TryGetValue(steamId, out int cached))
            return cached;

        int total = 0;
        try
        {
            var archives = UnityEngine.Object.FindObjectsByType<SpeechEventArchive>(FindObjectsSortMode.None);
            if (archives == null) return 0;

            foreach (var archive in archives)
            {
                if (archive == null) continue;
                long playerUid = 0;
                bool isLocal = false;
                try
                {
                    playerUid = archive.PlayerUID;
                    isLocal = archive.IsLocal;
                }
                catch { /* not ready */ }

                ulong archiveSteam = ResolveSteamIdFromUid(playerUid, isLocal);
                if (archiveSteam != steamId) continue;
                total += VoiceEventStats.GetEventCount(archive);
            }
        }
        catch { /* ignore */ }

        return total;
    }

    private static ulong ResolveSteamIdFromArchive(SpeechEventArchive archive)
    {
        long playerUid = 0;
        bool isLocal = false;
        try
        {
            playerUid = archive.PlayerUID;
            isLocal = archive.IsLocal;
        }
        catch
        {
            return 0;
        }

        if (!isLocal && playerUid == 0)
        {
            try
            {
                if (string.IsNullOrEmpty(archive.PlayerId))
                    return 0;
            }
            catch
            {
                return 0;
            }
        }

        return ResolveSteamIdFromUid(playerUid, isLocal);
    }

    private static bool IsArchiveIdentityReady(SpeechEventArchive archive)
    {
        long playerUid = 0;
        bool isLocal = false;
        try
        {
            playerUid = archive.PlayerUID;
            isLocal = archive.IsLocal;
        }
        catch
        {
            return false;
        }

        if (!isLocal && playerUid == 0)
        {
            try
            {
                return !string.IsNullOrEmpty(archive.PlayerId);
            }
            catch
            {
                return false;
            }
        }

        return true;
    }

    private static ulong ResolveSteamIdFromActor(ProtoActor actor)
    {
        if (actor.steamID != 0)
            return actor.steamID;

        if (actor.UID != 0)
        {
            ulong fromUid = ResolveSteamIdFromUid(actor.UID, actor.IsHost);
            if (fromUid != 0)
                return fromUid;
        }

        if (actor.ActorID != 0 && _actorToSteam.TryGetValue(actor.ActorID, out ulong mapped))
            return mapped;

        return 0;
    }

    private static void RegisterActorMapping(ProtoActor actor, ulong steamId)
    {
        if (actor.ActorID != 0)
            _actorToSteam[actor.ActorID] = steamId;
    }

    private static ulong ResolveSteamIdFromUid(long playerUid, bool isLocal)
    {
        try
        {
            object? pdata = typeof(Hub).GetField("pdata", InstanceMemberFlags)?.GetValue(Hub.s);
            var field = pdata?.GetType().GetField("actorUIDToSteamID", InstanceMemberFlags);
            if (field?.GetValue(pdata) is Dictionary<long, ulong> dict
                && dict.TryGetValue(playerUid, out ulong steamId))
            {
                return steamId;
            }
        }
        catch { /* ignore */ }

        if (isLocal)
        {
            try
            {
                var platformMgr = MonoSingleton<PlatformMgr>.Instance;
                var pathField = typeof(PlatformMgr).GetField("_uniqueUserPath", InstanceMemberFlags);
                string? userPath = pathField?.GetValue(platformMgr) as string;
                if (!string.IsNullOrEmpty(userPath) && ulong.TryParse(userPath, out ulong localSteam))
                    return localSteam;
            }
            catch { /* ignore */ }
        }

        return 0;
    }

    private static string ResolveDisplayName(ulong steamId, string fallback)
    {
        try
        {
            object? pdata = typeof(Hub).GetField("pdata", InstanceMemberFlags)?.GetValue(Hub.s);
            object? main = pdata?.GetType().GetField("main", InstanceMemberFlags)?.GetValue(pdata);
            if (main != null)
            {
                var cacheField = main.GetType().GetField("steamIDToNameCache", InstanceMemberFlags);
                if (cacheField?.GetValue(main) is Dictionary<ulong, string> cache
                    && cache.TryGetValue(steamId, out string? name)
                    && !string.IsNullOrWhiteSpace(name))
                {
                    return name;
                }
            }

            if (pdata?.GetType().GetField("MyNickName", InstanceMemberFlags)?.GetValue(pdata) is string myNick
                && !string.IsNullOrWhiteSpace(myNick))
            {
                ulong localSteam = ResolveSteamIdFromUid(0, isLocal: true);
                if (localSteam == steamId)
                    return myNick;
            }
        }
        catch { /* ignore */ }

        return string.IsNullOrWhiteSpace(fallback) ? steamId.ToString() : fallback;
    }

    private static void PersistLeaderboard(int slotId)
    {
        var leaderboard = LeaderboardBuilder.Build(slotId, _players.Values);
        StatisticsStore.SaveLeaderboard(slotId, leaderboard);
    }
}
