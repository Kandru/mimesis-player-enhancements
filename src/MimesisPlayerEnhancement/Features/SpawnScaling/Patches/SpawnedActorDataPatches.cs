namespace MimesisPlayerEnhancement.Features.SpawnScaling.Patches
{
    // game@0.3.1 Assembly-CSharp/SpawnedActorData.cs:L136-140
    [HarmonyPatch(typeof(SpawnedActorData), "OnActorDead")]
    internal static class SpawnedActorDataOnActorDeadPatch
    {
        private const string Feature = SpawnScalingPatchHelpers.Feature;

        [HarmonyPostfix]
        public static void Postfix(SpawnedActorData __instance)
        {
            // Runs on every actor death — skip entirely when spawn scaling is off.
            if (!SceneScopedConfigGate.Spawn.EnableSpawnScaling)
            {
                return;
            }

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
