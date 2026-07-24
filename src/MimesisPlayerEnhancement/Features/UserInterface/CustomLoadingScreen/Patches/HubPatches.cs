namespace MimesisPlayerEnhancement.Features.UserInterface.CustomLoadingScreen.Patches
{
    // game@0.3.1 Assembly-CSharp/Hub.cs:L763-780
    [HarmonyPatch(typeof(Hub), nameof(Hub.LoadScene))]
    internal static class HubLoadScenePatch
    {
        private const string Feature = CustomLoadingScreenConstants.Feature;

        private static void Prefix(string sceneName)
        {
            try
            {
                CustomLoadingScreenApplier.ApplyBeforeSceneLoad(sceneName);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"LoadScene patch failed — {ex.Message}");
            }
        }

        private static void Postfix()
        {
            try
            {
                CustomLoadingScreenApplier.OnSceneLoadCompleted();
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"LoadScene post patch failed — {ex.Message}");
            }
        }
    }
}
