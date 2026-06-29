using Bifrost.ConstEnum;
using MimesisPlayerEnhancement.Features.SpawnScaling;
using ReluProtocol.Enum;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    internal static class LootMultiplicatorLog
    {
        private const string Feature = "LootMultiplicator";

        internal static void InfoScalingApplied(int playerCount)
        {
            float sharedPlayerScale = playerCount > 4 ? playerCount / 4f : 1f;
            ModLog.Info(
                Feature,
                $"Loot scaling applied — players={playerCount} (shared playerScale={sharedPlayerScale:0.##}× when auto enabled), " +
                $"mapConsumable={ModConfig.MapConsumableLootMultiplier.Value:0.##}× auto={ModConfig.AutoScaleMapConsumableLootByPlayerCount.Value}, " +
                $"mapEquipment={ModConfig.MapEquipmentLootMultiplier.Value:0.##}× auto={ModConfig.AutoScaleMapEquipmentLootByPlayerCount.Value}, " +
                $"mapMiscellany={ModConfig.MapMiscellanyLootMultiplier.Value:0.##}× auto={ModConfig.AutoScaleMapMiscellanyLootByPlayerCount.Value}, " +
                $"dropConsumable={ModConfig.DropConsumableLootMultiplier.Value:0.##}× auto={ModConfig.AutoScaleDropConsumableLootByPlayerCount.Value}, " +
                $"dropEquipment={ModConfig.DropEquipmentLootMultiplier.Value:0.##}× auto={ModConfig.AutoScaleDropEquipmentLootByPlayerCount.Value}, " +
                $"dropMiscellany={ModConfig.DropMiscellanyLootMultiplier.Value:0.##}× auto={ModConfig.AutoScaleDropMiscellanyLootByPlayerCount.Value}, " +
                $"triggerConsumable={ModConfig.TriggerConsumableLootMultiplier.Value:0.##}× auto={ModConfig.AutoScaleTriggerConsumableLootByPlayerCount.Value}, " +
                $"triggerEquipment={ModConfig.TriggerEquipmentLootMultiplier.Value:0.##}× auto={ModConfig.AutoScaleTriggerEquipmentLootByPlayerCount.Value}, " +
                $"triggerMiscellany={ModConfig.TriggerMiscellanyLootMultiplier.Value:0.##}× auto={ModConfig.AutoScaleTriggerMiscellanyLootByPlayerCount.Value}");
        }

        internal static void InfoRuntimeScaled(
            LootSource source,
            ItemType itemType,
            int masterId,
            int before,
            int after,
            float multiplier,
            string context)
        {
            string name = ItemTypeLookup.GetDisplayName(masterId);
            ModLog.Info(
                Feature,
                $"{context} — source={source}, type={itemType}, item={name}, master={masterId}, " +
                $"{multiplier:0.##}× (count {before} -> {after})");
        }

        internal static void InfoDropTableScaled(int vanillaCount, int entriesAdded, int playerCount)
        {
            ModLog.Info(
                Feature,
                $"Drop table scaled — vanillaEntries={vanillaCount}, entriesAdded={entriesAdded}, players={playerCount}");
        }

        internal static void DebugFieldScaled(string label, int before, int after, float multiplier)
        {
            if (!ModConfig.EnableDebugLogging.Value)
            {
                return;
            }

            if (before == after)
            {
                ModLog.Debug(Feature, $"{label} unchanged at {before} ({multiplier:0.##}×)");
                return;
            }

            ModLog.Debug(Feature, $"{label} scaled {before} -> {after} ({multiplier:0.##}×)");
        }

        internal static void DebugLootScaled(
            LootSource source,
            ItemType itemType,
            int masterId,
            int before,
            int after,
            float multiplier,
            string context)
        {
            if (!ModConfig.EnableDebugLogging.Value)
            {
                return;
            }

            string name = ItemTypeLookup.GetDisplayName(masterId);
            ModLog.Debug(
                Feature,
                $"{context} — source={source}, type={itemType}, item={name}, master={masterId}, " +
                $"{multiplier:0.##}× (count {before} -> {after})");
        }

        internal static void DebugMapSlotConfigured(
            LootSource source,
            ItemType itemType,
            int masterId,
            int stackBefore,
            int stackAfter,
            float multiplier,
            string context)
        {
            if (!ModConfig.EnableDebugLogging.Value)
            {
                return;
            }

            string name = ItemTypeLookup.GetDisplayName(masterId);
            ModLog.Debug(
                Feature,
                $"Map loot slot configured — {context}, source={source}, type={itemType}, item={name}, master={masterId}, " +
                $"{multiplier:0.##}× (stack {stackBefore} -> {stackAfter})");
        }

        internal static void DebugLootSpawned(
            DungeonRoom? room,
            int masterId,
            string itemName,
            ItemType itemType,
            LootSource source,
            float effectiveMultiplier,
            bool scalingApplied,
            Vector3 position,
            bool isIndoor,
            ReasonOfSpawn reason,
            int stackCount,
            string spawnSource)
        {
            if (!ModConfig.EnableDebugLogging.Value)
            {
                return;
            }

            ModLog.Debug(
                Feature,
                $"Loot spawned — source={source}, type={itemType}, item={itemName}, master={masterId}, stack={stackCount}, " +
                $"multiplier={effectiveMultiplier:0.##}×, budgetsScaled={scalingApplied}, " +
                $"pos={SpawnScalingLog.FormatLocation(room, position)}, indoor={isIndoor}, reason={reason}, via={spawnSource}");
        }

        internal static void DebugLootSpawnFailed(
            int masterId,
            string itemName,
            ItemType itemType,
            LootSource source,
            bool scalingApplied,
            ReasonOfSpawn reason,
            string spawnSource)
        {
            if (!ModConfig.EnableDebugLogging.Value)
            {
                return;
            }

            ModLog.Debug(
                Feature,
                $"Loot spawn failed — source={source}, type={itemType}, item={itemName}, master={masterId}, " +
                $"budgetsScaled={scalingApplied}, reason={reason}, via={spawnSource}");
        }

        internal static void DebugExecuteLootSpawn(
            DungeonRoom room,
            SpawnedActorData spawnData,
            ItemType itemType,
            float multiplier,
            bool scalingApplied)
        {
            if (!ModConfig.EnableDebugLogging.Value)
            {
                return;
            }

            int masterId = ExtractMasterId(spawnData);
            int stackCount = ExtractStackCount(spawnData);
            string name = masterId > 0 ? ItemTypeLookup.GetDisplayName(masterId) : spawnData.Name;
            ModLog.Debug(
                Feature,
                $"ExecuteLootingObjectSpawn — type={itemType}, item={name}, master={masterId}, stack={stackCount}, " +
                $"multiplier={multiplier:0.##}×, budgetsScaled={scalingApplied}, pos={SpawnScalingLog.FormatLocation(room, spawnData.PosVector)}");
        }

        private static int ExtractMasterId(SpawnedActorData spawnData)
        {
            return spawnData switch
            {
                FixedSpawnedActorData fixedSpawn => fixedSpawn.MasterID,
                RandomSpawnedItemActorData random => random.MasterID,
                _ => 0,
            };
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
