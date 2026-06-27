using System.Collections.Generic;
using System.Net;
using System.Reflection;
using Mimic.Actors;
using Mimic.Voice.SpeechSystem;

namespace MimesisPlayerEnhancement;

public static class VoiceEventStats
{
    private const BindingFlags InstanceMemberFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static int GetEventCount(SpeechEventArchive? archive)
    {
        if (archive == null)
            return 0;

        try
        {
            return archive.events?.Count ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// In-game display name (Steam/session nickname), not the voice-comms UUID.
    /// Mirrors <c>UIPrefab_InGameMenu.ResolveNickName</c>.
    /// </summary>
    public static string ResolveDisplayName(SpeechEventArchive? archive, long playerUid, bool isLocal)
    {
        if (archive != null)
        {
            try
            {
                ProtoActor? proto = archive.Player?.ProtoActorCache;
                if (proto != null && !string.IsNullOrWhiteSpace(proto.nickName))
                    return proto.nickName;
            }
            catch
            {
                /* Player / voice component may not be ready */
            }
        }

        if (playerUid != 0)
        {
            string? fromMap = ResolveNickNameFromActorMap(playerUid);
            if (!string.IsNullOrWhiteSpace(fromMap))
                return fromMap;
        }

        if (isLocal)
        {
            string? hostNick = GetHostNickName();
            if (!string.IsNullOrWhiteSpace(hostNick))
                return hostNick;
        }

        return "(pending)";
    }

    /// <summary>
    /// Voice-comms identifier (Dissonance / syncedCommsPlayerName). Used internally for persistence matching.
    /// </summary>
    public static string GetVoiceId(SpeechEventArchive? archive)
    {
        if (archive == null)
            return "?";

        try
        {
            string? voiceId = archive.PlayerId;
            return string.IsNullOrEmpty(voiceId) ? "(pending)" : voiceId;
        }
        catch
        {
            return "(unavailable)";
        }
    }

    public static string DescribePlayer(SpeechEventArchive? archive)
    {
        if (archive == null)
            return "archive=null";

        long playerUid = 0;
        bool isLocal = false;

        try
        {
            playerUid = archive.PlayerUID;
            isLocal = archive.IsLocal;
        }
        catch
        {
            /* Player component may not be ready yet */
        }

        int count = GetEventCount(archive);
        string role = isLocal ? "host" : "client";
        string uid = playerUid == 0 ? "(pending)" : playerUid.ToString();
        string name = ResolveDisplayName(archive, playerUid, isLocal);
        SessionContext? session = FindSessionContext(playerUid, 0);
        ulong steamIdValue = ResolveSteamId(playerUid, isLocal, session);
        if (session == null && steamIdValue != 0)
            session = FindSessionContext(playerUid, steamIdValue);
        if (session != null && steamIdValue == 0)
            steamIdValue = ResolveSteamId(playerUid, isLocal, session);
        string steamId = FormatSteamId(steamIdValue);
        string ip = ResolveConnectionAddress(isLocal, session);
        return $"uid={uid} name={name} role={role} steamId={steamId} ip={ip} voiceEvents={count}";
    }

    /// <summary>Same as <see cref="DescribePlayer"/> plus voice-comms UUID (debug only).</summary>
    public static string DescribePlayerVerbose(SpeechEventArchive? archive)
    {
        string summary = DescribePlayer(archive);
        if (archive == null)
            return summary;

        long playerUid = 0;
        try { playerUid = archive.PlayerUID; } catch { /* Player not ready */ }

        SessionContext? session = FindSessionContext(playerUid, 0);
        string serverId = "(unknown)";
        try
        {
            if (session != null)
                serverId = session.ServerID.ToString();
        }
        catch
        {
            /* Session may be unavailable */
        }

        return $"{summary} voiceId={GetVoiceId(archive)} serverId={serverId}";
    }

    private static string FormatSteamId(ulong steamId) =>
        steamId == 0 ? "(pending)" : steamId.ToString();

    /// <summary>
    /// Resolve SteamID for a player.
    /// Prefers the live session context, then actorUIDToSteamID, then the host's PlatformMgr path.
    /// </summary>
    private static ulong ResolveSteamId(long playerUid, bool isLocal, SessionContext? session)
    {
        if (session != null)
        {
            try
            {
                ulong fromSession = session.SteamID;
                if (fromSession != 0)
                    return fromSession;
            }
            catch
            {
                /* Session may be tearing down */
            }
        }

        if (playerUid != 0)
        {
            try
            {
                object? pdata = GetHubMember("pdata");
                var field = pdata?.GetType().GetField("actorUIDToSteamID", InstanceMemberFlags);
                if (field?.GetValue(pdata) is Dictionary<long, ulong> dict
                    && dict.TryGetValue(playerUid, out ulong steamId))
                {
                    return steamId;
                }
            }
            catch
            {
                /* Hub / actor map may be unavailable */
            }
        }

        if (isLocal)
            return GetLocalSteamId();

        return 0;
    }

    private static ulong GetLocalSteamId()
    {
        try
        {
            var platformMgr = MonoSingleton<PlatformMgr>.Instance;
            if (platformMgr == null)
                return 0;

            var field = typeof(PlatformMgr).GetField("_uniqueUserPath", InstanceMemberFlags);
            string? userPath = field?.GetValue(platformMgr) as string;
            if (!string.IsNullOrEmpty(userPath) && ulong.TryParse(userPath, out ulong steamId))
                return steamId;
        }
        catch
        {
            /* PlatformMgr may be unavailable during teardown */
        }

        return 0;
    }

    /// <summary>
    /// Best-effort remote address. Only the host/server typically has peer endpoints.
    /// Steam SDR / relay connections may not expose a public IP.
    /// </summary>
    private static string ResolveConnectionAddress(bool isLocal, SessionContext? session)
    {
        if (isLocal)
            return "local";

        if (session == null)
            return "(unavailable)";

        try
        {
            ISession? netSession = session.Session;
            IPEndPoint? endpoint = netSession?.GetRemoteEndPoint();
            if (endpoint != null)
            {
                string address = endpoint.Address.ToString();
                if (endpoint.Port > 0)
                    return $"{address}:{endpoint.Port}";

                return address;
            }
        }
        catch
        {
            /* Session / transport may not be ready yet */
        }

        try
        {
            if (session.IsSDRLink)
                return "steam-sdr";
        }
        catch
        {
            /* Session may be tearing down */
        }

        return "(unavailable)";
    }

    private static SessionContext? FindSessionContext(long playerUid, ulong steamId)
    {
        SessionManager? sessionManager = GetSessionManager();
        if (sessionManager == null)
            return null;

        try
        {
            var hostField = typeof(SessionManager).GetField("_hostSessionContext", InstanceMemberFlags);
            if (hostField?.GetValue(sessionManager) is SessionContext host
                && MatchesSessionContext(host, playerUid, steamId))
            {
                return host;
            }

            var contextsField = typeof(SessionManager).GetField("m_Contexts", InstanceMemberFlags);
            if (contextsField?.GetValue(sessionManager) is Dictionary<long, SessionContext> contexts)
            {
                foreach (SessionContext context in contexts.Values)
                {
                    if (MatchesSessionContext(context, playerUid, steamId))
                        return context;
                }
            }
        }
        catch
        {
            /* Session manager may be unavailable during teardown */
        }

        return null;
    }

    private static bool MatchesSessionContext(SessionContext context, long playerUid, ulong steamId)
    {
        if (context == null)
            return false;

        try
        {
            if (playerUid != 0 && context.GetPlayerUID() == playerUid)
                return true;

            if (steamId != 0 && context.SteamID == steamId)
                return true;
        }
        catch
        {
            /* Context may be mid-setup or disposed */
        }

        return false;
    }

    private static SessionManager? GetSessionManager()
    {
        try
        {
            object? vworld = GetHubMember("vworld");
            if (vworld == null)
                return null;

            var field = vworld.GetType().GetField("_sessionManager", InstanceMemberFlags);
            return field?.GetValue(vworld) as SessionManager;
        }
        catch
        {
            return null;
        }
    }

    private static string? ResolveNickNameFromActorMap(long playerUid)
    {
        try
        {
            object? main = GetGameMain();
            if (main == null)
                return null;

            var getMap = main.GetType().GetMethod("GetProtoActorMap", InstanceMemberFlags);
            if (getMap?.Invoke(main, null) is not System.Collections.Generic.Dictionary<int, ProtoActor> map)
                return null;

            foreach (ProtoActor? actor in map.Values)
            {
                if (actor == null || actor.UID != playerUid)
                    continue;

                return string.IsNullOrWhiteSpace(actor.nickName) ? null : actor.nickName;
            }
        }
        catch
        {
            /* Hub / actor map may be unavailable during teardown */
        }

        return null;
    }

    private static string? GetHostNickName()
    {
        try
        {
            object? main = GetGameMain();
            if (main != null)
            {
                var getHostNick = main.GetType().GetMethod("GetHostActorNickName", InstanceMemberFlags);
                if (getHostNick?.Invoke(main, null) is string hostNick && !string.IsNullOrWhiteSpace(hostNick))
                    return hostNick;
            }

            object? pdata = GetHubMember("pdata");
            if (pdata == null)
                return null;

            var myNickField = pdata.GetType().GetField("MyNickName", InstanceMemberFlags);
            if (myNickField?.GetValue(pdata) is string myNick && !string.IsNullOrWhiteSpace(myNick))
                return myNick;
        }
        catch
        {
            /* Hub may be unavailable */
        }

        return null;
    }

    private static object? GetGameMain()
    {
        object? pdata = GetHubMember("pdata");
        if (pdata == null)
            return null;

        var mainField = pdata.GetType().GetField("main", InstanceMemberFlags);
        return mainField?.GetValue(pdata);
    }

    private static object? GetHubMember(string name)
    {
        if (Hub.s == null)
            return null;

        var hubType = typeof(Hub);
        var field = hubType.GetField(name, InstanceMemberFlags);
        if (field != null)
            return field.GetValue(Hub.s);

        var prop = hubType.GetProperty(name, InstanceMemberFlags);
        if (prop != null && prop.CanRead)
            return prop.GetValue(Hub.s);

        return null;
    }
}
