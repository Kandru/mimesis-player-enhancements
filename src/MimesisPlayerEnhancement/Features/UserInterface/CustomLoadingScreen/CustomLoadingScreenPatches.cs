namespace MimesisPlayerEnhancement.Features.UserInterface.CustomLoadingScreen
{
    internal static class CustomLoadingScreenPatches
    {
        private const string Feature = CustomLoadingScreenConstants.Feature;

        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            IEnumerable<Type> patchTypes = HarmonyPatchHelper.GetNamespacePatchTypes(typeof(CustomLoadingScreenPatches));
            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                patchTypes);

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("LoadScene/Hub", AccessTools.Method(typeof(Hub), nameof(Hub.LoadScene))),
                ("SetLoadingScene/UIPrefab_Scene_Loading", AccessTools.Method(typeof(UIPrefab_Scene_Loading), nameof(UIPrefab_Scene_Loading.SetLoadingScene))),
                ("SetLoadingText/UIPrefab_Scene_Loading", AccessTools.Method(typeof(UIPrefab_Scene_Loading), nameof(UIPrefab_Scene_Loading.SetLoadingText))),
                ("Show/UIPrefabScript (scene loading)", AccessTools.Method(typeof(UIPrefabScript), nameof(UIPrefabScript.Show))),
                ("Hide/UIPrefabScript (scene loading)", AccessTools.Method(typeof(UIPrefabScript), nameof(UIPrefabScript.Hide))),
                ("Cor_Hide/UIPrefabScript (game status)", AccessTools.Method(typeof(UIPrefabScript), nameof(UIPrefabScript.Cor_Hide))),
                ("OnChangeLevelObjectStateSig/NewTramLeverLevelObject", AccessTools.Method(typeof(NewTramLeverLevelObject), nameof(NewTramLeverLevelObject.OnChangeLevelObjectStateSig))),
                ("EndSceneLoading/GameMainBase", AccessTools.Method(typeof(GameMainBase), nameof(GameMainBase.EndSceneLoading))),
                ("FadeOut/UIManager", AccessTools.Method(typeof(UIManager), nameof(UIManager.FadeOut))),
            ]);
        }
    }
}
