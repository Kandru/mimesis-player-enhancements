using System.Collections;
using System.Reflection;

namespace MimesisPlayerEnhancement.Features.UserInterface.InventorySlotOptimization
{
    internal static class ProtoActorInventoryAccess
    {
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

        private static readonly PropertyInfo SelectedItemProperty =
            AccessTools.Property(InventoryType, "SelectedItem")
            ?? throw new InvalidOperationException("Inventory.SelectedItem property not found");

        private static readonly MethodInfo SendChangeActiveInvenSlotMethod =
            AccessTools.Method(InventoryType, "SendChangeActiveInvenSlot", [typeof(int)])
            ?? throw new InvalidOperationException("Inventory.SendChangeActiveInvenSlot not found");

        internal static MethodBase OnUpdateInvenSigMethod { get; } =
            AccessTools.Method(InventoryType, "OnUpdateInvenSig")
            ?? throw new InvalidOperationException("Inventory.OnUpdateInvenSig not found");

        internal static MethodBase OnChangeItemLooksSigMethod { get; } =
            AccessTools.Method(InventoryType, "OnChangeItemLooksSig")
            ?? throw new InvalidOperationException("Inventory.OnChangeItemLooksSig not found");

        internal static bool IsLocalAvatarInventory(object inventory) =>
            OwnerField.GetValue(inventory) is ProtoActor owner && owner.AmIAvatar();

        internal static IList? GetSlotItems(object inventory) =>
            SlotItemsField.GetValue(inventory) as IList;

        internal static int GetSelectedSlotIndex(object inventory) =>
            (int)SelectedSlotIndexField.GetValue(inventory)!;

        internal static void RequestSelectSlot(object inventory, int targetSlotIndex)
        {
            if (SelectedItemProperty.GetValue(inventory) is InventoryItem selectedItem
                && selectedItem.MasterInfo.ForbidChange)
            {
                return;
            }

            SendChangeActiveInvenSlotMethod.Invoke(inventory, [targetSlotIndex]);
        }
    }
}
