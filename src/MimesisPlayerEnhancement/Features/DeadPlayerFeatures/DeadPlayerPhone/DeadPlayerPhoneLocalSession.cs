namespace MimesisPlayerEnhancement.Features.DeadPlayerFeatures.DeadPlayerPhone
{
    internal static class DeadPlayerPhoneLocalSession
    {
        internal static void Clear(bool endVoice)
        {
            if (endVoice && DeadPlayerPhoneLocalState.Phase == DeadPlayerPhoneSessionPhase.Talking)
            {
                DeadPlayerPhoneVoice.EndTalk();
            }

            DeadPlayerPhoneCamera.Exit();
            DeadPlayerPhoneLocalState.Clear();
        }
    }
}
