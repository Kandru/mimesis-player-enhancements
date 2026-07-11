namespace MimesisPlayerEnhancement.Ui.MenuMirror
{
    /// <summary>
    /// Entry points for the menu mirror. Main menu capture is deferred to Start because
    /// vanilla Start offsets rootNode before the column is visually positioned; in-game
    /// menu capture happens on Start as well. OnEnable only rebuilds an already-captured
    /// column (re-show / color reset) without re-capturing.
    /// </summary>
    internal static class MenuMirrorPatches
    {
        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                "Ui",
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(MenuMirrorPatches)));

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary("Ui", result);
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit("Ui", harmony,
            [
                ("Start/UIPrefab_MainMenu (menu mirror)", AccessTools.Method(typeof(UIPrefab_MainMenu), "Start")),
                ("OnEnable/UIPrefab_MainMenu (menu mirror)", AccessTools.Method(typeof(UIPrefab_MainMenu), "OnEnable")),
                ("Start/UIPrefab_InGameMenu (menu mirror)", AccessTools.Method(typeof(UIPrefab_InGameMenu), "Start")),
                ("OnEnable/UIPrefab_InGameMenu (menu mirror)", AccessTools.Method(typeof(UIPrefab_InGameMenu), "OnEnable")),
            ]);
        }
    }
}
