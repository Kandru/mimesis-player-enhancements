namespace MimesisPlayerEnhancement.Features.UserInterface.InventorySlotOptimization
{
    internal static class InventorySlotLayout
    {
        // Invalid selected index only — empty slots still highlight via trailing packed empties.
        internal const int NoSelectionDisplayIndex = -1;

        internal static List<InventoryItem> PackInventoryListForDisplay(
            List<InventoryItem> sourceItems,
            int selectedIndex,
            out int displaySelectedIndex)
        {
            int count = sourceItems.Count;
            List<InventoryItem> packed = new List<InventoryItem>(count);
            bool[] rawOccupied = new bool[count];
            for (int i = 0; i < count; i++)
            {
                packed.Add(null!);
                rawOccupied[i] = IsEffectiveItem(sourceItems[i]);
            }

            int writeIndex = 0;
            for (int readIndex = 0; readIndex < count; readIndex++)
            {
                if (!rawOccupied[readIndex])
                {
                    continue;
                }

                packed[writeIndex++] = sourceItems[readIndex];
            }

            displaySelectedIndex = ResolveDisplaySelectedIndex(rawOccupied, selectedIndex);
            return packed;
        }

        /// <summary>
        /// Maps a raw selected index onto the left-packed hotbar.
        /// Occupied selections land on their packed item; empty selections land on the
        /// matching trailing empty frame (so empty slots still highlight without lighting an item).
        /// </summary>
        internal static int ResolveDisplaySelectedIndex(bool[] rawOccupied, int selectedIndex)
        {
            int count = rawOccupied.Length;
            if (selectedIndex < 0 || selectedIndex >= count)
            {
                return NoSelectionDisplayIndex;
            }

            if (rawOccupied[selectedIndex])
            {
                int packedIndex = 0;
                for (int i = 0; i < selectedIndex; i++)
                {
                    if (rawOccupied[i])
                    {
                        packedIndex++;
                    }
                }

                return packedIndex;
            }

            int effectiveCount = 0;
            int emptyOrdinal = 0;
            for (int i = 0; i < count; i++)
            {
                if (rawOccupied[i])
                {
                    effectiveCount++;
                }
                else if (i < selectedIndex)
                {
                    emptyOrdinal++;
                }
            }

            return effectiveCount + emptyOrdinal;
        }

        private static bool IsEffectiveItem(InventoryItem? item) =>
            item != null && item.ItemMasterID != 0;
    }
}
