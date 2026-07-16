using System.Net;
using System.Reflection;
using FishNet.Object.Synchronizing;

namespace MimesisPlayerEnhancement
{
    public sealed class PlayerConnectionInfo
    {
        public long PlayerUid;
        public string DisplayName = "";
        public string ConnectionRole = "";
        public ulong SteamId;
        public string ConnectionAddress = "";
        public int VoiceLineCount;
    }

    public static class VoiceEventStats
    {
        private const BindingFlags InstanceMemberFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static int GetVoiceLineCount(SpeechEventArchive? archive)
        {
            if (archive == null)
            {
                return 0;
            }

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
                    {
                        return proto.nickName;
                    }
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
                {
                    return fromMap;
                }
            }

            if (isLocal)
            {
                string? hostNick = GetHostNickName();
                if (!string.IsNullOrWhiteSpace(hostNick))
                {
                    return hostNick;
                }
            }

            return "(pending)";
        }

        /// <summary>
        /// Voice-comms identifier (Dissonance / syncedCommsPlayerName). Used internally for persistence matching.
        /// </summary>
        public static string GetVoiceId(SpeechEventArchive? archive)
        {
            if (archive == null)
            {
                return "?";
            }

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
            {
                return "archive=null";
            }

            return TryGetConnectionInfo(archive, out PlayerConnectionInfo info)
                ? FormatLifecycleIdentity(archive, info)
                : "archive=unavailable";
        }

        internal static string FormatLifecycleIdentity(SpeechEventArchive? archive, PlayerConnectionInfo info)
        {
            return FormatFull(info, ResolveDisplayVoiceId(archive, info.SteamId));
        }

        internal static string ResolveDisplayVoiceId(SpeechEventArchive? archive, ulong steamId = 0)
        {
            string fromArchive = GetVoiceId(archive);
            if (fromArchive is not "(pending)" and not "(unavailable)" and not "?")
            {
                return fromArchive;
            }

            if (steamId != 0 && PlayerRegistry.TryGetVoiceId(steamId, out string mapped) && !string.IsNullOrEmpty(mapped))
            {
                return mapped;
            }

            if (archive?.events != null)
            {
                string? fromEvents = ResolveDominantEventPlayerName(archive);
                if (!string.IsNullOrEmpty(fromEvents))
                {
                    return fromEvents;
                }
            }

            return fromArchive;
        }

