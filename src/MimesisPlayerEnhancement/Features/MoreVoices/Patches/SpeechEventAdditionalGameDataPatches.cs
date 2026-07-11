namespace MimesisPlayerEnhancement.Features.MoreVoices.Patches
{
    [HarmonyPatch(typeof(SpeechEventAdditionalGameData), nameof(SpeechEventAdditionalGameData.PickBestMatch))]
    [HarmonyPriority(500)]
    internal static class PickBestMatchUnifyPatch
    {
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
            if (!MoreVoicesUnify.IsActive || VoicePerformanceRuntime.IsActive)
            {
                speechEvent = null;
                mimickingPlayerID = string.Empty;
                pickReason = string.Empty;
                return true;
            }

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
    }

    [HarmonyPatch(typeof(SpeechEventAdditionalGameData), nameof(SpeechEventAdditionalGameData.PickBestMatch))]
    internal static class PickBestMatchPerformancePatch
    {
        [HarmonyPrefix]
        internal static bool Prefix(
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
            if (!VoicePerformanceRuntime.IsActive)
            {
                speechEvent = null;
                mimickingPlayerID = string.Empty;
                pickReason = string.Empty;
                return true;
            }

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
    }
}
