namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots.Patches
{
    [HarmonyPatch(typeof(MainMenu), "Start")]
    internal static class MainMenuStartPostfix
    {
        private const string Feature = "ExtendedSaveSlots";

        [HarmonyPostfix]
        private static void Postfix(MainMenu __instance)
        {
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
