namespace MimesisPlayerEnhancement.Features.UserInterface.CustomLoadingScreen.Patches
{
    [HarmonyPatch(typeof(GameMainBase), nameof(GameMainBase.EndSceneLoading))]
    internal static class GameMainBaseEndSceneLoadingPatch
    {
        private const string Feature = CustomLoadingScreenConstants.Feature;

        private static void Postfix()
        {
            try
            {
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
