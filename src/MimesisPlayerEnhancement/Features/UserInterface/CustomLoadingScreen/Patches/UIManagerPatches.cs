namespace MimesisPlayerEnhancement.Features.UserInterface.CustomLoadingScreen.Patches
{
    /// <summary>The exiting cutscene / video player calls <c>FadeOut(black)</c> on a full-screen
    /// image that sits above the normal loading UI. While we hold a custom departure screen,
    /// skip that fade so the custom image stays visible on top.</summary>
    [HarmonyPatch(typeof(UIManager), nameof(UIManager.FadeOut))]
    internal static class UIManagerFadeOutPatch
    {
        private const string Feature = CustomLoadingScreenConstants.Feature;

        private static bool Prefix()
        {
            if (!CustomLoadingScreenSession.HoldThroughDeparture)
            {
                return true;
            }

            try
            {
                CustomLoadingScreenApplier.SuppressVanillaFullscreenCovers();
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"FadeOut suppress failed — {ex.Message}");
            }

            return false;
        }
    }
}
