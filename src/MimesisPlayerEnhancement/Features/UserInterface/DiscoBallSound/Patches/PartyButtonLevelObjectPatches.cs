namespace MimesisPlayerEnhancement.Features.UserInterface.DiscoBallSound.Patches
{
    // game@0.3.1 Assembly-CSharp/PartyButtonLevelObject.cs:L106-125
    [HarmonyPatch(typeof(PartyButtonLevelObject), "PlayPartySound")]
    internal static class PartyButtonPlayPartySoundPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(PartyButtonLevelObject __instance)
        {
            try
            {
                if (!DiscoBallSoundResolver.ShouldApplyReplacement())
                {
                    return true;
                }

                return !DiscoBallSoundPlayer.TryStartLoop(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(DiscoBallSoundConstants.Feature, $"Disco ball sound start failed — {ex.Message}");
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(PartyButtonLevelObject), "StopPartySound")]
    internal static class PartyButtonStopPartySoundPatch
    {
        [HarmonyPrefix]
        private static void Prefix(PartyButtonLevelObject __instance)
        {
            try
            {
                DiscoBallSoundPlayer.StopLoop(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(DiscoBallSoundConstants.Feature, $"Disco ball sound stop failed — {ex.Message}");
            }
        }
    }
}
