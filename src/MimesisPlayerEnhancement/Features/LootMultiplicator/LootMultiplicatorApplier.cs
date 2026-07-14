using System.Collections;
using System.Reflection;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    internal static class LootMultiplicatorApplier
    {
        private const string Feature = "LootMultiplicator";

        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo SpawnedActorDatasField =
            AccessToolsField(typeof(DungeonRoom), "_spawnedActorDatas");

        internal static bool IsApplied(DungeonRoom room)
        {
            return DungeonRoomAppliedSet.IsApplied(room, DungeonRoomApplyKind.LootScaling);
        }

        internal static void EnsureApplied(DungeonRoom room)
        {
            if (DungeonRoomAppliedSet.IsApplied(room, DungeonRoomApplyKind.LootScaling))
            {
                return;
            }

            if (HostApplyGate.IsParticipantClient())
            {
                if (DungeonRoomAppliedSet.MarkSkippedOnce(room, DungeonRoomApplyKind.LootScalingSkippedOnce))
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
            DungeonRoomAppliedSet.MarkApplied(room, DungeonRoomApplyKind.LootScaling);
            FixedLootSpawnCoordinator.ApplyAfterInit(room);
        }

        internal static void Apply(DungeonRoom room)
        {
            if (!LootScalingGate.ShouldScale(room))
            {
                ModLog.Debug(Feature, "Loot scaling skipped — EnableLootMultiplicator is off");
                return;
            }

            int playerCount = room.GetMemberCount();
            LootMultiplicatorSceneConfig config = SceneScopedConfigGate.Loot;
            LootMultiplicatorLog.InfoScalingApplied(playerCount);

            int scaled = ScaleSpawnPointDatas(room, playerCount, config);
            ModLog.Info(Feature, $"Map loot spawn data updated — scaledSlots={scaled}, players={playerCount}");
        }

        private static int ScaleSpawnPointDatas(DungeonRoom room, int playerCount, LootMultiplicatorSceneConfig config)
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

                scaled += ScaleLootSpawnData(entry.Value, playerCount, config) ? 1 : 0;
            }

            return scaled;
        }

        private static bool ScaleLootSpawnData(object spawnData, int playerCount, LootMultiplicatorSceneConfig config)
        {
            return spawnData switch
            {
                RandomSpawnedItemActorData random => ScaleRandomLootSpawnData(random, playerCount, config),
                FixedSpawnedActorData fixedLoot when IsLootFixedSpawn(fixedLoot) =>
                    ScaleFixedLootSpawnData(fixedLoot, playerCount, config),
                _ => false,
            };
        }

        private static bool IsLootFixedSpawn(FixedSpawnedActorData spawnData)
        {
            return spawnData.MarkerType.Equals(MapMarkerType.LootingObject);
        }

        private static bool ScaleRandomLootSpawnData(
            RandomSpawnedItemActorData spawnData,
            int playerCount,
            LootMultiplicatorSceneConfig config)
        {
            ItemType itemType = ItemTypeLookup.GetDominantItemType(spawnData.Candidates);
            float multiplier = LootMultiplierResolver.GetEffectiveMultiplier(LootSource.Map, itemType, playerCount, masterId: 0, config);
            // Stack count is scaled at spawn time via RuntimeLootScaler (ReasonOfSpawn.Spawn).
            return ScaleCommonSpawnFields(
                spawnData,
                spawnData.MasterID,
                itemType,
                multiplier,
                "randomLoot",
                scaleStackCount: false);
        }

        private static bool ScaleFixedLootSpawnData(
            FixedSpawnedActorData spawnData,
            int playerCount,
            LootMultiplicatorSceneConfig config)
        {
            ItemType itemType = ItemTypeLookup.GetItemType(spawnData.MasterID);
            float multiplier = LootMultiplierResolver.GetEffectiveMultiplier(
                LootSource.Map,
                itemType,
                playerCount,
                spawnData.MasterID,
                config);
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
            if (multiplier <= FeatureToggleGate.NeutralMultiplier)
            {
                return false;
            }

            bool scaled = false;
            int stackBefore = 0;
            int stackAfter = 0;
            bool stackTracked = false;

            if (scaleStackCount
                && SpawnDataFieldScaler.TryScaleStackCount(spawnData, multiplier, out stackBefore, out stackAfter))
            {
                stackTracked = true;
                LootMultiplicatorLog.DebugFieldScaled(
                    $"{context}[{masterId}].stackCount",
                    stackBefore,
                    stackAfter,
                    multiplier);
                scaled = true;
            }

            if (SpawnDataFieldScaler.TryScaleMaxRespawnCount(spawnData, multiplier, out int respawnBefore, out int respawnAfter))
            {
                LootMultiplicatorLog.DebugFieldScaled(
                    $"{context}[{masterId}].maxRespawn",
                    respawnBefore,
                    respawnAfter,
                    multiplier);
                scaled = true;
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
