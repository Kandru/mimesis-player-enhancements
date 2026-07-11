using MimesisPlayerEnhancement.Features.SpawnScaling.Patches;

namespace MimesisPlayerEnhancement.Features.SpawnScaling
{
    internal static class SpawnScalingPatches
    {
        private const string Feature = "SpawnScaling";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();

            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(SpawnScalingPatches)));

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        /// <summary>Called via FeatureModule.SyncFromConfig when the SpawnScaling section changes.</summary>
        public static void RefreshFromConfig()
        {
            if (!ModConfig.EnableSpawnScaling.Value)
            {
                MapPlacedEncounterScheduler.ClearPendingEncounters();
            }
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("InitSpawn/DungeonRoom", AccessTools.Method(typeof(DungeonRoom), "InitSpawn")),
                ("ManageSpawnData/DungeonRoom", AccessTools.Method(typeof(DungeonRoom), "ManageSpawnData")),
                ("OnActorDead/SpawnedActorData", AccessTools.Method(typeof(SpawnedActorData), "OnActorDead")),
                ("OnMemberDead/GroupSpawnData", AccessTools.Method(typeof(GroupSpawnData), "OnMemberDead", [typeof(int)])),
                ("SpawnMonster/IVroom", SpawnScalingPatchHelpers.ResolveSpawnMonsterMethod()),
            ]);
        }
    }
}
