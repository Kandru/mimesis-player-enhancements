namespace MimesisPlayerEnhancement.Features.MoreVoices.Patches
{
    // game@0.3.1 Assembly-CSharp/Mimic.Voice.SpeechSystem/SpeechEventAdditionalGameData.cs:L302-449
    [HarmonyPatch(typeof(SpeechEventAdditionalGameData), nameof(SpeechEventAdditionalGameData.PickBestMatch))]
    internal static class PickBestMatchPatch
    {
        private const string Feature = "MoreVoices";

        [HarmonyPrefix]
        public static bool Prefix(
            MimicVoiceSpawner.MimicContext context,
            List<(string playerID, SpeechEvent evt)> allEvents,
            SpeechEventAdditionalGameData curGameData,
            bool periodic,
            int pickCount,
            float playTimeIntervalRandom,
            out SpeechEvent? speechEvent,
            out string mimickingPlayerID,
            out string pickReason,
            ref bool __result)
        {
            if (!MoreVoicesUnify.IsActive && !VoicePerformanceRuntime.IsActive)
            {
                speechEvent = null;
                mimickingPlayerID = string.Empty;
                pickReason = string.Empty;
                return true;
            }

            try
            {
                __result = VoicePickBestMatch.TryPick(
                    context,
                    allEvents,
                    curGameData,
                    periodic,
                    pickCount,
                    playTimeIntervalRandom,
                    out speechEvent,
                    out mimickingPlayerID,
                    out pickReason);
                return false;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"PickBestMatch replacement failed: {ex.Message}");
                speechEvent = null;
                mimickingPlayerID = string.Empty;
                pickReason = string.Empty;
                return true;
            }
        }
    }
}
