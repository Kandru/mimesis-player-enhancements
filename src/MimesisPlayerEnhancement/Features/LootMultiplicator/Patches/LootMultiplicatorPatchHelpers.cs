using System.Reflection;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator.Patches
{
    internal static class LootMultiplicatorPatchHelpers
    {
        internal const string Feature = "LootMultiplicator";

        internal static readonly Type[] SpawnLootingObjectParameterTypes =
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

        internal static MethodBase? ResolveSpawnLootingObjectMethod()
        {
            return AccessTools.Method(typeof(IVroom), "SpawnLootingObject", SpawnLootingObjectParameterTypes);
        }

        internal static MethodBase? ResolveExecuteLootingObjectSpawnMethod()
        {
            return AccessTools.Method(typeof(IVroom), "ExecuteLootingObjectSpawn", [typeof(SpawnedActorData)]);
        }

        internal static bool IsMapFixedLootSpawn(SpawnedActorData? spawnData)
        {
            return spawnData is FixedSpawnedActorData fixedSpawn
            && fixedSpawn.MarkerType.Equals(MapMarkerType.LootingObject)
            && fixedSpawn.MasterID > 0;
        }

        internal static bool IsLootSpawnData(SpawnedActorData spawnData, out ItemType itemType, out int masterId)
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

        internal static int ExtractStackCount(SpawnedActorData spawnData)
        {
            return spawnData switch
            {
                FixedSpawnedActorData fixedSpawn => fixedSpawn.StackCount,
                RandomSpawnedItemActorData random => random.StackCount,
                _ => 0,
            };
        }

        /// <summary>Called via FeatureModule.SyncFromConfig when the LootMultiplicator section changes.</summary>
        internal static void RefreshFromConfig()
        {
            if (SceneScopedConfigGate.IsModuleSyncDeferred("LootMultiplicator"))
            {
                return;
            }

            LootItemFilter.ReloadFromSceneSnapshot();

            if (!ModConfig.EnableLootMultiplicator.Value)
            {
                FixedLootSpawnCoordinator.ClearPendingRespawns();
            }
        }

        internal static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("InitSpawn/DungeonRoom", AccessTools.Method(typeof(DungeonRoom), "InitSpawn")),
                ("ManageSpawnData/DungeonRoom", AccessTools.Method(typeof(DungeonRoom), "ManageSpawnData")),
                ("GetDropItemList/ItemDropInfo", AccessTools.Method(typeof(ItemDropInfo), "GetDropItemList")),
                ("ExecuteLootingObjectSpawn/IVroom", ResolveExecuteLootingObjectSpawnMethod()),
                ("SpawnLootingObject/IVroom", ResolveSpawnLootingObjectMethod()),
                ("OnActorDead/SpawnedActorData", AccessTools.Method(typeof(SpawnedActorData), "OnActorDead")),
                ("BarterItem/InventoryController", AccessTools.Method(typeof(InventoryController), "BarterItem", [typeof(PosWithRot)])),
                ("ExtractRoomInfo/DeathMatchRoom", AccessTools.Method(typeof(DeathMatchRoom), "ExtractRoomInfo", [typeof(bool)])),
            ]);
        }
    }
}
