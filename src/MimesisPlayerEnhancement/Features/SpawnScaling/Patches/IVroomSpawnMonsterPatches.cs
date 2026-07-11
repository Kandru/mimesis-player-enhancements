using System.Reflection;
using ReluProtocol.Enum;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.SpawnScaling.Patches
{
    [HarmonyPatch]
    internal static class IVroomSpawnMonsterPatch
    {
        private const string Feature = SpawnScalingPatchHelpers.Feature;

        public static MethodBase? TargetMethod()
        {
            return SpawnScalingPatchHelpers.ResolveSpawnMonsterMethod();
        }

        [HarmonyPrefix]
        public static bool Prefix(
            IVroom __instance,
            SpawnedActorData spawnData,
            ref bool __result)
        {
            if (!SceneScopedConfigGate.Spawn.EnableSpawnScaling
                || __instance is not DungeonRoom dungeonRoom
                || !HostApplyGate.ShouldApplyHostOnlyFeature())
            {
                return true;
            }

            if (MapPlacedEncounterProximity.ShouldBlockBonusEncounterSpawn(dungeonRoom, spawnData))
            {
                __result = false;
                return false;
            }

            return true;
        }

        [HarmonyPostfix]
        public static void Postfix(
            IVroom __instance,
            int masterID,
            SpawnedActorData spawnData,
            bool isIndoor,
            string aiName,
            string btName,
            ReasonOfSpawn reasonOfSpawn,
            bool __result)
        {
            if (!ModConfig.EnableDebugLogging.Value || !SceneScopedConfigGate.Spawn.EnableSpawnScaling)
            {
                return;
            }

            if (__instance is not DungeonRoom dungeonRoom)
            {
                return;
            }

            int playerCount = dungeonRoom.GetMemberCount();
            SpawnCategory category = SpawnCategoryLookup.GetCategory(masterID);
            float multiplier = SpawnMultiplierResolver.GetEffectiveMultiplier(category, playerCount);
            string entityName = MonsterTypeLookup.GetDisplayName(masterID);
            bool scalingApplied = SpawnScalingApplier.IsApplied(dungeonRoom);

            if (__result)
            {
                SpawnScalingLog.DebugEntitySpawned(
                    dungeonRoom,
                    masterID,
                    entityName,
                    category,
                    multiplier,
                    scalingApplied,
                    ExtractSpawnPosition(spawnData),
                    isIndoor,
                    reasonOfSpawn,
                    "SpawnMonster");
            }
            else
            {
                SpawnScalingLog.DebugSpawnFailed(masterID, entityName, category, scalingApplied, "SpawnMonster");
            }
        }

        private static Vector3 ExtractSpawnPosition(SpawnedActorData spawnData)
        {
            if (spawnData == null)
            {
                return Vector3.zero;
            }

            FieldInfo posVectorField = spawnData.GetType().GetField("PosVector", SpawnScalingPatchHelpers.InstanceFlags);
            if (posVectorField?.GetValue(spawnData) is Vector3 posVector)
            {
                return posVector;
            }

            FieldInfo posField = spawnData.GetType().GetField("Pos", SpawnScalingPatchHelpers.InstanceFlags);
            return posField?.GetValue(spawnData) is PosWithRot posWithRot ? posWithRot.pos : Vector3.zero;
        }
    }
}
