using System.Reflection;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardSessionAccess
    {
        private const BindingFlags InstanceMemberFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo? NetworkGradesField =
            typeof(SessionManager).GetField("_networkGrades", InstanceMemberFlags);

        private static readonly FieldInfo? BannedSteamIdsField =
            typeof(SessionManager).GetField("_bannedSteamIDs", InstanceMemberFlags);

        private static readonly FieldInfo? EnterPktHashCodeField =
            typeof(SessionContext).GetField("_enterPktHashCode", InstanceMemberFlags);

        internal static SessionManager? GetSessionManager() => SessionContextAccess.GetSessionManager();

        internal static IEnumerable<SessionContext> EnumerateSessionContexts(SessionManager sessionManager) =>
            SessionContextAccess.EnumerateSessionContexts(sessionManager);

        internal static VPlayer? GetVPlayer(SessionContext context) =>
            SessionContextAccess.GetVPlayer(context);

        internal static bool TryGetPlayerByUid(long uid, out VPlayer? player) =>
            SessionContextAccess.TryGetPlayerByUid(uid, out player);

        internal static bool TryGetSessionContextByUid(long uid, out SessionContext? context) =>
            SessionContextAccess.TryGetSessionContextByUid(uid, out context);

        internal static bool TryGetSessionContextBySessionId(
            SessionManager sessionManager,
            long sessionId,
            out SessionContext? context) =>
            SessionContextAccess.TryGetSessionContextBySessionId(sessionManager, sessionId, out context);

        internal static bool TryGetHostPlayerUid(out long hostUid) =>
            SessionContextAccess.TryGetHostPlayerUid(out hostUid);

        internal static int GetEnterPktHashCode(SessionContext context)
        {
            return EnterPktHashCodeField?.GetValue(context) is int hashCode ? hashCode : context.EnterPktHashCode;
        }

        internal static SessionContext? FindHostSessionContext(SessionManager sessionManager) =>
            SessionContextAccess.FindHostSessionContext(sessionManager);

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

        internal static bool TryGetSessionId(SessionContext context, out long sessionId) =>
            SessionContextAccess.TryGetSessionId(context, out sessionId);

        internal static void DisconnectSession(SessionManager sessionManager, long sessionId, DisconnectReason reason) =>
            SessionContextAccess.DisconnectSession(sessionManager, sessionId, reason);
    }
}
