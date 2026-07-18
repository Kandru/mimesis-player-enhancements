using System.Reflection;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Util
{
    /// <summary>
    /// Cached reflection access to SessionManager and SessionContext instances.
    /// Shared by gameplay features and the web dashboard.
    /// </summary>
    internal static class SessionContextAccess
    {
        private const BindingFlags InstanceMemberFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo? HostSessionContextField =
            typeof(SessionManager).GetField("_hostSessionContext", InstanceMemberFlags);

        private static readonly FieldInfo? ContextsField =
            typeof(SessionManager).GetField("m_Contexts", InstanceMemberFlags);

        private static readonly FieldInfo? SessionVPlayerField =
            typeof(SessionContext).GetField("_vPlayer", InstanceMemberFlags);

        private static readonly FieldInfo? SessionManagerField =
            typeof(VWorld).GetField("_sessionManager", InstanceMemberFlags);

        internal static SessionManager? GetSessionManager()
        {
            try
            {
                VWorld? vworld = GameSessionAccess.TryGetVWorld();
                if (vworld == null)
                {
                    return null;
                }

                return SessionManagerField?.GetValue(vworld) as SessionManager;
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

        internal static bool TryGetSessionContextByUid(long uid, out SessionContext? context)
        {
            context = null;
            if (uid == 0)
            {
                return false;
            }

            SessionManager? sessionManager = GetSessionManager();
            if (sessionManager == null)
            {
                return false;
            }

            foreach (SessionContext candidate in EnumerateSessionContexts(sessionManager))
            {
                if (candidate.GetPlayerUID() != uid)
                {
                    continue;
                }

                context = candidate;
                return true;
            }

            return false;
        }

        internal static bool TryGetSessionContextBySessionId(
            SessionManager sessionManager,
            long sessionId,
            out SessionContext? context)
        {
            context = null;
            if (sessionId == 0 || sessionManager == null)
            {
                return false;
            }

            if (HostSessionContextField?.GetValue(sessionManager) is SessionContext host
                && host != null
                && host.GetSessionID() == sessionId)
            {
                context = host;
                return true;
            }

            if (ContextsField?.GetValue(sessionManager) is Dictionary<long, SessionContext> contexts
                && contexts.TryGetValue(sessionId, out SessionContext? resolved))
            {
                context = resolved;
                return true;
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
