using Mimic;

namespace MimesisPlayerEnhancement.Features.Economy.Patches
{
    // game@0.3.1 Assembly-CSharp/Mimic/InventoryItem.cs:L118-128
    [HarmonyPatch(typeof(InventoryItem), nameof(InventoryItem.ReinforceCost), MethodType.Getter)]
    internal static class InventoryItemReinforceCostPatch
    {
        private const string Feature = "Economy";

        [HarmonyPostfix]
        public static void Postfix(ref int __result)
        {
            if (!EconomyApplier.IsEnabled() || __result <= 0)
            {
                return;
            }

            try
            {
                __result = EconomyApplier.ScaleReinforcePrice(__result);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"ReinforceCost postfix failed — {ex.Message}");
            }
        }
    }
}
