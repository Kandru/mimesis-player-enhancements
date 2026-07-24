using System.Reflection;

namespace MimesisPlayerEnhancement.Features.Statistics.Patches
{
    // game@0.3.1 Assembly-CSharp/InventoryController.cs:L1956-1977
    [HarmonyPatch]
    internal static class InventoryControllerOnAddItemByLootingPatches
    {
        private static readonly FieldInfo? SelfField =
            AccessTools.Field(typeof(InventoryController), "_self");

        private static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(InventoryController), "OnAddItemByLooting", [typeof(ItemElement)])
            ?? throw new InvalidOperationException("InventoryController.OnAddItemByLooting not found");

        [HarmonyPostfix]
        public static void Postfix(InventoryController __instance, ItemElement itemElement)
        {
            StatisticsPatchGuard.Run("InventoryController.OnAddItemByLooting", () =>
            {
                if (itemElement == null || SelfField?.GetValue(__instance) is not VPlayer player)
                {
                    return;
                }

                TrainDepositTracker.OnItemPickedUp(itemElement, player.SteamID);
            });
        }
    }
}
