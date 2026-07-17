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

    [HarmonyPatch(typeof(UIPrefabScript), nameof(UIPrefabScript.Show))]
    internal static class UIPrefabSceneLoadingShowPatch
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
                CustomLoadingScreenApplier.EnsureAppliedBeforeShow(loading);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Show patch failed — {ex.Message}");
            }
        }
    }

    /// <summary>The game status UI (member list) is hidden exactly when a lever-triggered
    /// departure sequence starts — after any exiting cutscene in the tram, seconds before
    /// <c>Hub.LoadScene</c>. Showing the custom loading screen here lets the departure fade
    /// land directly on the custom image instead of a black gap.</summary>
    [HarmonyPatch(typeof(UIPrefabScript), nameof(UIPrefabScript.Cor_Hide))]
    internal static class UIPrefabGameStatusCorHidePatch
    {
        private const string Feature = CustomLoadingScreenConstants.Feature;

        private static void Prefix(UIPrefabScript __instance)
        {
            if (__instance is not UIPrefab_GameStatus)
            {
                return;
            }

            try
            {
                CustomLoadingScreenApplier.ApplyOnDepartureStart();
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Cor_Hide patch failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(UIPrefabScript), nameof(UIPrefabScript.Hide))]
    internal static class UIPrefabSceneLoadingHidePatch
    {
        private const string Feature = CustomLoadingScreenConstants.Feature;

        private static bool Prefix(UIPrefabScript __instance)
        {
            if (__instance is not UIPrefab_Scene_Loading loading)
            {
                return true;
            }

            // Exiting cutscenes call EndSceneLoading → Hide ~0.1s in. Keep the custom screen
            // up until Hub.LoadScene finishes switching to the destination scene.
            if (CustomLoadingScreenSession.HoldThroughDeparture)
            {
                return false;
            }

            try
            {
                CustomLoadingScreenApplier.Restore(loading);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Hide patch failed — {ex.Message}");
            }

            return true;
        }
    }
}
