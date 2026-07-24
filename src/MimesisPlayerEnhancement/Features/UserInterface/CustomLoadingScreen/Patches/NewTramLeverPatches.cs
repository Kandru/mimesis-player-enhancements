namespace MimesisPlayerEnhancement.Features.UserInterface.CustomLoadingScreen.Patches
{
    /// <summary>Lever <c>Open</c> is the earliest reliable "departure committed" signal shared by
    /// tram→dungeon, tram→maintenance, maintenance→tram, and dungeon exit. Apply the custom
    /// screen here so the player fades into the image instead of sitting on black until
    /// <c>Hub.LoadScene</c>.</summary>
    // game@0.3.1 Assembly-CSharp/NewTramLeverLevelObject.cs:L196-233
    [HarmonyPatch(typeof(NewTramLeverLevelObject), nameof(NewTramLeverLevelObject.OnChangeLevelObjectStateSig))]
    internal static class NewTramLeverOpenPatch
    {
        private const string Feature = CustomLoadingScreenConstants.Feature;

        private static void Postfix(NewTramLeverLevelObject __instance, int currentState)
        {
            if (currentState != (int)NewTramLeverState.Open)
            {
                return;
            }

            try
            {
                CustomLoadingScreenApplier.ApplyOnLeverOpen(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Lever Open patch failed — {ex.Message}");
            }
        }
    }
}
