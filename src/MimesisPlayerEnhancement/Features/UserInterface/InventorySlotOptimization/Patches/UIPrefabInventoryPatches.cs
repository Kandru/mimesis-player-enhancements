namespace MimesisPlayerEnhancement.Features.UserInterface.InventorySlotOptimization.Patches
{
    // game@0.3.1 Assembly-CSharp/UIPrefab_Inventory.cs:L164-230
    [HarmonyPatch(typeof(UIPrefab_Inventory), nameof(UIPrefab_Inventory.UpdateSlot))]
    internal static class UIPrefabInventoryUpdateSlotDisplayPackPrefix
    {
        private const string Feature = "Ui";

        [HarmonyPrefix]
        private static void Prefix(ref List<InventoryItem> inventoryItems, ref int currentInventorySlotIndex)
        {
            if (!InventorySlotOptimizer.IsEnabled)
            {
                return;
            }

            try
            {
                ProtoActor? avatar = Hub.Main?.GetMyAvatar();
                if (avatar == null || !ReferenceEquals(inventoryItems, avatar.GetInventoryItems()))
                {
                    return;
                }

                inventoryItems = InventorySlotLayout.PackInventoryListForDisplay(
                    inventoryItems,
                    currentInventorySlotIndex,
                    out int displaySelectedIndex);
                currentInventorySlotIndex = displaySelectedIndex;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Inventory slot display pack failed — {ex.Message}");
            }
        }
    }
}
