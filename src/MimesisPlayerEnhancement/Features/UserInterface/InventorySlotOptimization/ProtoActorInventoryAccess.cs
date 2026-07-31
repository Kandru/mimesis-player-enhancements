using System.Collections;
using System.Reflection;

namespace MimesisPlayerEnhancement.Features.UserInterface.InventorySlotOptimization
{
    internal static class ProtoActorInventoryAccess
    {
        private const string Feature = "Ui";

        private static readonly BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        internal static readonly Type InventoryType =
            typeof(ProtoActor).GetNestedType("Inventory", InstanceFlags)
            ?? throw new InvalidOperationException("ProtoActor.Inventory nested type not found");

        private static readonly FieldInfo OwnerField =
            AccessTools.Field(InventoryType, "owner")
            ?? throw new InvalidOperationException("Inventory.owner field not found");

        private static readonly FieldInfo SelectedSlotIndexField =
            AccessTools.Field(InventoryType, "selectedSlotIndex")
            ?? throw new InvalidOperationException("Inventory.selectedSlotIndex field not found");

        private static readonly FieldInfo SlotItemsField =
            AccessTools.Field(InventoryType, "slotItems")
            ?? throw new InvalidOperationException("Inventory.slotItems field not found");

        private static readonly FieldInfo LastChangeActiveSlotTimeField =
            AccessTools.Field(InventoryType, "lastChangeActiveSlotTime")
            ?? throw new InvalidOperationException("Inventory.lastChangeActiveSlotTime field not found");

        private static readonly PropertyInfo SelectedItemProperty =
            AccessTools.Property(InventoryType, "SelectedItem")
            ?? throw new InvalidOperationException("Inventory.SelectedItem property not found");

        private static readonly MethodInfo SelectSlotMethod =
            AccessTools.Method(InventoryType, "SelectSlot", [typeof(int)])
            ?? throw new InvalidOperationException("Inventory.SelectSlot not found");

        private static readonly FieldInfo ProtoActorInventoryField =
            AccessTools.Field(typeof(ProtoActor), "inventory")
            ?? throw new InvalidOperationException("ProtoActor.inventory field not found");

        private static object? _pendingInventory;
        private static int _pendingSlotIndex = -1;

        internal static MethodBase OnUpdateInvenSigMethod { get; } =
            AccessTools.Method(InventoryType, "OnUpdateInvenSig")
            ?? throw new InvalidOperationException("Inventory.OnUpdateInvenSig not found");

        internal static MethodBase OnChangeItemLooksSigMethod { get; } =
            AccessTools.Method(InventoryType, "OnChangeItemLooksSig")
            ?? throw new InvalidOperationException("Inventory.OnChangeItemLooksSig not found");

        internal static MethodBase SelectNextSlotMethod { get; } =
            AccessTools.Method(InventoryType, "SelectNextSlot")
            ?? throw new InvalidOperationException("Inventory.SelectNextSlot not found");

        internal static MethodBase SelectPreviousSlotMethod { get; } =
            AccessTools.Method(InventoryType, "SelectPreviousSlot")
            ?? throw new InvalidOperationException("Inventory.SelectPreviousSlot not found");

        internal static bool IsLocalAvatarInventory(object inventory) =>
            OwnerField.GetValue(inventory) is ProtoActor owner && owner.AmIAvatar();

        internal static IList? GetSlotItems(object inventory) =>
            SlotItemsField.GetValue(inventory) as IList;

        internal static int GetSelectedSlotIndex(object inventory) =>
            (int)SelectedSlotIndexField.GetValue(inventory)!;

        internal static void RequestSelectSlot(object inventory, int targetSlotIndex)
        {
            if (IsForbidChange(inventory))
            {
                ClearPendingSelect();
                return;
            }

            if (TrySendSelectSlot(inventory, targetSlotIndex))
            {
                ClearPendingSelect();
                return;
            }

            QueuePendingSelect(inventory, targetSlotIndex);
        }

        internal static void ProcessPendingSelect()
        {
            if (_pendingInventory == null || _pendingSlotIndex < 0)
            {
                return;
            }

            if (!InventorySlotOptimizer.SelectionEnabled)
            {
                ClearPendingSelect();
                return;
            }

            if (IsForbidChange(_pendingInventory))
            {
                ClearPendingSelect();
                return;
            }

            if (!TrySendSelectSlot(_pendingInventory, _pendingSlotIndex))
            {
                return;
            }

            ModLog.Debug(Feature, $"Deferred inventory slot select sent — slot {_pendingSlotIndex}");
            ClearPendingSelect();
        }

        internal static void ClearPendingSelect()
        {
            _pendingInventory = null;
            _pendingSlotIndex = -1;
        }

        internal static bool TryGetInventory(ProtoActor actor, out object? inventory)
        {
            inventory = ProtoActorInventoryField.GetValue(actor);
            return inventory != null;
        }

        internal static void SelectSlot(object inventory, int slotIndex) =>
            SelectSlotMethod.Invoke(inventory, [slotIndex]);

        private static bool TrySendSelectSlot(object inventory, int targetSlotIndex)
        {
            float before = GetLastChangeActiveSlotTime(inventory);
            SelectSlotMethod.Invoke(inventory, [targetSlotIndex]);
            float after = GetLastChangeActiveSlotTime(inventory);
            return InventorySlotOptimizer.DidSlotChangeSend(before, after);
        }

        private static void QueuePendingSelect(object inventory, int targetSlotIndex)
        {
            bool isNew =
                !ReferenceEquals(_pendingInventory, inventory)
                || _pendingSlotIndex != targetSlotIndex;
            _pendingInventory = inventory;
            _pendingSlotIndex = targetSlotIndex;
            if (isNew)
            {
                ModLog.Debug(Feature, $"Deferred inventory slot select — slot {targetSlotIndex}");
            }
        }

        private static bool IsForbidChange(object inventory) =>
            SelectedItemProperty.GetValue(inventory) is InventoryItem selectedItem
            && selectedItem.MasterInfo.ForbidChange;

        private static float GetLastChangeActiveSlotTime(object inventory) =>
            (float)LastChangeActiveSlotTimeField.GetValue(inventory)!;
    }
}
