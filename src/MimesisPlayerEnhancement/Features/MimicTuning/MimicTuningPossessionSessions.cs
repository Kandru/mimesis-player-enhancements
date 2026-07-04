namespace MimesisPlayerEnhancement.Features.MimicTuning
{
    internal static class MimicTuningPossessionSessions
    {
        private static readonly Dictionary<int, long> SessionDurationMsByMimicActorId = [];

        internal static void SetSessionDurationMs(int mimicActorId, long durationMs)
        {
            if (mimicActorId <= 0 || durationMs <= 0)
            {
                return;
            }

            SessionDurationMsByMimicActorId[mimicActorId] = durationMs;
        }

        internal static bool TryGetSessionDurationMs(int mimicActorId, out long durationMs)
        {
            return SessionDurationMsByMimicActorId.TryGetValue(mimicActorId, out durationMs);
        }

        internal static void ClearSession(int mimicActorId)
        {
            if (mimicActorId <= 0)
            {
                return;
            }

            _ = SessionDurationMsByMimicActorId.Remove(mimicActorId);
        }
    }
}
