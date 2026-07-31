using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    /// <summary>
    /// Mirrors vanilla kick timing: KickPlayerSig first, hard remove after ~5s (without banning).
    /// </summary>
    internal static class WebDashboardPendingKickScheduler
    {
        private const string Feature = "WebDashboard";

        // game@0.3.1 SessionManager.HandleKickPlayerReq — _reservedForceKickSessions delay
        internal const long ForceRemoveDelayMs = 5000L;

        private static readonly Dictionary<long, long> PendingDueTicks = new();

        internal static bool IsScheduled(long sessionId) => PendingDueTicks.ContainsKey(sessionId);

        internal static void Schedule(long sessionId, long dueTickMs)
        {
            PendingDueTicks[sessionId] = dueTickMs;
        }

        internal static void ClearOnSessionEnded()
        {
            PendingDueTicks.Clear();
        }

        internal static IEnumerable<long> CollectDueSessionIds(
            IReadOnlyDictionary<long, long> pendingDueTicks,
            long currentTickMs)
        {
            foreach (KeyValuePair<long, long> entry in pendingDueTicks)
            {
                if (currentTickMs >= entry.Value)
                {
                    yield return entry.Key;
                }
            }
        }

        internal static void ProcessDue()
        {
            if (PendingDueTicks.Count == 0)
            {
                return;
            }

            SessionManager? sessionManager = WebDashboardSessionAccess.GetSessionManager();
            if (sessionManager == null)
            {
                return;
            }

            long now = GameSessionAccess.TryGetTimeUtil()?.GetCurrentTickMilliSec() ?? 0L;
            if (now == 0)
            {
                return;
            }

            List<long>? dueSessionIds = null;
            foreach (long sessionId in CollectDueSessionIds(PendingDueTicks, now))
            {
                dueSessionIds ??= [];
                dueSessionIds.Add(sessionId);
            }

            if (dueSessionIds == null)
            {
                return;
            }

            foreach (long sessionId in dueSessionIds)
            {
                PendingDueTicks.Remove(sessionId);
                if (!WebDashboardSessionAccess.TryGetSessionContextBySessionId(sessionManager, sessionId, out _))
                {
                    continue;
                }

                try
                {
                    sessionManager.Remove(sessionId, DisconnectReason.KickByServer);
                    ModLog.Debug(Feature, $"Pending kick — removed session={sessionId}.");
                }
                catch (System.Exception ex)
                {
                    ModLog.Warn(Feature, $"Pending kick remove failed session={sessionId}: {ex.Message}");
                }
            }
        }
    }
}
