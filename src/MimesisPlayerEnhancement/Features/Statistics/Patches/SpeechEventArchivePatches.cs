namespace MimesisPlayerEnhancement.Features.Statistics.Patches
{
    // game@0.3.1 Assembly-CSharp/Mimic.Voice.SpeechSystem/SpeechEventArchive.cs:L111-126
    [HarmonyPatch(typeof(SpeechEventArchive), "OnStartClient")]
    internal static class StatisticsSpeechEventArchivePatches
    {
        [HarmonyPostfix]
        private static void Postfix(SpeechEventArchive __instance)
        {
            bool isLocal;
            try
            {
                isLocal = __instance.IsLocal;
            }
            catch
            {
                return;
            }

            if (!isLocal)
            {
                return;
            }

            StatisticsMessages.OnLocalPlayerArchiveStarted();
        }
    }
}
