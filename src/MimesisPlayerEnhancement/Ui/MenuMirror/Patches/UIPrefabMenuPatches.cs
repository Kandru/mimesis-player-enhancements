namespace MimesisPlayerEnhancement.Ui.MenuMirror.Patches
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
