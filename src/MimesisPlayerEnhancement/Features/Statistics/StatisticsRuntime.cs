using MimesisPlayerEnhancement.Features.Statistics.Models;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    internal sealed class SessionStats
    {
        internal string SessionId = "";
        internal DateTime StartedAtUtc;
        internal DateTime LastConnectedAtUtc;
        internal DateTime? LastDisconnectedAtUtc;
        internal int ReconnectCount;
        internal bool IsOpen = true;
        internal StatCounters Counters = new();
    }

    internal static class StatisticsRuntime
    {
        private const int MaxRecentSessions = 20;

        private static readonly Dictionary<ulong, SessionStats> CurrentSessions = [];
        private static readonly Dictionary<ulong, List<SessionStats>> RecentSessions = [];

        internal static SessionStats? GetCurrentSession(ulong steamId)
        {
            return CurrentSessions.TryGetValue(steamId, out SessionStats? session) ? session : null;
        }

        internal static IReadOnlyList<SessionStats> GetRecentSessions(ulong steamId)
        {
            return RecentSessions.TryGetValue(steamId, out List<SessionStats>? sessions)
                ? sessions
                : [];
        }

        internal static SessionStats CreateSession(DateTime now)
        {
            return new SessionStats
            {
                SessionId = Guid.NewGuid().ToString("N"),
                StartedAtUtc = now,
                LastConnectedAtUtc = now,
                IsOpen = true,
                Counters = new StatCounters(),
            };
        }

        internal static void SetCurrentSession(ulong steamId, SessionStats? session)
        {
            if (steamId == 0)
            {
                return;
            }

            if (session == null)
            {
                _ = CurrentSessions.Remove(steamId);
                return;
            }

            CurrentSessions[steamId] = session;
        }

        internal static void FinalizeSession(ulong steamId, SessionStats session, bool countAsCompleted)
        {
            if (steamId == 0 || session == null)
            {
                return;
            }

            session.IsOpen = false;
            if (!RecentSessions.TryGetValue(steamId, out List<SessionStats>? list))
            {
                list = [];
                RecentSessions[steamId] = list;
            }

            list.Add(CloneSession(session));
            while (list.Count > MaxRecentSessions)
            {
                list.RemoveAt(0);
            }

            _ = CurrentSessions.Remove(steamId);

            if (countAsCompleted)
            {
                PlayerGlobalStats global = StatisticsHistory.EnsureGlobal(steamId);
                global.SessionsCompleted++;
            }
        }

        internal static SessionStats CloneSession(SessionStats session)
        {
            return new SessionStats
            {
                SessionId = session.SessionId,
                StartedAtUtc = session.StartedAtUtc,
                LastConnectedAtUtc = session.LastConnectedAtUtc,
                LastDisconnectedAtUtc = session.LastDisconnectedAtUtc,
                ReconnectCount = session.ReconnectCount,
                IsOpen = false,
                Counters = session.Counters.Clone(),
            };
        }

        internal static bool HasOpenDisconnectedSessions()
        {
            foreach (SessionStats session in CurrentSessions.Values)
            {
                if (session.IsOpen && session.LastDisconnectedAtUtc.HasValue)
                {
                    return true;
                }
            }

            return false;
        }

        internal static void Clear()
        {
            CurrentSessions.Clear();
            RecentSessions.Clear();
        }
    }
}
