namespace MimesisPlayerEnhancement.Features.UserInterface.VoiceNoiseGate.Patches
{
    [HarmonyPatch(typeof(VoiceManager), nameof(VoiceManager.SetTalkMode))]
    internal static class VoiceManagerSetTalkModeVoiceNoiseGatePatch
    {
        private const string Feature = "Ui";

        [HarmonyPostfix]
        public static void Postfix(CommActivationMode mode)
        {
            try
            {
                VoiceNoiseGateRuntime.OnTalkModeChanged(mode);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Voice noise gate talk-mode hook failed — {ex.Message}");
            }
        }
    }
}
