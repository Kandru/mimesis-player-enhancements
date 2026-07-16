namespace MimesisPlayerEnhancement.Features.Replays.Patches
{
    [HarmonyPatch(typeof(UIPrefab_MainMenu), "OnEnable")]
    internal static class MainMenuOnEnableReplayPostfix
    {
        private const string Feature = "Replays";

        [HarmonyPostfix]
        private static void Postfix(UIPrefab_MainMenu __instance)
        {
            try
            {
                ReplayPickerController.OnMainMenuShown(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"UIPrefab_MainMenu.OnEnable patch failed — {ex.Message}");
            }
        }
    }
}
