using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.PlayerTuning.Patches
{
    [HarmonyPatch(typeof(MappedStats), nameof(MappedStats.LoadBaseStats))]
    internal static class MappedStatsLoadBaseStatsPatch
    {
        private const string Feature = "PlayerTuning";

        [HarmonyPostfix]
        public static void Postfix(MappedStats __instance, ActorType type, bool __result)
        {
            try
            {
                if (!PlayerTuningApplier.ShouldApply || !__result || type != ActorType.Player)
                {
                    return;
                }

                PlayerTuningApplier.ApplyMappedPlayerStats(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"LoadBaseStats postfix failed — {ex.Message}");
            }
        }
    }

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