        private static string? ResolveDominantEventPlayerName(SpeechEventArchive archive)
        {
            try
            {
                SyncList<SpeechEvent>? events = archive.events;
                if (events == null || events.Count == 0)
                {
                    return null;
                }

                Dictionary<string, int> counts = [];
                string? dominant = null;
                int max = 0;
                for (int i = 0; i < events.Count; i++)
                {
                    SpeechEvent? ev = events[i];
                    if (ev == null || string.IsNullOrEmpty(ev.PlayerName))
                    {
                        continue;
                    }

                    counts.TryGetValue(ev.PlayerName, out int count);
                    count++;
                    counts[ev.PlayerName] = count;
                    if (count > max)
                    {
                        max = count;
                        dominant = ev.PlayerName;
                    }
                }

                return dominant;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Short player tag for feature debug/operational logs (name + steamId).</summary>
        public static string DescribePlayerBrief(SpeechEventArchive? archive)
        {
            if (archive == null)
            {
                return "player=unknown";
            }

            return TryGetConnectionInfo(archive, out PlayerConnectionInfo info)
                ? FormatBrief(info)
                : "player=unavailable";
        }

        /// <summary>
        /// Best-effort identity from a live archive. Prefer this at disconnect prefix time before teardown clears fields.
        /// </summary>
        public static bool TryCaptureArchiveIdentity(
            SpeechEventArchive? archive,
            out long playerUid,
            out bool isLocal,
            out ulong steamId)
        {
            playerUid = 0;
            isLocal = false;
            steamId = 0;

            if (archive == null)
            {
                return false;
            }

            try
            {
                playerUid = archive.PlayerUID;
                isLocal = archive.IsLocal;
            }
            catch
            {
                return false;
            }

            steamId = GameSessionAccess.ResolveSteamId(playerUid, isLocal);
            if (steamId == 0 && TryGetConnectionInfo(archive, out PlayerConnectionInfo info))
            {
                if (info.SteamId != 0)
                {
                    steamId = info.SteamId;
                }

                if (playerUid == 0 && info.PlayerUid != 0)
                {
                    playerUid = info.PlayerUid;
                }
            }

            return steamId != 0 || playerUid != 0;
        }

        public static string DescribeSteamPlayer(ulong steamId, long playerUid = 0, SpeechEventArchive? archive = null)
        {
            SessionContext? session = FindSessionContext(playerUid, steamId);
            if (playerUid == 0 && session != null)
            {
                try { playerUid = session.GetPlayerUID(); } catch { /* mid-setup */ }
            }

            if (playerUid == 0 && steamId != 0)
            {
                playerUid = ResolvePlayerUidFromSteamId(steamId);
            }

            bool isLocal = steamId != 0 && steamId == GameSessionAccess.GetLocalSteamId();
            if (TryGetConnectionInfo(session, playerUid, steamId, isLocal, out PlayerConnectionInfo info))
            {
                if (archive != null)
                {
                    info.VoiceLineCount = GetVoiceLineCount(archive);
                }

                return FormatFull(info, ResolveDisplayVoiceId(archive, steamId));
            }

            return $"steamId={steamId} uid={(playerUid == 0 ? "(pending)" : playerUid.ToString())}";
        }

        private static string FormatBrief(PlayerConnectionInfo info)
        {
            string steamId = info.SteamId == 0 ? "(pending)" : info.SteamId.ToString();
            return $"name={info.DisplayName} steamId={steamId}";
        }

        private static string FormatFull(PlayerConnectionInfo info, string voiceId)
        {
            string uid = info.PlayerUid == 0 ? "(pending)" : info.PlayerUid.ToString();
            string steamId = info.SteamId == 0 ? "(pending)" : info.SteamId.ToString();
            return $"uid={uid} name={info.DisplayName} role={info.ConnectionRole} steamId={steamId} ip={info.ConnectionAddress} voiceId={voiceId} voiceLines={info.VoiceLineCount}";
        }

        public static bool TryGetConnectionInfo(SpeechEventArchive? archive, out PlayerConnectionInfo info)
        {
            return TryGetConnectionInfo(archive, null, 0, out info);
        }

        public static bool TryGetConnectionInfo(
            SpeechEventArchive? archive,
            SessionContext? knownContext,
            ulong steamIdHint,
            out PlayerConnectionInfo info)
        {
            info = new PlayerConnectionInfo();
            if (archive == null)
            {
                return false;
            }

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

            SessionContext? session = knownContext;
            ulong steamIdValue = steamIdHint;

            if (session != null)
            {
                try
                {
                    if (steamIdValue == 0)
                    {
                        steamIdValue = session.SteamID;
                    }

                    if (playerUid == 0)
                    {
                        playerUid = session.GetPlayerUID();
                    }
                }
                catch
                {
                    /* Context may be mid-setup */
                }
            }

            if (steamIdValue == 0)
            {
                steamIdValue = ResolveSteamId(playerUid, isLocal, session);
            }

            if (session == null)
            {
                session = FindSessionContext(playerUid, steamIdValue);
            }

            if (session != null && steamIdValue == 0)
            {
                steamIdValue = ResolveSteamId(playerUid, isLocal, session);
            }

            if (session != null && playerUid == 0)
            {
                try
                {
                    long fromSession = session.GetPlayerUID();
                    if (fromSession != 0)
                    {
                        playerUid = fromSession;
                    }
                }
                catch
                {
                    /* Context may be mid-setup */
                }
            }

            if (playerUid == 0 && steamIdValue != 0)
            {
                playerUid = ResolvePlayerUidFromSteamId(steamIdValue);
            }

            return TryBuildConnectionInfo(
                archive,
                playerUid,
                isLocal,
                steamIdValue,
                session,
                out info);
        }

        public static bool TryGetConnectionInfo(
            SessionContext? session,
            long playerUid,
            ulong steamId,
            bool isLocal,
            out PlayerConnectionInfo info)
        {
            return TryBuildConnectionInfo(
                null,
                playerUid,
                isLocal,
                steamId,
                session,
                out info);
        }

        private static bool TryBuildConnectionInfo(
            SpeechEventArchive? archive,
            long playerUid,
            bool isLocal,
            ulong steamIdValue,
            SessionContext? session,
            out PlayerConnectionInfo info)
        {
            info = new PlayerConnectionInfo
            {
                PlayerUid = playerUid,
                DisplayName = ResolveDisplayName(archive, playerUid, isLocal),
                ConnectionRole = isLocal ? "host" : "client",
                SteamId = steamIdValue,
                ConnectionAddress = ResolveConnectionAddress(isLocal, session),
                VoiceLineCount = GetVoiceLineCount(archive),
            };

            return true;
        }

        private static long ResolvePlayerUidFromSteamId(ulong steamId)
        {
            if (steamId == 0)
            {
                return 0;
            }

            try
            {
                object? pdata = GetHubMember("pdata");
                FieldInfo? field = pdata?.GetType().GetField("actorUIDToSteamID", InstanceMemberFlags);
                if (field?.GetValue(pdata) is Dictionary<long, ulong> dict)
                {
                    foreach (KeyValuePair<long, ulong> kvp in dict)
                    {
                        if (kvp.Value == steamId)
                        {
                            return kvp.Key;
                        }
                    }
                }
            }
            catch
            {
                /* Hub / actor map may be unavailable */
            }

            SessionManager? sessionManager = GetSessionManager();
            if (sessionManager == null)
            {
                return 0;
            }

            try
            {
                FieldInfo hostField = typeof(SessionManager).GetField("_hostSessionContext", InstanceMemberFlags);
                if (hostField?.GetValue(sessionManager) is SessionContext host && host.SteamID == steamId)
                {
                    return host.GetPlayerUID();
                }

                FieldInfo contextsField = typeof(SessionManager).GetField("m_Contexts", InstanceMemberFlags);
                if (contextsField?.GetValue(sessionManager) is Dictionary<long, SessionContext> contexts)
                {
                    foreach (SessionContext context in contexts.Values)
                    {
                        if (context.SteamID != steamId)
                        {
                            continue;
                        }

                        return context.GetPlayerUID();
                    }
                }
            }
            catch
            {
                /* Session manager may be unavailable */
            }

            return 0;
        }

        /// <summary>
        /// Resolve SteamID for a player.
        /// Prefers the live session context, then the shared session-access lookup.
        /// </summary>
        private static ulong ResolveSteamId(long playerUid, bool isLocal, SessionContext? session)
        {
            if (session != null)
            {
                try
                {
                    ulong fromSession = session.SteamID;
                    if (fromSession != 0)
                    {
                        return fromSession;
                    }
                }
                catch
                {
                    /* Session may be tearing down */
                }
            }

            return GameSessionAccess.ResolveSteamId(playerUid, isLocal);
        }

        /// <summary>
        /// Best-effort remote address. Only the host/server typically has peer endpoints.
        /// Steam SDR / relay connections may not expose a public IP.
        /// </summary>
        private static string ResolveConnectionAddress(bool isLocal, SessionContext? session)
        {
            if (isLocal)
            {
                return "local";
            }

            if (session == null)
            {
                return "(unavailable)";
            }

            try
            {
                ISession? netSession = session.Session;
                IPEndPoint? endpoint = netSession?.GetRemoteEndPoint();
                if (endpoint != null)
                {
                    string address = endpoint.Address.ToString();
                    return endpoint.Port > 0 ? $"{address}:{endpoint.Port}" : address;
                }
            }
            catch
            {
                /* Session / transport may not be ready yet */
            }

            try
            {
                if (session.IsSDRLink)
                {
                    return "steam-sdr";
                }
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
            {
                return null;
            }

            try
            {
                FieldInfo hostField = typeof(SessionManager).GetField("_hostSessionContext", InstanceMemberFlags);
                if (hostField?.GetValue(sessionManager) is SessionContext host
                    && MatchesSessionContext(host, playerUid, steamId))
                {
                    return host;
                }

                FieldInfo contextsField = typeof(SessionManager).GetField("m_Contexts", InstanceMemberFlags);
                if (contextsField?.GetValue(sessionManager) is Dictionary<long, SessionContext> contexts)
                {
                    foreach (SessionContext context in contexts.Values)
                    {
                        if (MatchesSessionContext(context, playerUid, steamId))
                        {
                            return context;
                        }
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
            {
                return false;
            }

            try
            {
                if (playerUid != 0 && context.GetPlayerUID() == playerUid)
                {
                    return true;
                }

                if (steamId != 0 && context.SteamID == steamId)
                {
                    return true;
                }
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
                {
                    return null;
                }

                FieldInfo field = vworld.GetType().GetField("_sessionManager", InstanceMemberFlags);
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
                {
                    return null;
                }

                MethodInfo getMap = main.GetType().GetMethod("GetProtoActorMap", InstanceMemberFlags);
                if (getMap?.Invoke(main, null) is not Dictionary<int, ProtoActor> map)
                {
                    return null;
                }

                List<ProtoActor?> actors = [.. map.Values];
                foreach (ProtoActor? actor in actors)
                {
                    if (actor == null || actor.UID != playerUid)
                    {
                        continue;
                    }

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
                    MethodInfo getHostNick = main.GetType().GetMethod("GetHostActorNickName", InstanceMemberFlags);
                    if (getHostNick?.Invoke(main, null) is string hostNick && !string.IsNullOrWhiteSpace(hostNick))
                    {
                        return hostNick;
                    }
                }

                object? pdata = GetHubMember("pdata");
                if (pdata == null)
                {
                    return null;
                }

                FieldInfo myNickField = pdata.GetType().GetField("MyNickName", InstanceMemberFlags);
                if (myNickField?.GetValue(pdata) is string myNick && !string.IsNullOrWhiteSpace(myNick))
                {
                    return myNick;
                }
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
            {
                return null;
            }

            FieldInfo mainField = pdata.GetType().GetField("main", InstanceMemberFlags);
            return mainField?.GetValue(pdata);
        }

        private static object? GetHubMember(string name)
        {
            if (Hub.s == null)
            {
                return null;
            }

            Type hubType = typeof(Hub);
            FieldInfo field = hubType.GetField(name, InstanceMemberFlags);
            if (field != null)
            {
                return field.GetValue(Hub.s);
            }

            PropertyInfo prop = hubType.GetProperty(name, InstanceMemberFlags);
            return prop != null && prop.CanRead ? prop.GetValue(Hub.s) : null;
        }
    }
}
