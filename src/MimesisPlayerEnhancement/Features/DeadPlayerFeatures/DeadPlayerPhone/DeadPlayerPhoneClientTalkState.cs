namespace MimesisPlayerEnhancement.Features.DeadPlayerFeatures.DeadPlayerPhone
{
    /// <summary>
    /// Client-side phone talk metadata shared across dead and living clients.
    /// </summary>
    internal static class DeadPlayerPhoneClientTalkState
    {
        private static readonly Dictionary<int, int> RingInitiatorActorIdByPhoneId = [];

        internal static int PhoneLevelObjectId { get; private set; }

        internal static int DeadCallerActorId { get; private set; }

        internal static int AnswererActorId { get; private set; }

        internal static bool IsActive => PhoneLevelObjectId > 0 && DeadCallerActorId > 0;

        internal static void SetRingInitiator(int phoneLevelObjectId, int actorId)
        {
            if (phoneLevelObjectId <= 0 || actorId <= 0)
            {
                return;
            }

            RingInitiatorActorIdByPhoneId[phoneLevelObjectId] = actorId;
        }

        internal static bool TryGetRingInitiator(int phoneLevelObjectId, out int actorId)
        {
            if (RingInitiatorActorIdByPhoneId.TryGetValue(phoneLevelObjectId, out actorId))
            {
                return actorId > 0;
            }

            actorId = 0;
            return false;
        }

        internal static void ClearRingInitiator(int phoneLevelObjectId)
        {
            if (phoneLevelObjectId > 0)
            {
                _ = RingInitiatorActorIdByPhoneId.Remove(phoneLevelObjectId);
            }
        }

        internal static void BeginTalk(int phoneLevelObjectId, int deadCallerActorId, int answererActorId)
        {
            PhoneLevelObjectId = phoneLevelObjectId;
            DeadCallerActorId = deadCallerActorId;
            AnswererActorId = answererActorId;
        }

        internal static void Clear()
        {
            PhoneLevelObjectId = 0;
            DeadCallerActorId = 0;
            AnswererActorId = 0;
            RingInitiatorActorIdByPhoneId.Clear();
        }
    }
}
