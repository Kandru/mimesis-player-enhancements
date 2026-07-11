namespace MimesisPlayerEnhancement.Features.ModVersionDisplay.Patches
{
    [HarmonyPatch(typeof(UIPrefab_InGameMenu), "SetVersionText")]
    internal static class UIPrefabInGameMenuSetVersionTextPatch
    {
        private const string Feature = "ModVersionDisplay";

        [HarmonyPostfix]
        private static void Postfix(UIPrefab_InGameMenu __instance)
        {
            try
            {
                ModVersionDisplayPatchHelpers.PrependModVersion(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"In-game menu version overlay failed — {ex.Message}");
            }
        }
    }
}
