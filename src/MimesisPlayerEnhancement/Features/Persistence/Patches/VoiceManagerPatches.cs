namespace MimesisPlayerEnhancement.Features.Persistence.Patches
{
    /// <summary>
    /// Patches VoiceManager.GetRandomOtherSpeechEventArchive to fall back
    /// to the local archive when no other archives have events.
    /// This ensures hallucination voices work even when playing solo
    /// with warmed-up events in the local archive.
    /// </summary>
    // game@0.3.1 Assembly-CSharp/VoiceManager.cs:L1336-1346
    [HarmonyPatch(typeof(VoiceManager), "GetRandomOtherSpeechEventArchive")]
    internal static class VoiceManagerPatches
    {
        private const string Feature = "Persistence";

        [HarmonyPostfix]
        public static void Postfix(ref SpeechEventArchive __result)
        {
            if (!ModConfig.EnablePersistence.Value)
            {
                return;
            }

            try
            {
                // Only intervene if the original method found nothing
                if (__result != null)
                {
                    return;
                }

                // Get the local archive (stored by the injection patch)
                SpeechEventArchive? local = SpeechEventPoolManager.GetLocalArchive();
                if (local == null)
                {
                    return;
                }

                // Only use it if it has events in the warmed-up pool
                if (local.WarmedUpCount > 0)
                {
                    __result = local;
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Hallucination fallback: {ex.Message}");
            }
        }
    }
}
