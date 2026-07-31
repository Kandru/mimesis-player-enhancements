using System.Collections;

namespace MimesisPlayerEnhancement.Features.UserInterface.InventorySlotOptimization
{
    internal static class InventorySlotOptimizer
    {
        internal struct SlotSnapshot
        {
            internal bool IsValid;
            internal int SelectedIndex;
            internal int OccupancyMask;
        }

        internal static bool IsEnabled =>
            ModConfig.EnableInventorySlotOptimization?.Value == true;

        internal static bool SelectionEnabled =>
            IsEnabled
            && (ModConfig.EnableInventorySelectNextOnRemove?.Value == true
                || !IsPickupModeVanilla(ModConfig.InventoryPickupSelectMode?.Value));

        internal static bool TryBeginPatch(object inventory, bool selectNextOnly, out SlotSnapshot snapshot)
        {
            snapshot = default;
            if (!IsEnabled)
            {
                return false;
            }

            if (selectNextOnly)
            {
                if (ModConfig.EnableInventorySelectNextOnRemove?.Value != true)
                {
                    return false;
                }
            }
            else if (!SelectionEnabled)
            {
                return false;
            }

            return TryCaptureSnapshot(inventory, out snapshot);
        }

        internal static void ApplyAfterChange(object inventory, SlotSnapshot before, bool allowPickup)
        {
            IList? slotItems = ProtoActorInventoryAccess.GetSlotItems(inventory);
            if (slotItems == null)
            {
                return;
            }

            int slotCount = slotItems.Count;
            int afterMask = BuildOccupancyMask(slotItems, slotCount);

            if (TrySelectNextAfterRemove(inventory, slotCount, before, afterMask))
            {
                return;
            }

            if (!allowPickup || IsPickupModeVanilla(ModConfig.InventoryPickupSelectMode?.Value))
            {
                return;
            }

            int pickupSlot = FindPickupSlot(before.OccupancyMask, afterMask, slotCount);
            if (pickupSlot < 0)
            {
                return;
            }

            InventoryItem? pickedItem = slotItems[pickupSlot] as InventoryItem;
            string? pickupMode = ModConfig.InventoryPickupSelectMode?.Value;
            if (!ShouldSelectPickup(pickupMode, IsWeaponItem(pickedItem)))
            {
                return;
            }

            int selectedIndex = ProtoActorInventoryAccess.GetSelectedSlotIndex(inventory);
            if (pickupSlot != selectedIndex)
            {
                ProtoActorInventoryAccess.RequestSelectSlot(inventory, pickupSlot);
            }
        }

        internal static bool TryCaptureSnapshot(object inventory, out SlotSnapshot snapshot)
        {
            snapshot = default;
            if (!ProtoActorInventoryAccess.IsLocalAvatarInventory(inventory))
            {
                return false;
            }

            IList? slotItems = ProtoActorInventoryAccess.GetSlotItems(inventory);
            if (slotItems == null)
            {
                return false;
            }

            int slotCount = slotItems.Count;
            snapshot = new SlotSnapshot
            {
                IsValid = true,
                SelectedIndex = ProtoActorInventoryAccess.GetSelectedSlotIndex(inventory),
                OccupancyMask = BuildOccupancyMask(slotItems, slotCount),
            };
            return true;
        }

        internal static bool DidSelectedSlotLoseItem(SlotSnapshot before, int afterMask) =>
            before.IsValid
            && IsSlotOccupied(before.OccupancyMask, before.SelectedIndex)
            && !IsSlotOccupied(afterMask, before.SelectedIndex);

        internal static int FindFirstOccupiedToRight(int occupancyMask, int startIndex, int slotCount)
        {
            for (int index = startIndex + 1; index < slotCount; index++)
            {
                if (IsSlotOccupied(occupancyMask, index))
                {
                    return index;
                }
            }

            return -1;
        }

        internal static int FindFirstOccupiedToLeft(int occupancyMask, int startIndex, int slotCount)
        {
            for (int step = 1; step < slotCount; step++)
            {
                int index = (startIndex - step + slotCount) % slotCount;
                if (IsSlotOccupied(occupancyMask, index))
                {
                    return index;
                }
            }

            return -1;
        }

        internal static bool ShouldSelectPickup(string? mode, bool isWeapon)
        {
            if (string.Equals(mode, "Always", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(mode, "WeaponsOnly", StringComparison.OrdinalIgnoreCase))
            {
                return isWeapon;
            }

            return false;
        }

        internal static bool IsWeaponItem(InventoryItem? item) =>
            item?.MasterInfo is ItemEquipmentInfo equipmentInfo
            && equipmentInfo.WeaponType != WeaponType.Invalid;

        internal static bool IsSlotOccupied(int occupancyMask, int index) =>
            (occupancyMask & (1 << index)) != 0;

        internal static bool DidSlotChangeSend(float beforeTimestamp, float afterTimestamp) =>
            !beforeTimestamp.Equals(afterTimestamp);

        private static bool TrySelectNextAfterRemove(
            object inventory,
            int slotCount,
            SlotSnapshot before,
            int afterMask)
        {
            if (ModConfig.EnableInventorySelectNextOnRemove?.Value != true
                || !DidSelectedSlotLoseItem(before, afterMask))
            {
                return false;
            }

            int droppedSlotIndex = before.SelectedIndex;
            int targetSlot = FindFirstOccupiedToRight(afterMask, droppedSlotIndex, slotCount);
            if (targetSlot < 0)
            {
                targetSlot = FindFirstOccupiedToLeft(afterMask, droppedSlotIndex, slotCount);
            }

            if (targetSlot < 0)
            {
                return false;
            }

            int selectedIndex = ProtoActorInventoryAccess.GetSelectedSlotIndex(inventory);
            if (targetSlot != selectedIndex)
            {
                ProtoActorInventoryAccess.RequestSelectSlot(inventory, targetSlot);
            }

            return true;
        }

        private static int FindPickupSlot(int beforeMask, int afterMask, int slotCount)
        {
            for (int i = 0; i < slotCount; i++)
            {
                if (!IsSlotOccupied(beforeMask, i) && IsSlotOccupied(afterMask, i))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int BuildOccupancyMask(IList slotItems, int slotCount)
        {
            int mask = 0;
            for (int i = 0; i < slotCount; i++)
            {
                if (IsEffectiveItem(slotItems[i]))
                {
                    mask |= 1 << i;
                }
            }

            return mask;
        }

        private static bool IsEffectiveItem(object? slotEntry) =>
            slotEntry is InventoryItem item && item.ItemMasterID != 0;

        private static bool IsPickupModeVanilla(string? mode) =>
            string.IsNullOrWhiteSpace(mode)
            || string.Equals(mode, "Vanilla", StringComparison.OrdinalIgnoreCase);
    }
}
