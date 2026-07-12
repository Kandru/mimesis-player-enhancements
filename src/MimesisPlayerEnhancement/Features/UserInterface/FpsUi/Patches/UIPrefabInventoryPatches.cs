using System.Reflection;
using Mimic;

namespace MimesisPlayerEnhancement.Features.UserInterface.FpsUi.Patches
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
            if (__instance is not UIPrefab_Inventory inventoryUi)
            {
                return;
            }

            try
            {
                if (FpsUiOverlay.IsEnabled())
                {
                    FpsUiOverlay.NotifyInventoryShown();
                }

                if (FpsUiNetWorthOverlay.IsEnabled())
                {
                    ProtoActor? avatar = Hub.Main?.GetMyAvatar();
                    if (avatar != null)
                    {
                        inventoryUi.UpdateSlot(
                            avatar.GetInventoryItems(),
                            avatar.GetSelectedInventorySlotIndex());
                    }

                    FpsUiNetWorthOverlay.NotifyInventoryShown();
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"FPS UI inventory show failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch]
    internal static class InventoryHidePostfix
    {
        private const string Feature = "Ui";

        internal static MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(UIPrefabScript), nameof(UIPrefabScript.Hide));

        [HarmonyPostfix]
        private static void Postfix(UIPrefabScript __instance)
        {
            if (__instance is not UIPrefab_Inventory)
            {
                return;
            }

            try
            {
                if (FpsUiOverlay.IsEnabled())
                {
                    FpsUiOverlay.OnInventoryHidden();
                }

                FpsUiNetWorthOverlay.OnInventoryHidden();
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"FPS UI inventory hide failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(UIPrefab_Inventory), nameof(UIPrefab_Inventory.UpdateSlot))]
    internal static class InventoryUpdateSlotPostfix
    {
        private const string Feature = "Ui";

        [HarmonyPostfix]
        private static void Postfix(in List<InventoryItem> inventoryItems, int currentInventorySlotIndex)
        {
            _ = currentInventorySlotIndex;

            try
            {
                if (FpsUiNetWorthOverlay.IsEnabled())
                {
                    FpsUiNetWorthOverlay.UpdateValue(inventoryItems);
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"FPS UI inventory update failed — {ex.Message}");
            }
        }
    }
}
