using System.Reflection;

namespace MimesisPlayerEnhancement.Features.ModVersionDisplay.Patches
{
    [HarmonyPatch]
    internal static class UIPrefabInGameMenuSetVersionTextPatch
    {
        private const string Feature = "ModVersionDisplay";

        private static MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(UIPrefab_InGameMenu), "SetVersionText");

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
