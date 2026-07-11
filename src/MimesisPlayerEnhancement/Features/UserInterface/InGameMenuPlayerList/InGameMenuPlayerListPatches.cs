namespace MimesisPlayerEnhancement.Features.UserInterface.InGameMenuPlayerList
{
    internal static class InGameMenuPlayerListPatches
    {
        private const string Feature = "Ui";

        [HarmonyPatch(typeof(UIPrefab_InGameMenu), "Start")]
        internal static class InGameMenuStartPostfix
        {
            [HarmonyPostfix]
            private static void Postfix(UIPrefab_InGameMenu __instance)
            {
                try
                {
                    InGameMenuPlayerListLayout.Apply(__instance);
                }
                catch (System.Exception ex)
                {
                    ModLog.Warn(Feature, $"In-game menu player list layout init failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(UIPrefab_InGameMenu), "OnEnable")]
        internal static class InGameMenuOnEnablePostfix
        {
            [HarmonyPostfix]
            private static void Postfix(UIPrefab_InGameMenu __instance)
            {
                try
                {
                    InGameMenuPlayerListLayout.Apply(__instance);
                }
                catch (System.Exception ex)
                {
                    ModLog.Warn(Feature, $"In-game menu player list layout refresh failed — {ex.Message}");
                }
            }
        }
    }
}
