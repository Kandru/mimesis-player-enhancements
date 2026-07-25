namespace MimesisPlayerEnhancement.Features.UserInterface.InventorySlotOptimization
{
    internal static class InventorySlotLayout
    {
        internal static List<InventoryItem> PackInventoryListForDisplay(
            List<InventoryItem> sourceItems,
            int selectedIndex,
            out int displaySelectedIndex)
        {
            int count = sourceItems.Count;
            List<InventoryItem> packed = new List<InventoryItem>(count);
            for (int i = 0; i < count; i++)
            {
                packed.Add(null!);
            }

            int writeIndex = 0;
            InventoryItem? selectedItem = null;
            if (selectedIndex >= 0 && selectedIndex < count)
            {
                selectedItem = sourceItems[selectedIndex];
            }

            for (int readIndex = 0; readIndex < count; readIndex++)
            {
                InventoryItem? item = sourceItems[readIndex];
                if (!IsEffectiveItem(item))
                {
                    continue;
                }

                packed[writeIndex++] = item!;
            }

            displaySelectedIndex = MapDisplaySelectedIndex(packed, selectedItem, selectedIndex);
            return packed;
        }

        private static int MapDisplaySelectedIndex(
            List<InventoryItem> packed,
            InventoryItem? selectedItem,
            int fallbackIndex)
        {
            if (selectedItem == null || !IsEffectiveItem(selectedItem))
            {
                return fallbackIndex;
            }

            for (int i = 0; i < packed.Count; i++)
            {
                if (ReferenceEquals(packed[i], selectedItem))
                {
                    return i;
                }
            }

            return fallbackIndex;
        }

        private static bool IsEffectiveItem(InventoryItem? item) =>
            item != null && item.ItemMasterID != 0;
    }
}
