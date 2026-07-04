namespace MimesisPlayerEnhancement.Features.DeadPlayerFeatures.DeadPlayerPhone
{
    internal enum DeadPlayerPhoneSessionPhase
    {
        None = 0,
        Ringing = 1,
        Talking = 2,
        Cooldown = 3,
    }

    internal enum PreferredDeadPlayerAction
    {
        None = 0,
        Mimic = 1,
        Phone = 2,
    }

    internal sealed class DeadPlayerPhoneSession
    {
        internal int DeadPlayerActorId;
        internal int PhoneLevelObjectId;
        internal DeadPlayerPhoneSessionPhase Phase;
        internal long PhaseEndTimeMs;
        internal float TalkDurationSeconds;
    }
}
