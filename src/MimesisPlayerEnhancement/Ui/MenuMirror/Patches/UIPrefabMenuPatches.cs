namespace MimesisPlayerEnhancement.Ui.MenuMirror.Patches
{
    // game@0.3.1 Assembly-CSharp/UIPrefab_MainMenu.cs:L424-445
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

    // game@0.3.1 Assembly-CSharp/UIPrefab_MainMenu.cs:L411-414
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

    // game@0.3.1 Assembly-CSharp/UIPrefab_InGameMenu.cs:L489-580
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

    // game@0.3.1 Assembly-CSharp/UIPrefab_InGameMenu.cs:L607-673
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
