namespace MimesisPlayerEnhancement.Features.SpawnScaling.Patches
{
    [HarmonyPatch(typeof(SpawnedActorData), "OnActorDead")]
    internal static class SpawnedActorDataOnActorDeadPatch
    {
        private const string Feature = SpawnScalingPatchHelpers.Feature;

        [HarmonyPostfix]
        public static void Postfix(SpawnedActorData __instance)
        {
            try
            {
                MapPlacedEncounterScheduler.OnActorDead(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"OnActorDead map-placed encounter scaling failed — {ex.Message}");
            }
        }
    }
}
