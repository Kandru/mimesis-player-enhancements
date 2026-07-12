using System.Reflection;

namespace MimesisPlayerEnhancement.Features.UserInterface.FpsVitalsHud.Patches
{
    [HarmonyPatch]
    internal static class InventoryShowPostfix
    {
        private const string Feature = "Ui";

        internal static MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(UIPrefabScript), nameof(UIPrefabScript.Show));

        [HarmonyPostfix]
        private static void Postfix(UIPrefabScript __instance)
        {
            if (__instance is not UIPrefab_Inventory)
            {
                return;
            }

            if (!FpsVitalsHudOverlay.IsEnabled())
            {
                return;
            }

            try
            {
                FpsVitalsHudOverlay.RefreshLayout();
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"FPS vitals HUD layout refresh failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(UIPrefab_Inventory), nameof(UIPrefab_Inventory.UpdateSlot))]
    internal static class InventoryUpdateSlotPostfix
    {
        private const string Feature = "Ui";

        [HarmonyPostfix]
        private static void Postfix()
        {
            if (!FpsVitalsHudOverlay.IsEnabled())
            {
                return;
            }

            try
            {
                FpsVitalsHudOverlay.RefreshLayout();
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"FPS vitals HUD layout refresh failed — {ex.Message}");
            }
        }
    }
}
