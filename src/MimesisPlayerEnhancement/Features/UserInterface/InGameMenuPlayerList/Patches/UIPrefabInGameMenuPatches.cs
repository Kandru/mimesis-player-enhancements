namespace MimesisPlayerEnhancement.Features.UserInterface.InGameMenuPlayerList.Patches
{
    // game@0.3.1 Assembly-CSharp/UIPrefab_InGameMenu.cs:L607-656
    [HarmonyPatch(typeof(UIPrefab_InGameMenu), "OnEnable")]
    internal static class InGameMenuOnEnablePostfix
    {
        private const string Feature = "Ui";

        [HarmonyPostfix]
        [HarmonyPriority(HarmonyLib.Priority.Last)]
        private static void Postfix(UIPrefab_InGameMenu __instance)
        {
            try
            {
                InGameMenuPlayerListOverlay.Show(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"In-game menu player list overlay show failed — {ex.Message}");
            }
        }
    }

    // game@0.3.1 Assembly-CSharp/UIPrefab_InGameMenu.cs:L582-586
    [HarmonyPatch(typeof(UIPrefab_InGameMenu), "OnDisable")]
    internal static class InGameMenuOnDisablePostfix
    {
        private const string Feature = "Ui";

        [HarmonyPostfix]
        private static void Postfix(UIPrefab_InGameMenu __instance)
        {
            try
            {
                InGameMenuPlayerListOverlay.Hide(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"In-game menu player list overlay hide failed — {ex.Message}");
            }
        }
    }
}
