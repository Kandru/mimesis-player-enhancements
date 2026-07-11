namespace MimesisPlayerEnhancement.Features.LootMultiplicator.Patches
{
    [HarmonyPatch(typeof(ItemDropInfo), "GetDropItemList")]
    internal static class ItemDropInfoGetDropItemListPatch
    {
        private const string Feature = LootMultiplicatorPatchHelpers.Feature;

        [HarmonyPostfix]
        public static void Postfix(ItemDropInfo __instance, List<int> __result)
        {
            try
            {
                DropLootTableScaler.ScaleDropList(__instance, __result);
                if (!BarterDropTableContext.IsActive)
                {
                    LootItemFilter.ApplyToDropList(__result, __instance);
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"GetDropItemList postfix failed — {ex.Message}");
            }
        }
    }
}
