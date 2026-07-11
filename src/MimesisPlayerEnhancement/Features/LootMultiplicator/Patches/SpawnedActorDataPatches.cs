namespace MimesisPlayerEnhancement.Features.LootMultiplicator.Patches
{
    [HarmonyPatch(typeof(SpawnedActorData), "OnActorDead")]
    internal static class SpawnedActorDataOnActorDeadPatch
    {
        private const string Feature = LootMultiplicatorPatchHelpers.Feature;

        [HarmonyPostfix]
        public static void Postfix(SpawnedActorData __instance)
        {
            try
            {
                FixedLootSpawnCoordinator.OnActorDead(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"OnActorDead fixed loot scaling failed — {ex.Message}");
            }
        }
    }
}
