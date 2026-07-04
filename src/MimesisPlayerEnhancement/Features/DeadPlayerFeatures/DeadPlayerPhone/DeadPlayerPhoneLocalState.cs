using System;

namespace MimesisPlayerEnhancement.Features.DeadPlayerFeatures.DeadPlayerPhone
{
    internal static class DeadPlayerPhoneLocalState
    {
        internal static bool HasActiveLocalSession =>
            Phase != DeadPlayerPhoneSessionPhase.None;

        internal static DeadPlayerPhoneSessionPhase Phase { get; private set; }

        internal static int PhoneLevelObjectId { get; private set; }

        internal static float PhaseEndTime { get; private set; }

        internal static float TotalPhaseSeconds { get; private set; }

        internal static void StartRing(int phoneLevelObjectId, float durationSeconds)
        {
            Phase = DeadPlayerPhoneSessionPhase.Ringing;
            PhoneLevelObjectId = phoneLevelObjectId;
            TotalPhaseSeconds = durationSeconds;
            PhaseEndTime = UnityEngine.Time.time + durationSeconds;
        }

        internal static void StartTalk(int phoneLevelObjectId, float durationSeconds)
        {
            Phase = DeadPlayerPhoneSessionPhase.Talking;
            PhoneLevelObjectId = phoneLevelObjectId;
            TotalPhaseSeconds = durationSeconds;
            PhaseEndTime = UnityEngine.Time.time + durationSeconds;
        }

        internal static void Clear()
        {
            Phase = DeadPlayerPhoneSessionPhase.None;
            PhoneLevelObjectId = 0;
            PhaseEndTime = 0f;
            TotalPhaseSeconds = 0f;
        }

        internal static float GetRemainingSeconds() =>
            Math.Max(0f, PhaseEndTime - UnityEngine.Time.time);

        internal static float GetFillAmount()
        {
            if (TotalPhaseSeconds <= 0f)
            {
                return 0f;
            }

            return GetRemainingSeconds() / TotalPhaseSeconds;
        }
    }
}
