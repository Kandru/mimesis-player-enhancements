using System.Reflection;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.Statistics.Patches
{
    [HarmonyPatch(typeof(VPlayer), nameof(VPlayer.HandlePutIntoToilet))]
    internal static class VPlayerHandlePutIntoToiletPatches
    {
        [HarmonyPrefix]
        public static void Prefix(VPlayer __instance)
        {
            StatisticsPatchGuard.Run(nameof(VPlayer.HandlePutIntoToilet), () =>
            {
                TrainDepositTracker.BeginDeposit(__instance);
            });
        }

        [HarmonyPostfix]
        public static void Postfix(MsgErrorCode __result)
        {
            StatisticsPatchGuard.Run(nameof(VPlayer.HandlePutIntoToilet), () =>
            {
                if (__result != MsgErrorCode.Success)
                {
                    return;
                }

                TrainDepositTracker.CompleteDeposit();
            });
        }
    }

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
