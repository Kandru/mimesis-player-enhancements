namespace MimesisPlayerEnhancement.Features.ModVersionDisplay.Patches
{
    [HarmonyPatch(typeof(UIPrefab_MainMenu), "SetVersionText")]
    internal static class UIPrefabMainMenuSetVersionTextPatch
    {
        private const string Feature = "ModVersionDisplay";

        [HarmonyPostfix]
        private static void Postfix(UIPrefab_MainMenu __instance)
        {
            try
            {
                ModVersionDisplayPatchHelpers.PrependModVersion(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Main menu version overlay failed — {ex.Message}");
            }
        }
    }
}
