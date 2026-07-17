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
                ("SetLoadingScene/UIPrefab_Scene_Loading", AccessTools.Method(typeof(UIPrefab_Scene_Loading), nameof(UIPrefab_Scene_Loading.SetLoadingScene))),
                ("SetLoadingText/UIPrefab_Scene_Loading", AccessTools.Method(typeof(UIPrefab_Scene_Loading), nameof(UIPrefab_Scene_Loading.SetLoadingText))),
                ("Hide/UIPrefabScript (scene loading)", AccessTools.Method(typeof(UIPrefabScript), nameof(UIPrefabScript.Hide))),
                ("EndSceneLoading/GameMainBase", AccessTools.Method(typeof(GameMainBase), nameof(GameMainBase.EndSceneLoading))),
            ]);
        }
    }
}
