namespace MimesisPlayerEnhancement.Features.DeadPlayerFeatures.DeadPlayerPhone
{
    internal static class DeadPlayerPhoneLocalSession
    {
        internal static void Clear(bool endVoice)
        {
            if (endVoice && DeadPlayerPhoneLocalState.Phase == DeadPlayerPhoneSessionPhase.Talking)
            {
                DeadPlayerPhoneVoiceSession.End();
            }

            DeadPlayerPhoneCamera.Exit();
            DeadPlayerPhoneLocalState.Clear();
        }
    }
}
