namespace MimesisPlayerEnhancement.Features.UserInterface.InGameMenuPlayerList.Patches
{
    [HarmonyPatch(typeof(UIPrefab_InGameMenu), "Start")]
    internal static class InGameMenuStartPostfix
    {
        private const string Feature = "Ui";

        [HarmonyPostfix]
        private static void Postfix(UIPrefab_InGameMenu __instance)
        {
            try
            {
                InGameMenuPlayerListLayout.Apply(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"In-game menu player list layout init failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(UIPrefab_InGameMenu), "OnEnable")]
    internal static class InGameMenuOnEnablePostfix
    {
        private const string Feature = "Ui";

        [HarmonyPostfix]
        private static void Postfix(UIPrefab_InGameMenu __instance)
        {
            try
            {
                InGameMenuPlayerListLayout.Apply(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"In-game menu player list layout refresh failed — {ex.Message}");
            }
        }
    }
}
