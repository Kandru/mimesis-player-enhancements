namespace MimesisPlayerEnhancement.Features.UserInterface.CustomLoadingScreen.Patches
{
    [HarmonyPatch(typeof(GameMainBase), nameof(GameMainBase.EndSceneLoading))]
    internal static class GameMainBaseEndSceneLoadingPatch
    {
        private const string Feature = CustomLoadingScreenConstants.Feature;

        private static void Postfix()
        {
            if (CustomLoadingScreenSession.HoldThroughDeparture
                || CustomLoadingScreenSession.IsDismissing)
            {
                return;
            }

            try
            {
                // Hide() Prefix usually starts the fade; this covers paths that skip Hide.
                UIPrefab_Scene_Loading? loading = ModUiGameAccess.TryGetUiManager()?.ui_sceneloading;
                CustomLoadingScreenApplier.Restore(loading);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"EndSceneLoading patch failed — {ex.Message}");
            }
        }
    }
}
