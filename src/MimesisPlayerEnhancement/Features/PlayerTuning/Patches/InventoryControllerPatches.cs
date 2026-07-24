namespace MimesisPlayerEnhancement.Features.PlayerTuning.Patches
{
    // game@0.3.1 Assembly-CSharp/InventoryController.cs:L1877-1900
    [HarmonyPatch(typeof(InventoryController), nameof(InventoryController.OnChangeInventory))]
    internal static class InventoryControllerOnChangeInventoryPatch
    {
        private const string Feature = "PlayerTuning";

        [HarmonyPostfix]
        public static void Postfix(InventoryController __instance)
        {
            try
            {
                if (!PlayerTuningApplier.ShouldApply)
                {
                    return;
                }

                PlayerTuningApplier.ApplyInventoryWeightPenalty(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"OnChangeInventory postfix failed — {ex.Message}");
            }
        }
    }
}
