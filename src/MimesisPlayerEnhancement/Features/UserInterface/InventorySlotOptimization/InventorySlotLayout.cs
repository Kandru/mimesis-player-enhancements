using System.Collections;

namespace MimesisPlayerEnhancement.Features.UserInterface.InventorySlotOptimization
{
    internal static class InventorySlotLayout
    {
        // Invalid selected index only — empty slots still highlight via trailing packed empties.
        internal const int NoSelectionDisplayIndex = -1;

        internal readonly struct LayoutMaps
        {
            internal readonly int[] RawToDisplay;
            internal readonly int[] DisplayToRaw;

            internal LayoutMaps(int[] rawToDisplay, int[] displayToRaw)
            {
                RawToDisplay = rawToDisplay;
                DisplayToRaw = displayToRaw;
            }
        }

        internal static bool IsEffectiveItem(InventoryItem? item) =>
            item != null && item.ItemMasterID != 0;

        internal static bool IsEffectiveItem(object? slotEntry) =>
            slotEntry is InventoryItem item && IsEffectiveItem(item);

        internal static bool[] BuildRawOccupied(IList slotItems, int slotCount)
        {
            bool[] rawOccupied = new bool[slotCount];
            for (int i = 0; i < slotCount; i++)
            {
                rawOccupied[i] = IsEffectiveItem(slotItems[i]);
            }

            return rawOccupied;
        }

        internal static LayoutMaps BuildLayoutMaps(bool[] rawOccupied)
        {
            int count = rawOccupied.Length;
            int[] rawToDisplay = new int[count];
            int[] displayToRaw = new int[count];

            int effectiveCount = 0;
            for (int i = 0; i < count; i++)
            {
                if (rawOccupied[i])
                {
                    effectiveCount++;
                }
            }

            int occupiedWrite = 0;
            int emptyOrdinal = 0;
            for (int i = 0; i < count; i++)
            {
                if (rawOccupied[i])
                {
                    rawToDisplay[i] = occupiedWrite;
                    displayToRaw[occupiedWrite] = i;
                    occupiedWrite++;
                }
                else
                {
                    int displayIndex = effectiveCount + emptyOrdinal;
                    rawToDisplay[i] = displayIndex;
                    displayToRaw[displayIndex] = i;
                    emptyOrdinal++;
                }
            }

            return new LayoutMaps(rawToDisplay, displayToRaw);
        }

        internal static List<InventoryItem> PackInventoryListForDisplay(
            List<InventoryItem> sourceItems,
            int selectedIndex,
            out int displaySelectedIndex)
        {
            int count = sourceItems.Count;
            bool[] rawOccupied = new bool[count];
            for (int i = 0; i < count; i++)
            {
                rawOccupied[i] = IsEffectiveItem(sourceItems[i]);
            }

            LayoutMaps maps = BuildLayoutMaps(rawOccupied);
            List<InventoryItem> packed = new List<InventoryItem>(count);
            for (int i = 0; i < count; i++)
            {
                packed.Add(null!);
            }

            for (int rawIndex = 0; rawIndex < count; rawIndex++)
            {
                if (rawOccupied[rawIndex])
                {
                    packed[maps.RawToDisplay[rawIndex]] = sourceItems[rawIndex];
                }
            }

            displaySelectedIndex = ResolveDisplaySelectedIndex(maps, selectedIndex);
            return packed;
        }

        /// <summary>
        /// Maps a raw selected index onto the left-packed hotbar.
        /// Occupied selections land on their packed item; empty selections land on the
        /// matching trailing empty frame (so empty slots still highlight without lighting an item).
        /// </summary>
        internal static int ResolveDisplaySelectedIndex(bool[] rawOccupied, int selectedIndex) =>
            ResolveDisplaySelectedIndex(BuildLayoutMaps(rawOccupied), selectedIndex);

        internal static int ResolveDisplaySelectedIndex(LayoutMaps maps, int selectedIndex)
        {
            if (selectedIndex < 0 || selectedIndex >= maps.RawToDisplay.Length)
            {
                return NoSelectionDisplayIndex;
            }

            return maps.RawToDisplay[selectedIndex];
        }

        internal static int ResolveRawSelectedIndex(LayoutMaps maps, int displayIndex)
        {
            if (displayIndex < 0 || displayIndex >= maps.DisplayToRaw.Length)
            {
                return -1;
            }

            return maps.DisplayToRaw[displayIndex];
        }

        internal static int StepRawSelection(bool[] rawOccupied, int selectedIndex, int delta)
        {
            int count = rawOccupied.Length;
            if (count == 0)
            {
                return selectedIndex;
            }

            LayoutMaps maps = BuildLayoutMaps(rawOccupied);
            int displayIndex = ResolveDisplaySelectedIndex(maps, selectedIndex);
            if (displayIndex == NoSelectionDisplayIndex)
            {
                displayIndex = 0;
            }

            int nextDisplay = (displayIndex + delta % count + count) % count;
            return ResolveRawSelectedIndex(maps, nextDisplay);
        }
    }
}
