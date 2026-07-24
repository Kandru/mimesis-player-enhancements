namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots.Patches
{
    // game@0.3.1 Assembly-CSharp/MainMenu.cs:L119-267
    [HarmonyPatch(typeof(MainMenu), "Start")]
    internal static class MainMenuStartPostfix
    {
        private const string Feature = "ExtendedSaveSlots";

        [HarmonyPostfix]
        private static void Postfix(MainMenu __instance)
        {
            // game@0.3.1 Assembly-CSharp/MainMenu.cs ui_mainmenu
            UIPrefab_MainMenu? mainMenuUi = AccessTools.Field(typeof(MainMenu), "ui_mainmenu")
                .GetValue(__instance) as UIPrefab_MainMenu;

            if (mainMenuUi == null)
            {
                ModLog.Warn(Feature, "Failed to initialize save picker — main menu UI missing.");
                return;
            }

            TramSavePickerController.OnMainMenuStarted(__instance, mainMenuUi);
        }
    }
}
