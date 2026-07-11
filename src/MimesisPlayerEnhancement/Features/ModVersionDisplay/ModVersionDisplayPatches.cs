namespace MimesisPlayerEnhancement.Features.ModVersionDisplay
{
    internal static class ModVersionDisplayPatches
    {
        private const string Feature = "ModVersionDisplay";

        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();

            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(ModVersionDisplayPatches)));

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("SetVersionText/UIPrefab_MainMenu", AccessTools.Method(typeof(UIPrefab_MainMenu), "SetVersionText")),
                ("SetVersionText/UIPrefab_InGameMenu", AccessTools.Method(typeof(UIPrefab_InGameMenu), "SetVersionText")),
            ]);
        }
    }
}
