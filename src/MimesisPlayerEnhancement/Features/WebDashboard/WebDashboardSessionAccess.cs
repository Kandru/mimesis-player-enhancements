using System.Reflection;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardSessionAccess
    {
        private const BindingFlags InstanceMemberFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo? HostSessionContextField =
            typeof(SessionManager).GetField("_hostSessionContext", InstanceMemberFlags);

        private static readonly FieldInfo? ContextsField =
            typeof(SessionManager).GetField("m_Contexts", InstanceMemberFlags);

        private static readonly FieldInfo? NetworkGradesField =
            typeof(SessionManager).GetField("_networkGrades", InstanceMemberFlags);

        private static readonly FieldInfo? BannedSteamIdsField =
            typeof(SessionManager).GetField("_bannedSteamIDs", InstanceMemberFlags);

        private static readonly FieldInfo? SessionVPlayerField =
            typeof(SessionContext).GetField("_vPlayer", InstanceMemberFlags);

        private static readonly FieldInfo? EnterPktHashCodeField =
            typeof(SessionContext).GetField("_enterPktHashCode", InstanceMemberFlags);

        internal static SessionManager? GetSessionManager()
        {
            try
            {
                if (Hub.s == null)
                {
                    return null;
                }

                VWorld? vworld = GameSessionAccess.TryGetVWorld();
                if (vworld == null)
                {
                    return null;
                }

                FieldInfo? field = typeof(VWorld).GetField("_sessionManager", InstanceMemberFlags);
                return field?.GetValue(vworld) as SessionManager;
            }
            catch
            {
                return null;
            }
        }

        internal static IEnumerable<SessionContext> EnumerateSessionContexts(SessionManager sessionManager)
        {
            HashSet<SessionContext> seen = [];

            if (HostSessionContextField?.GetValue(sessionManager) is SessionContext host
                && host != null
                && seen.Add(host))
            {
                yield return host;
            }

            if (ContextsField?.GetValue(sessionManager) is Dictionary<long, SessionContext> contexts)
            {
                List<SessionContext> sessionContexts = [.. contexts.Values];
                foreach (SessionContext context in sessionContexts)
                {
                    if (context != null && seen.Add(context))
                    {
                        yield return context;
                    }
                }
            }
        }

        internal static VPlayer? GetVPlayer(SessionContext context)
        {
            return SessionVPlayerField?.GetValue(context) as VPlayer;
        }

        internal static bool TryGetPlayerByUid(long uid, out VPlayer? player)
        {
            player = null;
            if (uid == 0)
            {
                return false;
            }

            SessionManager? sessionManager = GetSessionManager();
            if (sessionManager == null)
            {
                return false;
            }

            foreach (SessionContext context in EnumerateSessionContexts(sessionManager))
            {
                if (context.GetPlayerUID() != uid)
                {
                    continue;
                }

                VPlayer? resolved = GetVPlayer(context);
                if (resolved != null)
                {
                    player = resolved;
                    return true;
                }
            }

            return false;
        }

        internal static bool TryGetHostPlayerUid(out long hostUid)
        {
            hostUid = 0;
            SessionManager? sessionManager = GetSessionManager();
            if (sessionManager == null)
            {
                return false;
            }

            foreach (SessionContext context in EnumerateSessionContexts(sessionManager))
            {
                VPlayer? player = GetVPlayer(context);
                if (player != null && player.IsHost)
                {
                    hostUid = player.UID;
                    return true;
                }
            }

            return false;
        }

        internal static int GetEnterPktHashCode(SessionContext context)
        {
            return EnterPktHashCodeField?.GetValue(context) is int hashCode ? hashCode : context.EnterPktHashCode;
        }

        internal static SessionContext? FindHostSessionContext(SessionManager sessionManager)
        {
            foreach (SessionContext context in EnumerateSessionContexts(sessionManager))
            {
                VPlayer? player = GetVPlayer(context);
                if (player != null && player.IsHost)
                {
                    return context;
                }
            }

            return HostSessionContextField?.GetValue(sessionManager) as SessionContext;
        }

        internal static bool TryGetNetworkGrade(SessionManager sessionManager, long playerUid, out int grade)
        {
            grade = -1;
            if (playerUid == 0 || NetworkGradesField?.GetValue(sessionManager) is not System.Collections.IDictionary grades)
            {
                return false;
            }

            try
            {
                foreach (System.Collections.DictionaryEntry entry in grades)
                {
                    if (!MatchesPlayerUid(entry.Key, playerUid) || entry.Value == null)
                    {
                        continue;
                    }

                    if (TryConvertGradeValue(entry.Value, out grade))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                /* grades dictionary may be mid-update */
            }

            return false;
        }

        private static bool MatchesPlayerUid(object key, long playerUid)
        {
            return key switch
            {
                long longKey => longKey == playerUid,
                int intKey => intKey == playerUid,
                _ => false,
            };
        }

        private static bool TryConvertGradeValue(object value, out int grade)
        {
            switch (value)
            {
                case int intGrade:
                    grade = intGrade;
                    return true;
                case long longGrade:
                    grade = (int)longGrade;
                    return true;
                case ReluProtocol.Enum.NetworkGrade networkGrade:
                    grade = (int)networkGrade;
                    return true;
                default:
                    try
                    {
                        grade = System.Convert.ToInt32(value);
                        return true;
                    }
                    catch
                    {
                        grade = -1;
                        return false;
                    }
            }
        }

        internal static bool IsBanned(SessionManager sessionManager, ulong steamId)
        {
            if (steamId == 0)
            {
                return false;
            }

            try
            {
                return sessionManager.ExistBannedSteamID(steamId);
            }
            catch
            {
                return false;
            }
        }

        internal static bool TryAddBan(SessionManager sessionManager, ulong steamId)
        {
            return steamId != 0 && BannedSteamIdsField?.GetValue(sessionManager) is HashSet<ulong> banned && banned.Add(steamId);
        }

        internal static bool TryRemoveBan(SessionManager sessionManager, ulong steamId)
        {
            return steamId != 0 && BannedSteamIdsField?.GetValue(sessionManager) is HashSet<ulong> banned && banned.Remove(steamId);
        }

        internal static IEnumerable<ulong> EnumerateBannedSteamIds(SessionManager sessionManager)
        {
            if (BannedSteamIdsField?.GetValue(sessionManager) is not HashSet<ulong> banned)
            {
                yield break;
            }

            foreach (ulong steamId in banned)
            {
                if (steamId != 0)
                {
                    yield return steamId;
                }
            }
        }

        internal static bool TryGetSessionId(SessionContext context, out long sessionId)
        {
            sessionId = 0;
            if (context == null)
            {
                return false;
            }

            try
            {
                sessionId = context.GetSessionID();
                return sessionId != 0;
            }
            catch
            {
                return false;
            }
        }

        internal static void DisconnectSession(SessionManager sessionManager, long sessionId, DisconnectReason reason)
        {
            sessionManager.Remove(sessionId, reason);
        }
    }
}
