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
        [HarmonyPatch(typeof(UIPrefab_MainMenu), "Start")]
        internal static class MainMenuStartPostfix
        {
            [HarmonyPostfix]
            [HarmonyPriority(HarmonyLib.Priority.Last)]
            private static void Postfix(UIPrefab_MainMenu __instance)
            {
                MenuMirrorController.RefreshFor(MenuKind.MainMenu, __instance, allowCapture: true);
            }
        }

        [HarmonyPatch(typeof(UIPrefab_MainMenu), "OnEnable")]
        internal static class MainMenuOnEnablePostfix
        {
            [HarmonyPostfix]
            [HarmonyPriority(HarmonyLib.Priority.Last)]
            private static void Postfix(UIPrefab_MainMenu __instance)
            {
                MenuMirrorController.RefreshFor(MenuKind.MainMenu, __instance, allowCapture: false);
            }
        }

        [HarmonyPatch(typeof(UIPrefab_InGameMenu), "Start")]
        internal static class InGameMenuStartPostfix
        {
            [HarmonyPostfix]
            [HarmonyPriority(HarmonyLib.Priority.Last)]
            private static void Postfix(UIPrefab_InGameMenu __instance)
            {
                MenuMirrorController.RefreshFor(MenuKind.InGameMenu, __instance, allowCapture: true);
            }
        }

        [HarmonyPatch(typeof(UIPrefab_InGameMenu), "OnEnable")]
        internal static class InGameMenuOnEnablePostfix
        {
            [HarmonyPostfix]
            [HarmonyPriority(HarmonyLib.Priority.Last)]
            private static void Postfix(UIPrefab_InGameMenu __instance)
            {
                MenuMirrorController.RefreshFor(MenuKind.InGameMenu, __instance, allowCapture: false);
            }
        }
    }
}
