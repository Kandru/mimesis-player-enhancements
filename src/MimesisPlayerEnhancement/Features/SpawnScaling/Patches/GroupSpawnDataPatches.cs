namespace MimesisPlayerEnhancement.Features.SpawnScaling.Patches
{
    // game@0.3.1 Assembly-CSharp/GroupSpawnData.cs:L62-79
    [HarmonyPatch(typeof(GroupSpawnData), "OnMemberDead")]
    internal static class GroupSpawnDataOnMemberDeadPatch
    {
        private const string Feature = SpawnScalingPatchHelpers.Feature;

        [HarmonyPostfix]
        public static void Postfix(GroupSpawnData __instance, int actorID, bool __result)
        {
            if (!SceneScopedConfigGate.Spawn.EnableSpawnScaling)
            {
                return;
            }

            try
            {
                GroupSpawnBonusWaveApplier.OnMemberDead(__instance, __result);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"OnMemberDead group bonus wave failed — {ex.Message}");
            }
        }
    }
}
