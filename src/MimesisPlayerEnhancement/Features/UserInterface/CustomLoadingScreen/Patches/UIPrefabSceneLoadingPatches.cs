namespace MimesisPlayerEnhancement.Features.UserInterface.CustomLoadingScreen.Patches
{
    [HarmonyPatch(typeof(UIPrefab_Scene_Loading), nameof(UIPrefab_Scene_Loading.SetLoadingScene))]
    internal static class UIPrefabSceneLoadingSetLoadingScenePatch
    {
        private const string Feature = CustomLoadingScreenConstants.Feature;

        private static void Postfix(UIPrefab_Scene_Loading __instance, string loadingSceneKey)
        {
            try
            {
                CustomLoadingScreenApplier.ApplyScene(__instance, loadingSceneKey);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"SetLoadingScene patch failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(UIPrefab_Scene_Loading), nameof(UIPrefab_Scene_Loading.SetLoadingText))]
    internal static class UIPrefabSceneLoadingSetLoadingTextPatch
    {
        private const string Feature = CustomLoadingScreenConstants.Feature;

        private static void Postfix(UIPrefab_Scene_Loading __instance, string str)
        {
            try
            {
                CustomLoadingScreenApplier.ApplyTextPhase(__instance, str);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"SetLoadingText patch failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(UIPrefabScript), nameof(UIPrefabScript.Hide))]
    internal static class UIPrefabSceneLoadingHidePatch
    {
        private const string Feature = CustomLoadingScreenConstants.Feature;

        private static void Prefix(UIPrefabScript __instance)
        {
            if (__instance is not UIPrefab_Scene_Loading loading)
            {
                return;
            }

            try
            {
                CustomLoadingScreenApplier.Restore(loading);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Hide patch failed — {ex.Message}");
            }
        }
    }
}
