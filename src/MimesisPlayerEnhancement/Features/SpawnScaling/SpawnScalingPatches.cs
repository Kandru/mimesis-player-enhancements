using System;
using System.Reflection;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;
using ReluProtocol;
using ReluProtocol.Enum;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.SpawnScaling;

public static class SpawnScalingPatches
{
    private const string Feature = "SpawnScaling";

    private const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly Type[] SpawnMonsterParameterTypes =
    {
        typeof(int),
        typeof(SpawnedActorData),
        typeof(bool),
        typeof(string),
        typeof(string),
        typeof(ReasonOfSpawn),
    };

    public static void Apply(HarmonyLib.Harmony harmony)
    {
        _ = GameNetworkApi.GetGameAssembly();

        var result = HarmonyPatchHelper.ApplyPatchTypes(
            harmony,
            Feature,
            HarmonyPatchHelper.GetNestedPatchTypes(typeof(SpawnScalingPatches)));

        LogPatchAudit(harmony);
        HarmonyPatchHelper.LogPatchSummary(Feature, result);
    }

    private static MethodBase? ResolveSpawnMonsterMethod() =>
        AccessTools.Method(typeof(IVroom), "SpawnMonster", SpawnMonsterParameterTypes);

    private static void LogPatchAudit(HarmonyLib.Harmony harmony)
    {
        HarmonyPatchHelper.LogPatchAudit(Feature, harmony, new (string, MethodBase?)[]
        {
            ("InitSpawn/DungeonRoom", AccessTools.Method(typeof(DungeonRoom), "InitSpawn")),
            ("OnActorDead/SpawnedActorData", AccessTools.Method(typeof(SpawnedActorData), "OnActorDead")),
            ("SpawnMonster/IVroom", ResolveSpawnMonsterMethod()),
        });
    }

    [HarmonyPatch(typeof(DungeonRoom), "InitSpawn")]
    public static class DungeonRoomInitSpawnPatch
    {
        [HarmonyPostfix]
        public static void Postfix(DungeonRoom __instance)
        {
            try
            {
                SpawnScalingApplier.EnsureApplied(__instance);
                FixedSpawnCoordinator.ApplyAfterInit(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"InitSpawn postfix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(SpawnedActorData), "OnActorDead")]
    public static class SpawnedActorDataOnActorDeadPatch
    {
        [HarmonyPostfix]
        public static void Postfix(SpawnedActorData __instance)
        {
            try
            {
                FixedSpawnCoordinator.OnActorDead(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"OnActorDead fixed spawn scaling failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch]
    public static class IVroomSpawnMonsterPatch
    {
        public static MethodBase? TargetMethod() => ResolveSpawnMonsterMethod();

        [HarmonyPrefix]
        public static void Prefix(IVroom __instance)
        {
            if (!ModConfig.EnableSpawnScaling.Value || __instance is not DungeonRoom dungeonRoom)
                return;

            try
            {
                SpawnScalingApplier.EnsureApplied(dungeonRoom);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"SpawnMonster prefix scaling failed — {ex.Message}");
            }
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
            if (!ModConfig.EnableDebugLogging.Value || !ModConfig.EnableSpawnScaling.Value)
                return;

            if (__instance is not DungeonRoom dungeonRoom)
                return;

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
                return Vector3.zero;

            var posVectorField = spawnData.GetType().GetField("PosVector", InstanceFlags);
            if (posVectorField?.GetValue(spawnData) is Vector3 posVector)
                return posVector;

            var posField = spawnData.GetType().GetField("Pos", InstanceFlags);
            if (posField?.GetValue(spawnData) is PosWithRot posWithRot)
                return posWithRot.pos;

            return Vector3.zero;
        }
    }
}
