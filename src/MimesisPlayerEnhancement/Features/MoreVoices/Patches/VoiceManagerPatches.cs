namespace MimesisPlayerEnhancement.Features.MoreVoices.Patches
{
    // game@0.3.1 Assembly-CSharp/VoiceManager.cs:L789-825
    [HarmonyPatch(typeof(VoiceManager), nameof(VoiceManager.SetVoiceMode))]
    internal static class VoiceManagerSetVoiceModePatch
    {
        private const string Feature = "MoreVoices";

        [HarmonyPostfix]
        public static void Postfix(VoiceManager __instance, VoiceMode voiceMode)
        {
            if (!MoreVoicesRecording.IsFeatureActive()
                || voiceMode != VoiceMode.PreGame
                || !MoreVoicesRecording.ShouldRecordInCurrentHubScene())
            {
                return;
            }

            try
            {
                MoreVoicesVoiceAccess.StartRecording(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Hub voice recording postfix failed: {ex.Message}");
            }
        }
    }

    // game@0.3.1 Assembly-CSharp/VoiceManager.cs:L605-644
    [HarmonyPatch(typeof(VoiceManager), nameof(VoiceManager.EndPossessionToMimic))]
    internal static class VoiceManagerEndPossessionToMimicPatch
    {
        private const string Feature = "MoreVoices";

        [HarmonyPostfix]
        public static void Postfix(VoiceManager __instance)
        {
            if (!MoreVoicesRecording.ShouldResumeRecordingAfterPossession())
            {
                return;
            }

            try
            {
                MoreVoicesVoiceAccess.StartRecording(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Possession voice recording resume failed: {ex.Message}");
            }
        }
    }
}
