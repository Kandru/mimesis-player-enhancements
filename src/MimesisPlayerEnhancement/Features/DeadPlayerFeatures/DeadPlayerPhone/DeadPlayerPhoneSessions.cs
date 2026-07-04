namespace MimesisPlayerEnhancement.Features.DeadPlayerFeatures.DeadPlayerPhone
{
    internal static class DeadPlayerPhoneSessions
    {
        private static DeadPlayerPhoneSession? _activeSession;
        private static readonly Dictionary<int, float> CooldownEndTimeByDeadActorId = [];

        internal static bool HasActiveSession => _activeSession != null;

        internal static bool TryGetActiveSession(out DeadPlayerPhoneSession? session)
        {
            session = _activeSession;
            return session != null;
        }

        internal static bool IsPhoneInSession(int levelObjectId) =>
            _activeSession != null && _activeSession.PhoneLevelObjectId == levelObjectId;

        internal static bool IsOwner(int deadPlayerActorId) =>
            _activeSession != null && _activeSession.DeadPlayerActorId == deadPlayerActorId;

        internal static void StartRingSession(int deadPlayerActorId, int phoneLevelObjectId, long ringEndTimeMs)
        {
            _activeSession = new DeadPlayerPhoneSession
            {
                DeadPlayerActorId = deadPlayerActorId,
                PhoneLevelObjectId = phoneLevelObjectId,
                Phase = DeadPlayerPhoneSessionPhase.Ringing,
                PhaseEndTimeMs = ringEndTimeMs,
            };
        }

        internal static void StartTalkSession(int deadPlayerActorId, int phoneLevelObjectId, long talkEndTimeMs, float talkSeconds)
        {
            _activeSession = new DeadPlayerPhoneSession
            {
                DeadPlayerActorId = deadPlayerActorId,
                PhoneLevelObjectId = phoneLevelObjectId,
                Phase = DeadPlayerPhoneSessionPhase.Talking,
                PhaseEndTimeMs = talkEndTimeMs,
                TalkDurationSeconds = talkSeconds,
            };
        }

        internal static void ClearActiveSession()
        {
            _activeSession = null;
        }

        internal static void SetCooldown(int deadPlayerActorId, float cooldownSeconds)
        {
            if (deadPlayerActorId <= 0 || cooldownSeconds <= 0f)
            {
                return;
            }

            CooldownEndTimeByDeadActorId[deadPlayerActorId] = UnityEngine.Time.time + cooldownSeconds;
        }

        internal static bool IsInCooldown(int deadPlayerActorId)
        {
            if (deadPlayerActorId <= 0)
            {
                return false;
            }

            return CooldownEndTimeByDeadActorId.TryGetValue(deadPlayerActorId, out float endTime)
                && UnityEngine.Time.time < endTime;
        }

        internal static void ClearAll()
        {
            _activeSession = null;
            CooldownEndTimeByDeadActorId.Clear();
        }
    }
}
