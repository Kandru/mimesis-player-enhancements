using MimesisPlayerEnhancement.Features.SpawnScaling.Patches;

namespace MimesisPlayerEnhancement.Features.SpawnScaling
{
    internal static class SpawnScalingPatches
    {
        private const string Feature = "SpawnScaling";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();
            SceneScopedConfigGate.RegisterDungeonRunEndCleanup(ResetRuntimeState);

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
                ResetRuntimeState();
            }
        }

        /// <summary>Called via FeatureModule.onSessionEnded.</summary>
        public static void OnSessionEnded()
        {
            ResetRuntimeState();
        }

        internal static void ResetRuntimeState()
        {
            MapPlacedEncounterScheduler.ClearPendingEncounters();
            MapPlacedEncounterProximity.ClearCaches();
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            // game@0.3.1 Assembly-CSharp/DungeonRoom.cs:L206-315
            // game@0.3.1 Assembly-CSharp/DungeonRoom.cs:L317-534
            // game@0.3.1 Assembly-CSharp/SpawnedActorData.cs:L136-140
            // game@0.3.1 Assembly-CSharp/GroupSpawnData.cs:L62-79
            // game@0.3.1 Assembly-CSharp/IVroom.cs:L3920-3930
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
