using System.Reflection;
using ReluProtocol;
using ReluProtocol.Enum;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.SpawnScaling
{
    public static class SpawnScalingPatches
    {
        private const string Feature = "SpawnScaling";

        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Type[] SpawnMonsterParameterTypes =
        [
            typeof(int),
            typeof(SpawnedActorData),
            typeof(bool),
            typeof(string),
            typeof(string),
            typeof(ReasonOfSpawn),
        ];

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();

            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNestedPatchTypes(typeof(SpawnScalingPatches)));

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

        private static MethodBase? ResolveSpawnMonsterMethod()
        {
            return AccessTools.Method(typeof(IVroom), "SpawnMonster", SpawnMonsterParameterTypes);
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("InitSpawn/DungeonRoom", AccessTools.Method(typeof(DungeonRoom), "InitSpawn")),
                ("ManageSpawnData/DungeonRoom", AccessTools.Method(typeof(DungeonRoom), "ManageSpawnData")),
                ("OnActorDead/SpawnedActorData", AccessTools.Method(typeof(SpawnedActorData), "OnActorDead")),
                ("OnMemberDead/GroupSpawnData", AccessTools.Method(typeof(GroupSpawnData), "OnMemberDead", [typeof(int)])),
                ("SpawnMonster/IVroom", ResolveSpawnMonsterMethod()),
            ]);
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
                    MapPlacedEncounterScheduler.ApplyAfterInit(__instance);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"InitSpawn postfix failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(DungeonRoom), "ManageSpawnData")]
        public static class DungeonRoomManageSpawnDataPatch
        {
            [ThreadStatic]
            private static ManageSpawnDataSnapshot? _snapshot;

            [HarmonyPrefix]
            public static void Prefix(DungeonRoom __instance)
            {
                try
                {
                    _snapshot = PeriodicSpawnWaitApplier.CaptureSnapshot(__instance);
                    SpawnTimingOverrideApplier.BeginManageSpawnData(__instance);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"ManageSpawnData prefix failed — {ex.Message}");
                }
            }

            [HarmonyPostfix]
            public static void Postfix(DungeonRoom __instance)
            {
                try
                {
                    if (_snapshot != null)
                    {
                        PeriodicSpawnWaitApplier.OnManageSpawnDataPostfix(__instance, _snapshot.Value);
                    }
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"ManageSpawnData postfix failed — {ex.Message}");
                }
            }

            [HarmonyFinalizer]
            public static void Finalizer(DungeonRoom __instance)
            {
                try
                {
                    SpawnTimingOverrideApplier.EndManageSpawnData(__instance);
                    _snapshot = null;
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"ManageSpawnData finalizer failed — {ex.Message}");
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
                    MapPlacedEncounterScheduler.OnActorDead(__instance);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"OnActorDead map-placed encounter scaling failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(GroupSpawnData), "OnMemberDead")]
        public static class GroupSpawnDataOnMemberDeadPatch
        {
            [HarmonyPostfix]
            public static void Postfix(GroupSpawnData __instance, int actorID, bool __result)
            {
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

        [HarmonyPatch]
        public static class IVroomSpawnMonsterPatch
        {
            public static MethodBase? TargetMethod()
            {
                return ResolveSpawnMonsterMethod();
            }

            [HarmonyPrefix]
            public static bool Prefix(
                IVroom __instance,
                SpawnedActorData spawnData,
                ref bool __result)
            {
                if (!ModConfig.EnableSpawnScaling.Value
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
                if (!ModConfig.EnableDebugLogging.Value || !ModConfig.EnableSpawnScaling.Value)
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

                FieldInfo posVectorField = spawnData.GetType().GetField("PosVector", InstanceFlags);
                if (posVectorField?.GetValue(spawnData) is Vector3 posVector)
                {
                    return posVector;
                }

                FieldInfo posField = spawnData.GetType().GetField("Pos", InstanceFlags);
                return posField?.GetValue(spawnData) is PosWithRot posWithRot ? posWithRot.pos : Vector3.zero;
            }
        }
    }
}
