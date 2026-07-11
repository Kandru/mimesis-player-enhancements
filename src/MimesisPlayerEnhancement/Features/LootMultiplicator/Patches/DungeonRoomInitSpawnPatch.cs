namespace MimesisPlayerEnhancement.Features.LootMultiplicator.Patches
{
    [HarmonyPatch(typeof(DungeonRoom), "InitSpawn")]
    internal static class DungeonRoomInitSpawnPatch
    {
        private const string Feature = LootMultiplicatorPatchHelpers.Feature;

        [HarmonyPostfix]
        public static void Postfix(DungeonRoom __instance)
        {
            try
            {
                LootMultiplicatorApplier.EnsureApplied(__instance);
                LootItemFilter.ApplyToRandomSpawnDatas(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"InitSpawn postfix failed — {ex.Message}");
            }
        }
    }
}
