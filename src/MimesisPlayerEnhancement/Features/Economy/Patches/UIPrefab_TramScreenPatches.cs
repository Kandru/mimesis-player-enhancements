using System.Reflection;

namespace MimesisPlayerEnhancement.Features.Economy.Patches
{
    // game@0.3.1 Assembly-CSharp/UIPrefab_TramScreen.cs:L417-431
    [HarmonyPatch]
    internal static class UIPrefabTramScreenRefreshRepairGuideTextPatch
    {
        private const string Feature = "Economy";

        private static MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(UIPrefab_TramScreen), "RefreshRepairGuideText");

        [HarmonyPostfix]
        private static void Postfix(UIPrefab_TramScreen __instance)
        {
            try
            {
                EconomyRetainCashUi.TryApplyTramScreenGuide(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"RefreshRepairGuideText postfix failed — {ex.Message}");
            }
        }
    }
}
