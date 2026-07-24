namespace MimesisPlayerEnhancement.Features.UserInterface.LoadingWaitPlayerList.Patches
{
    // game@0.3.1 Assembly-CSharp/UIPrefab_Scene_Loading.cs:L132-135
    [HarmonyPatch(typeof(UIPrefab_Scene_Loading), nameof(UIPrefab_Scene_Loading.SetLoadingText))]
    internal static class UIPrefabSceneLoadingWaitPlayerListSetLoadingTextPatch
    {
        private const string Feature = "Ui";

        private static void Postfix(UIPrefab_Scene_Loading __instance, string str)
        {
            try
            {
                LoadingWaitPlayerListRuntime.OnLoadingText(__instance, str);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Loading wait player list SetLoadingText patch failed — {ex.Message}");
            }
        }
    }

    // game@0.3.1 Assembly-CSharp/UIPrefabScript.cs:L97-116
    [HarmonyPatch(typeof(UIPrefabScript), nameof(UIPrefabScript.Hide))]
    internal static class UIPrefabSceneLoadingWaitPlayerListHidePatch
    {
        private const string Feature = "Ui";

        private static void Prefix(UIPrefabScript __instance)
        {
            if (__instance is not UIPrefab_Scene_Loading)
            {
                return;
            }

            try
            {
                if (LoadingWaitPlayerListRuntime.IsVisible)
                {
                    LoadingWaitPlayerListRuntime.Hide(fadeWithOverlay: true);
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Loading wait player list Hide patch failed — {ex.Message}");
            }
        }
    }
}
