using System.Collections;
using System.Reflection;

namespace MimesisPlayerEnhancement.Features.UserInterface.InventorySlotOptimization.Patches
{
    // game@0.3.1 Assembly-CSharp/Mimic.Actors/ProtoActor+Inventory.cs:L2356-2365
    [HarmonyPatch]
    internal static class ProtoActorInventorySelectNextSlotPrefix
    {
        internal static MethodBase TargetMethod() => ProtoActorInventoryAccess.SelectNextSlotMethod;

        [HarmonyPrefix]
        private static bool Prefix(object __instance)
        {
            return !ProtoActorInventoryDisplayOrderScroll.TrySelectInDisplayOrder(__instance, delta: 1);
        }
    }

    // game@0.3.1 Assembly-CSharp/Mimic.Actors/ProtoActor+Inventory.cs:L2356-2365
    [HarmonyPatch]
    internal static class ProtoActorInventorySelectPreviousSlotPrefix
    {
        internal static MethodBase TargetMethod() => ProtoActorInventoryAccess.SelectPreviousSlotMethod;

        [HarmonyPrefix]
        private static bool Prefix(object __instance)
        {
            return !ProtoActorInventoryDisplayOrderScroll.TrySelectInDisplayOrder(__instance, delta: -1);
        }
    }

    internal static class ProtoActorInventoryDisplayOrderScroll
    {
        private const string Feature = "Ui";

        internal static bool TrySelectInDisplayOrder(object inventory, int delta)
        {
            if (!InventorySlotOptimizer.IsEnabled)
            {
                return false;
            }

            try
            {
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
                bool[] rawOccupied = InventorySlotLayout.BuildRawOccupied(slotItems, slotCount);
                int selectedIndex = ProtoActorInventoryAccess.GetSelectedSlotIndex(inventory);
                int targetSlot = InventorySlotLayout.StepRawSelection(rawOccupied, selectedIndex, delta);
                if (targetSlot != selectedIndex)
                {
                    ProtoActorInventoryAccess.SelectSlot(inventory, targetSlot);
                }

                return true;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Inventory display-order scroll failed — {ex.Message}");
                return false;
            }
        }
    }
}
