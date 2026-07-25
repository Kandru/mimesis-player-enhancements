using System.Reflection;

namespace MimesisPlayerEnhancement.Features.UserInterface.InventorySlotOptimization.Patches
{
    // game@0.3.1 Assembly-CSharp/Mimic.Actors/ProtoActor+Inventory.cs:L2519-2522
    [HarmonyPatch]
    internal static class ProtoActorInventoryOnUpdateInvenSigPatches
    {
        private const string Feature = "Ui";

        internal static MethodBase TargetMethod() => ProtoActorInventoryAccess.OnUpdateInvenSigMethod;

        [HarmonyPrefix]
        private static void Prefix(object __instance, ref InventorySlotOptimizer.SlotSnapshot __state)
        {
            try
            {
                if (!InventorySlotOptimizer.TryBeginPatch(__instance, selectNextOnly: false, out InventorySlotOptimizer.SlotSnapshot snapshot))
                {
                    __state = default;
                    return;
                }

                __state = snapshot;
            }
            catch (Exception ex)
            {
                __state = default;
                ModLog.Warn(Feature, $"Inventory slot snapshot failed — {ex.Message}");
            }
        }

        [HarmonyPostfix]
        private static void Postfix(object __instance, InventorySlotOptimizer.SlotSnapshot __state)
        {
            if (!__state.IsValid)
            {
                return;
            }

            try
            {
                InventorySlotOptimizer.ApplyAfterChange(__instance, __state, allowPickup: true);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Inventory slot optimization failed — {ex.Message}");
            }
        }
    }

    // game@0.3.1 Assembly-CSharp/Mimic.Actors/ProtoActor+Inventory.cs:L2568-2584
    [HarmonyPatch]
    internal static class ProtoActorInventoryOnChangeItemLooksSigPatches
    {
        private const string Feature = "Ui";

        internal static MethodBase TargetMethod() => ProtoActorInventoryAccess.OnChangeItemLooksSigMethod;

        [HarmonyPrefix]
        private static void Prefix(object __instance, ref InventorySlotOptimizer.SlotSnapshot __state)
        {
            try
            {
                if (!InventorySlotOptimizer.TryBeginPatch(__instance, selectNextOnly: true, out InventorySlotOptimizer.SlotSnapshot snapshot))
                {
                    __state = default;
                    return;
                }

                __state = snapshot;
            }
            catch (Exception ex)
            {
                __state = default;
                ModLog.Warn(Feature, $"Inventory slot handheld snapshot failed — {ex.Message}");
            }
        }

        [HarmonyPostfix]
        private static void Postfix(object __instance, InventorySlotOptimizer.SlotSnapshot __state)
        {
            if (!__state.IsValid)
            {
                return;
            }

            try
            {
                InventorySlotOptimizer.ApplyAfterChange(__instance, __state, allowPickup: false);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Inventory slot handheld sync failed — {ex.Message}");
            }
        }
    }
}
