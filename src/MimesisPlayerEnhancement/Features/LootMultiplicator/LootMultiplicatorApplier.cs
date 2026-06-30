using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Bifrost.ConstEnum;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    internal static class LootMultiplicatorApplier
    {
        private const string Feature = "LootMultiplicator";

        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo SpawnedActorDatasField =
            AccessToolsField(typeof(DungeonRoom), "_spawnedActorDatas");

        private static readonly HashSet<DungeonRoom> AppliedRooms = [];
        private static readonly HashSet<DungeonRoom> SkippedClientRooms = [];

        internal static bool IsApplied(DungeonRoom room)
        {
            return AppliedRooms.Contains(room);
        }

        internal static void EnsureApplied(DungeonRoom room)
        {
            if (AppliedRooms.Contains(room))
            {
                return;
            }

            if (HostApplyGate.IsParticipantClient())
            {
                if (SkippedClientRooms.Add(room))
                {
                    ModLog.Debug(Feature, "Loot scaling skipped — participant client");
                }

                return;
            }

            if (!HostApplyGate.ShouldApplyHostOnlyFeature())
            {
                ModLog.Debug(Feature, "Loot scaling deferred — waiting for host session");
                return;
            }

            Apply(room);
            _ = AppliedRooms.Add(room);
            FixedLootSpawnCoordinator.ApplyAfterInit(room);
        }

        internal static void Apply(DungeonRoom room)
        {
            if (!ModConfig.EnableLootMultiplicator.Value)
            {
                ModLog.Debug(Feature, "Loot scaling skipped — EnableLootMultiplicator is off");
                return;
            }

            int playerCount = room.GetMemberCount();
            LootMultiplicatorLog.InfoScalingApplied(playerCount);

            int scaled = ScaleSpawnPointDatas(room, playerCount);
            ModLog.Info(Feature, $"Map loot spawn data updated — scaledSlots={scaled}, players={playerCount}");
        }

        private static int ScaleSpawnPointDatas(DungeonRoom room, int playerCount)
        {
            if (SpawnedActorDatasField.GetValue(room) is not IDictionary datas)
            {
                return 0;
            }

            int scaled = 0;
            foreach (DictionaryEntry entry in datas)
            {
                if (entry.Value == null)
                {
                    continue;
                }

                scaled += ScaleLootSpawnData(entry.Value, playerCount) ? 1 : 0;
            }

            return scaled;
        }

        private static bool ScaleLootSpawnData(object spawnData, int playerCount)
        {
            return spawnData switch
            {
                RandomSpawnedItemActorData random => ScaleRandomLootSpawnData(random, playerCount),
                FixedSpawnedActorData fixedLoot when IsLootFixedSpawn(fixedLoot) =>
                    ScaleFixedLootSpawnData(fixedLoot, playerCount),
                _ => false,
            };
        }

        private static bool IsLootFixedSpawn(FixedSpawnedActorData spawnData)
        {
            return spawnData.MarkerType.Equals(MapMarkerType.LootingObject);
        }

        private static bool ScaleRandomLootSpawnData(RandomSpawnedItemActorData spawnData, int playerCount)
        {
            ItemType itemType = ItemTypeLookup.GetDominantItemType(spawnData.Candidates);
            float multiplier = LootMultiplierResolver.GetEffectiveMultiplier(LootSource.Map, itemType, playerCount);
            // Stack count is scaled at spawn time via RuntimeLootScaler (ReasonOfSpawn.Spawn).
            return ScaleCommonSpawnFields(
                spawnData,
                spawnData.MasterID,
                itemType,
                multiplier,
                "randomLoot",
                scaleStackCount: false);
        }

        private static bool ScaleFixedLootSpawnData(FixedSpawnedActorData spawnData, int playerCount)
        {
            ItemType itemType = ItemTypeLookup.GetItemType(spawnData.MasterID);
            float multiplier = LootMultiplierResolver.GetEffectiveMultiplier(
                LootSource.Map,
                itemType,
                playerCount,
                spawnData.MasterID);
            bool scaleStackCount = itemType.Equals(ItemType.Consumable);
            return ScaleCommonSpawnFields(
                spawnData,
                spawnData.MasterID,
                itemType,
                multiplier,
                "fixedLoot",
                scaleStackCount: scaleStackCount);
        }

        private static bool ScaleCommonSpawnFields(
            object spawnData,
            int masterId,
            ItemType itemType,
            float multiplier,
            string context,
            bool scaleStackCount)
        {
            bool scaled = false;
            int stackBefore = 0;
            int stackAfter = 0;
            bool stackTracked = false;

            FieldInfo? stackCountField = ReflectionFieldCache.GetField(spawnData, "StackCount");
            if (scaleStackCount && stackCountField != null)
            {
                stackBefore = (int)(stackCountField.GetValue(spawnData) ?? 0);
                stackAfter = LootMultiplierResolver.ScaleCountWithImplicitBase(stackBefore, multiplier, implicitWhenZero: 1);
                stackCountField.SetValue(spawnData, stackAfter);
                stackTracked = true;
                LootMultiplicatorLog.DebugFieldScaled(
                    $"{context}[{masterId}].stackCount",
                    stackBefore,
                    stackAfter,
                    multiplier);
                scaled = true;
            }

            FieldInfo? maxRespawnField = ReflectionFieldCache.GetField(spawnData, "MaxRespawnCount");
            if (maxRespawnField != null)
            {
                int respawnBefore = (int)(maxRespawnField.GetValue(spawnData) ?? 0);
                if (respawnBefore > 0)
                {
                    int respawnAfter = LootMultiplierResolver.ScaleCount(respawnBefore, multiplier);
                    maxRespawnField.SetValue(spawnData, respawnAfter);
                    LootMultiplicatorLog.DebugFieldScaled(
                        $"{context}[{masterId}].maxRespawn",
                        respawnBefore,
                        respawnAfter,
                        multiplier);
                    scaled = true;
                }
            }

            if (scaled && stackTracked)
            {
                LootMultiplicatorLog.DebugMapSlotConfigured(
                    LootSource.Map,
                    itemType,
                    masterId,
                    stackBefore,
                    stackAfter,
                    multiplier,
                    context);
            }

            return scaled;
        }

        private static FieldInfo AccessToolsField(Type type, string name)
        {
            FieldInfo? field = type.GetField(name, InstanceFlags);
            return field ?? throw new InvalidOperationException($"{type.Name}.{name} not found");
        }
    }
}
