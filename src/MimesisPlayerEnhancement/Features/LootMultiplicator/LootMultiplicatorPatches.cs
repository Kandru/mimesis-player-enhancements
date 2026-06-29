using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Bifrost.ConstEnum;
using Bifrost.Cooked;
using HarmonyLib;
using MimesisPlayerEnhancement.Features.SpawnScaling;
using MimesisPlayerEnhancement.Util;
using ReluProtocol;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    public static class LootMultiplicatorPatches
    {
        private const string Feature = "LootMultiplicator";
        private static readonly Type[] SpawnLootingObjectParameterTypes =
        [
            typeof(ItemElement),
            typeof(PosWithRot),
            typeof(bool),
            typeof(ReasonOfSpawn),
            typeof(int),
            typeof(int),
            typeof(long),
            typeof(bool),
            typeof(bool),
        ];

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();

            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNestedPatchTypes(typeof(LootMultiplicatorPatches)));

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        private static MethodBase? ResolveSpawnLootingObjectMethod()
        {
            return AccessTools.Method(typeof(IVroom), "SpawnLootingObject", SpawnLootingObjectParameterTypes);
        }

        private static MethodBase? ResolveExecuteLootingObjectSpawnMethod()
        {
            return AccessTools.Method(typeof(IVroom), "ExecuteLootingObjectSpawn", [typeof(SpawnedActorData)]);
        }

        private static MethodBase? ResolveRunEventActionInternalMethod()
        {
            return AccessTools.Method(typeof(IVroom), "RunEventActionInternal", [typeof(IGameAction), typeof(List<IGameActionParam>)]);
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("InitSpawn/DungeonRoom", AccessTools.Method(typeof(DungeonRoom), "InitSpawn")),
                ("ManageSpawnData/DungeonRoom", AccessTools.Method(typeof(DungeonRoom), "ManageSpawnData")),
                ("GetDropItemList/ItemDropInfo", AccessTools.Method(typeof(ItemDropInfo), "GetDropItemList")),
                ("RunEventActionInternal/IVroom", ResolveRunEventActionInternalMethod()),
                ("ExecuteLootingObjectSpawn/IVroom", ResolveExecuteLootingObjectSpawnMethod()),
                ("SpawnLootingObject/IVroom", ResolveSpawnLootingObjectMethod()),
                ("OnActorDead/SpawnedActorData", AccessTools.Method(typeof(SpawnedActorData), "OnActorDead")),
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
                    LootMultiplicatorApplier.EnsureApplied(__instance);
                    FixedLootSpawnCoordinator.ApplyAfterInit(__instance);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"InitSpawn postfix failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(DungeonRoom), "ManageSpawnData")]
        public static class DungeonRoomManageSpawnDataTranspiler
        {
            private static readonly MethodInfo RollScaledBudgetMethod =
                AccessTools.Method(typeof(RandomMapLootBudgetScaler), nameof(RandomMapLootBudgetScaler.RollScaledBudget))
                ?? throw new InvalidOperationException("RandomMapLootBudgetScaler.RollScaledBudget not found");

            private static readonly MethodInfo SimpleRandNextMethod =
                AccessTools.Method(typeof(SimpleRandUtil), "Next", [typeof(int), typeof(int)])
                ?? throw new InvalidOperationException("SimpleRandUtil.Next not found");

            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> codes = [.. instructions];

                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode != OpCodes.Call
                        || !ReferenceEquals(codes[i].operand, SimpleRandNextMethod))
                    {
                        continue;
                    }

                    if (!IsMiscBudgetRoll(codes, i))
                    {
                        continue;
                    }

                    codes.Insert(i, new CodeInstruction(OpCodes.Ldarg_0));
                    codes[i + 1] = new CodeInstruction(OpCodes.Call, RollScaledBudgetMethod);
                    break;
                }

                return codes;
            }

            private static bool IsMiscBudgetRoll(List<CodeInstruction> codes, int callIndex)
            {
                for (int j = Math.Max(0, callIndex - 8); j < callIndex; j++)
                {
                    if (codes[j].opcode == OpCodes.Ldfld
                        && codes[j].operand is FieldInfo field
                        && field.Name == "MiscMaxVal")
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(ItemDropInfo), "GetDropItemList")]
        public static class ItemDropInfoGetDropItemListPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ItemDropInfo __instance, List<int> __result)
            {
                try
                {
                    DropLootTableScaler.ScaleDropList(__instance, __result);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"GetDropItemList postfix failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch]
        public static class IVroomRunEventActionInternalPatch
        {
            public static MethodBase? TargetMethod()
            {
                return ResolveRunEventActionInternalMethod();
            }

            [HarmonyPostfix]
            public static void Postfix(
                IVroom __instance,
                IGameAction action,
                List<IGameActionParam> paramList,
                bool __result)
            {
                if (!__result || action is not GameActionSpawnItem spawnAction)
                {
                    return;
                }

                try
                {
                    GameActionParamPosition? positionParam =
                        GameActionParamHelper.FindParam<GameActionParamPosition>(paramList);
                    if (positionParam == null)
                    {
                        return;
                    }

                    TriggerLootTableScaler.TrySpawnExtraItems(__instance, spawnAction, positionParam);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"RunEventActionInternal trigger loot scaling failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch]
        public static class IVroomExecuteLootingObjectSpawnPatch
        {
            public static MethodBase? TargetMethod()
            {
                return ResolveExecuteLootingObjectSpawnMethod();
            }

            [HarmonyPrefix]
            public static void Prefix(IVroom __instance, SpawnedActorData spawnedActorData)
            {
                if (!ModConfig.EnableLootMultiplicator.Value || __instance is not DungeonRoom dungeonRoom)
                {
                    return;
                }

                try
                {
                    LootMultiplicatorApplier.EnsureApplied(dungeonRoom);

                    if (IsMapFixedLootSpawn(spawnedActorData))
                    {
                        MapLootSpawnContext.Enter();
                    }
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"ExecuteLootingObjectSpawn prefix failed — {ex.Message}");
                }
            }

            [HarmonyPostfix]
            public static void Postfix(IVroom __instance, SpawnedActorData spawnedActorData)
            {
                if (IsMapFixedLootSpawn(spawnedActorData))
                {
                    MapLootSpawnContext.Exit();
                }

                if (!ModConfig.EnableDebugLogging.Value || !ModConfig.EnableLootMultiplicator.Value)
                {
                    return;
                }

                if (__instance is not DungeonRoom dungeonRoom || spawnedActorData == null)
                {
                    return;
                }

                if (!IsLootSpawnData(spawnedActorData, out ItemType itemType, out int masterId))
                {
                    return;
                }

                try
                {
                    int playerCount = dungeonRoom.GetMemberCount();
                    float multiplier = LootMultiplierResolver.GetEffectiveMultiplier(
                        LootSource.Map,
                        itemType,
                        playerCount,
                        masterId);
                    bool scalingApplied = LootMultiplicatorApplier.IsApplied(dungeonRoom);
                    string itemName = masterId > 0 ? ItemTypeLookup.GetDisplayName(masterId) : spawnedActorData.Name;
                    int stackCount = ExtractStackCount(spawnedActorData);

                    LootMultiplicatorLog.DebugExecuteLootSpawn(
                        dungeonRoom,
                        spawnedActorData,
                        itemType,
                        multiplier,
                        scalingApplied);

                    LootMultiplicatorLog.DebugLootSpawned(
                        dungeonRoom,
                        masterId,
                        itemName,
                        itemType,
                        LootSource.Map,
                        multiplier,
                        scalingApplied,
                        spawnedActorData.PosVector,
                        spawnedActorData.IsIndoor,
                        ReasonOfSpawn.Spawn,
                        stackCount,
                        "ExecuteLootingObjectSpawn");
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"ExecuteLootingObjectSpawn postfix logging failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch]
        public static class IVroomSpawnLootingObjectPatch
        {
            public static MethodBase? TargetMethod()
            {
                return ResolveSpawnLootingObjectMethod();
            }

            [HarmonyPrefix]
            public static bool Prefix(
                IVroom __instance,
                ref ItemElement? element,
                ReasonOfSpawn reasonOfSpawn,
                int spawnPointIndex,
                bool isRestored,
                ref int __result)
            {
                if (!ModConfig.EnableLootMultiplicator.Value)
                {
                    return true;
                }

                try
                {
                    FakeLootDropConverter.TryConvertActorDyingDrop(__instance, ref element, reasonOfSpawn);
                    if (element == null)
                    {
                        __result = 0;
                        return false;
                    }

                    if (!LootSpawnScalingContext.IsDuplicating
                        && __instance is DungeonRoom dungeonRoom
                        && spawnPointIndex != 0
                        && LootSpawnDataLookup.TryFindByMarkerIndex(dungeonRoom, spawnPointIndex, out SpawnedActorData? spawnData)
                        && FixedSpawnProximity.ShouldBlockFixedLootRespawn(dungeonRoom, spawnData))
                    {
                        __result = 0;
                        return false;
                    }

                    if (__instance is DungeonRoom scalingRoom)
                    {
                        LootMultiplicatorApplier.EnsureApplied(scalingRoom);
                    }

                    RuntimeLootScaler.ScaleSpawnedItem(__instance, element, reasonOfSpawn, spawnPointIndex, isRestored);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"SpawnLootingObject prefix failed — {ex.Message}");
                }

                return true;
            }

            [HarmonyPostfix]
            public static void Postfix(
                IVroom __instance,
                ItemElement element,
                PosWithRot pos,
                bool isIndoor,
                ReasonOfSpawn reasonOfSpawn,
                int spawnPointIndex,
                int prevProjectileActorID,
                long projectileDropTime,
                bool ignoreNav,
                bool isRestored,
                int __result)
            {
                if (!ModConfig.EnableDebugLogging.Value || !ModConfig.EnableLootMultiplicator.Value)
                {
                    return;
                }

                if (element == null || !RuntimeLootScaler.TryMapReasonToSource(reasonOfSpawn, out LootSource source))
                {
                    return;
                }

                DungeonRoom? dungeonRoom = __instance as DungeonRoom;
                ItemType itemType = ItemElementStackHelper.GetItemType(element);
                int masterId = element.ItemMasterID;
                string itemName = ItemTypeLookup.GetDisplayName(masterId);
                int playerCount = SessionPlayerCountHelper.ResolveFromRoom(__instance);
                float multiplier = LootMultiplierResolver.GetEffectiveMultiplier(
                    source,
                    itemType,
                    playerCount,
                    masterId);
                bool scalingApplied = dungeonRoom != null && LootMultiplicatorApplier.IsApplied(dungeonRoom);
                int stackCount = ItemElementStackHelper.GetStackCount(element);

                if (__result > 0)
                {
                    LootMultiplicatorLog.DebugLootSpawned(
                        dungeonRoom,
                        masterId,
                        itemName,
                        itemType,
                        source,
                        multiplier,
                        scalingApplied,
                        pos.pos,
                        isIndoor,
                        reasonOfSpawn,
                        stackCount,
                        "SpawnLootingObject");
                }
                else
                {
                    LootMultiplicatorLog.DebugLootSpawnFailed(
                        masterId,
                        itemName,
                        itemType,
                        source,
                        scalingApplied,
                        reasonOfSpawn,
                        "SpawnLootingObject");
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
                    FixedLootSpawnCoordinator.OnActorDead(__instance);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"OnActorDead fixed loot scaling failed — {ex.Message}");
                }
            }
        }

        private static bool IsMapFixedLootSpawn(SpawnedActorData? spawnData)
        {
            return spawnData is FixedSpawnedActorData fixedSpawn
            && fixedSpawn.MarkerType.Equals(MapMarkerType.LootingObject)
            && fixedSpawn.MasterID > 0;
        }

        private static bool IsLootSpawnData(SpawnedActorData spawnData, out ItemType itemType, out int masterId)
        {
            switch (spawnData)
            {
                case RandomSpawnedItemActorData random:
                    itemType = ItemTypeLookup.GetDominantItemType(random.Candidates);
                    masterId = random.MasterID;
                    return true;
                case FixedSpawnedActorData fixedSpawn
                    when fixedSpawn.MarkerType.Equals(MapMarkerType.LootingObject)
                         && ItemTypeLookup.TryGetItem(fixedSpawn.MasterID, out _):
                    itemType = ItemTypeLookup.GetItemType(fixedSpawn.MasterID);
                    masterId = fixedSpawn.MasterID;
                    return true;
                default:
                    itemType = default;
                    masterId = 0;
                    return false;
            }
        }

        private static int ExtractStackCount(SpawnedActorData spawnData)
        {
            return spawnData switch
            {
                FixedSpawnedActorData fixedSpawn => fixedSpawn.StackCount,
                RandomSpawnedItemActorData random => random.StackCount,
                _ => 0,
            };
        }
    }
}
