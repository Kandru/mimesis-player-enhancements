using System.Reflection;

namespace MimesisPlayerEnhancement.Features.ModVersionDisplay.Patches
{
    // game@0.3.2 Assembly-CSharp/UIPrefab_MainMenu.cs:L492-498
    [HarmonyPatch]
    internal static class UIPrefabMainMenuSetVersionTextPatch
    {
        private const string Feature = "ModVersionDisplay";

        private static MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(UIPrefab_MainMenu), "SetVersionText");

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
