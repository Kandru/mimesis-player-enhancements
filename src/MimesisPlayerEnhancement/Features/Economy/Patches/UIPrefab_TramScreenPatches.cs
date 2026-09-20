using System.Reflection;

namespace MimesisPlayerEnhancement.Features.Economy.Patches
{
    // game@0.3.2 Assembly-CSharp/UIPrefab_TramScreen.cs:L434-449
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
