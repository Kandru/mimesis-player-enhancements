namespace MimesisPlayerEnhancement.Features.Replays.Patches
{
    [HarmonyPatch(typeof(MainMenu), "Start")]
    internal static class MainMenuStartReplayPostfix
    {
        private const string Feature = "Replays";

        [HarmonyPostfix]
        private static void Postfix(MainMenu __instance)
        {
            try
            {
                UIPrefab_MainMenu? mainMenuUi = AccessTools.Field(typeof(MainMenu), "ui_mainmenu")
                    .GetValue(__instance) as UIPrefab_MainMenu;
                if (mainMenuUi == null)
                {
                    return;
                }

                ReplayPickerController.OnMainMenuStarted(__instance, mainMenuUi);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"MainMenu.Start patch failed — {ex.Message}");
            }
        }
    }
}
