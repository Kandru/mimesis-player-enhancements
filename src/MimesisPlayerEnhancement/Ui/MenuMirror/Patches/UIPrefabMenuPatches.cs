namespace MimesisPlayerEnhancement.Ui.MenuMirror.Patches
{
    // game@0.3.2 Assembly-CSharp/UIPrefab_MainMenu.cs:L466-490
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

    // game@0.3.2 Assembly-CSharp/UIPrefab_MainMenu.cs:L453-456
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

    // game@0.3.2 Assembly-CSharp/UIPrefab_InGameMenu.cs:L489-583
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

    // game@0.3.2 Assembly-CSharp/UIPrefab_InGameMenu.cs:L610-664
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
