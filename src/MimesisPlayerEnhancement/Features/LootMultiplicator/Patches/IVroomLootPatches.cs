using System.Reflection;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator.Patches
{
    [HarmonyPatch]
    internal static class IVroomExecuteLootingObjectSpawnPatch
    {
        private const string Feature = LootMultiplicatorPatchHelpers.Feature;

        public static MethodBase? TargetMethod()
        {
            return LootMultiplicatorPatchHelpers.ResolveExecuteLootingObjectSpawnMethod();
        }

        [HarmonyPrefix]
        public static void Prefix(IVroom __instance, SpawnedActorData spawnedActorData)
        {
            if (!LootScalingGate.ShouldScale() || __instance is not DungeonRoom dungeonRoom)
            {
                return;
            }

            try
            {
                LootMultiplicatorApplier.EnsureApplied(dungeonRoom);

                if (LootMultiplicatorPatchHelpers.IsMapFixedLootSpawn(spawnedActorData))
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
            if (LootMultiplicatorPatchHelpers.IsMapFixedLootSpawn(spawnedActorData))
            {
                MapLootSpawnContext.Exit();
            }

            if (!ModConfig.EnableDebugLogging.Value || !LootScalingGate.ShouldScale())
            {
                return;
            }

            if (__instance is not DungeonRoom dungeonRoom || spawnedActorData == null)
            {
                return;
            }

            if (!LootMultiplicatorPatchHelpers.IsLootSpawnData(spawnedActorData, out ItemType itemType, out int masterId))
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
                int stackCount = LootMultiplicatorPatchHelpers.ExtractStackCount(spawnedActorData);

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
    internal static class IVroomSpawnLootingObjectPatch
    {
        private const string Feature = LootMultiplicatorPatchHelpers.Feature;

        public static MethodBase? TargetMethod()
        {
            return LootMultiplicatorPatchHelpers.ResolveSpawnLootingObjectMethod();
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
            try
            {
                if (LootScalingGate.ShouldScale())
                {
                    FakeLootDropConverter.TryConvertActorDyingDrop(__instance, ref element, reasonOfSpawn);
                }

                if (element == null)
                {
                    __result = 0;
                    return false;
                }

                if (LootItemFilter.ShouldApply()
                    && !isRestored
                    && LootSourceResolver.TryResolveLootSource(reasonOfSpawn, spawnPointIndex, out LootSource filterSource)
                    && !filterSource.Equals(LootSource.Trigger)
                    && !LootItemFilter.TryPrepareSpawn(__instance, ref element))
                {
                    __result = 0;
                    return false;
                }

                if (element == null)
                {
                    __result = 0;
                    return false;
                }

                if (!LootScalingGate.ShouldScale())
                {
                    return true;
                }

                if (__instance is DungeonRoom dungeonRoom
                    && spawnPointIndex != 0
                    && LootSpawnDataLookup.TryFindByMarkerIndex(dungeonRoom, spawnPointIndex, out SpawnedActorData? spawnData)
                    && MapPlacedEncounterProximity.ShouldBlockBonusLootRespawn(dungeonRoom, spawnData))
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
            if (!ModConfig.EnableDebugLogging.Value || !LootScalingGate.ShouldScale())
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
}
