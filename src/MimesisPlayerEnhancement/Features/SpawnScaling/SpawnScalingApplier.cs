using System;
using System.Collections;
using System.Collections.Generic;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.SpawnScaling
{
    internal static class SpawnScalingApplier
    {
        private const string Feature = "SpawnScaling";

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
                    ModLog.Debug(Feature, "Spawn scaling skipped — participant client");
                }

                return;
            }

            if (!HostApplyGate.ShouldApplyHostOnlyFeature())
            {
                ModLog.Debug(Feature, "Spawn scaling deferred — waiting for host session");
                return;
            }

            Apply(room);
            _ = AppliedRooms.Add(room);
        }

        internal static void Apply(DungeonRoom room)
        {
            if (!ModConfig.EnableSpawnScaling.Value)
            {
                ModLog.Debug(Feature, "Spawn scaling skipped — EnableSpawnScaling is off");
                return;
            }

            int playerCount = room.GetMemberCount();
            SpawnScalingLog.InfoScalingApplied(playerCount);

            float mimicMultiplier = SpawnMultiplierResolver.GetEffectiveMultiplier(SpawnCategory.Mimic, playerCount);
            float jakoMultiplier = SpawnMultiplierResolver.GetEffectiveMultiplier(SpawnCategory.Jako, playerCount);

            int mimicMax = ScaleField(room, SpawnScalingFields.MimicSpawnCountMaxField, mimicMultiplier, "mimicSpawnCountMax");
            int mimicRemain = ScaleField(room, SpawnScalingFields.MimicSpawnCountRemainField, mimicMultiplier, "mimicSpawnCountRemain");
            int threatLimit = ScaleField(room, SpawnScalingFields.NormalMonsterThreatLimitField, jakoMultiplier, "normalMonsterThreatLimit");
            int threatRemain = ScaleField(room, SpawnScalingFields.NormalMonsterThreatRemainField, jakoMultiplier, "normalMonsterThreatRemain");
            int threatMin = ScaleField(
                room,
                SpawnScalingFields.NormalMonsterSpawnThreatMinThresholdField,
                jakoMultiplier,
                "normalMonsterSpawnThreatMinThreshold");

            int specialGroups = ScaleSpecialGroups(room, playerCount);
            int spawnPoints = ScaleSpawnPointDatas(room, playerCount);
            int bonusGroupWaves = ConfigureBonusGroupWaves(room, playerCount);

            RoomSpawnScalingState state = RoomSpawnScalingRegistry.GetOrCreate(room);
            if (SpawnScalingFields.DungeonMasterInfoField.GetValue(room) is DungeonMasterInfo dungeonInfo)
            {
                SpawnTimingOverrideApplier.ConfigureTimingOverrides(room, state, dungeonInfo, jakoMultiplier, mimicMultiplier);
            }

            ModLog.Info(
                Feature,
                $"Spawn budgets updated — mimic {mimicMultiplier:0.##}× (max={mimicMax}, remain={mimicRemain}), " +
                $"jako {jakoMultiplier:0.##}× (limit={threatLimit}, remain={threatRemain}, min={threatMin}), " +
                $"specialGroups={specialGroups}, spawnPoints={spawnPoints}, bonusGroupWaves={bonusGroupWaves}");
        }

        private static int ScaleSpecialGroups(DungeonRoom room, int playerCount)
        {
            if (SpawnScalingFields.SpecialMonsterSpawnGroupsField.GetValue(room) is not IList groups)
            {
                return 0;
            }

            int scaled = 0;
            foreach (object group in groups)
            {
                if (group == null)
                {
                    continue;
                }

                object? info = SpawnScalingFields.SpecialGroupInfoField.GetValue(group);
                if (info is not SpecialMonsterSpawnInfo spawnInfo)
                {
                    continue;
                }

                float multiplier = SpawnMultiplierResolver.GetEffectiveMultiplier(spawnInfo.MasterID, playerCount);
                SpawnCategory category = SpawnCategoryLookup.GetCategory(spawnInfo.MasterID);
                string entityName = MonsterTypeLookup.GetDisplayName(spawnInfo.MasterID);

                int max = ScaleField(group, SpawnScalingFields.SpecialGroupSpawnCountMaxField, multiplier, $"specialGroup[{spawnInfo.MasterID}].spawnCountMax");
                int remain = ScaleField(group, SpawnScalingFields.SpecialGroupSpawnCountRemainField, multiplier, $"specialGroup[{spawnInfo.MasterID}].spawnCountRemain");
                _ = ScaleField(spawnInfo, SpawnScalingFields.SpecialSpawnInfoSpawnCountMinField, multiplier, $"specialGroup[{spawnInfo.MasterID}].spawnCountMin");
                _ = ScaleField(spawnInfo, SpawnScalingFields.SpecialSpawnInfoSpawnCountMaxField, multiplier, $"specialGroup[{spawnInfo.MasterID}].spawnCountMaxInfo");

                scaled++;
                ModLog.Debug(
                    Feature,
                    $"Special group configured — category={SpawnCategoryLookup.Format(category)}, name={entityName}, " +
                    $"master={spawnInfo.MasterID}, {multiplier:0.##}× (max={max}, remain={remain})");
            }

            return scaled;
        }

        private static int ScaleSpawnPointDatas(DungeonRoom room, int playerCount)
        {
            if (SpawnScalingFields.SpawnedActorDatasField.GetValue(room) is not IDictionary datas)
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

                scaled += ScaleSpawnDataObject(entry.Value, playerCount) ? 1 : 0;
            }

            return scaled;
        }

        private static int ConfigureBonusGroupWaves(DungeonRoom room, int playerCount)
        {
            if (SpawnScalingFields.GroupSpawnDatasField.GetValue(room) is not IDictionary datas)
            {
                return 0;
            }

            RoomSpawnScalingState state = RoomSpawnScalingRegistry.GetOrCreate(room);
            int configured = 0;

            foreach (DictionaryEntry entry in datas)
            {
                if (entry.Value is not GroupSpawnData groupData)
                {
                    continue;
                }

                float? groupMultiplier = null;
                string? entityName = null;
                SpawnCategory category = SpawnCategory.Other;
                if (groupData.Members != null)
                {
                    foreach (GroupCreatureData member in groupData.Members)
                    {
                        category = SpawnCategoryLookup.GetCategory(member.MasterID);
                        groupMultiplier = SpawnMultiplierResolver.GetEffectiveMultiplier(member.MasterID, playerCount);
                        entityName = MonsterTypeLookup.GetDisplayName(member.MasterID);
                        break;
                    }
                }

                if (groupMultiplier == null)
                {
                    continue;
                }

                int bonusWaves = Math.Max(0, SpawnMultiplierResolver.ScaleCount(1, groupMultiplier.Value) - 1);
                state.SetBonusGroupWaves(groupData.GroupID, bonusWaves);
                configured++;

                ModLog.Debug(
                    Feature,
                    $"Group spawn configured — category={SpawnCategoryLookup.Format(category)}, name={entityName ?? "unknown"}, " +
                    $"id={groupData.GroupID}, {groupMultiplier.Value:0.##}× (bonusWaves={bonusWaves})");
            }

            if (configured > 0)
            {
                RoomSpawnScalingRegistry.Register(room, state);
            }

            return configured;
        }

        private static bool ScaleSpawnDataObject(object spawnData, int playerCount)
        {
            if (ReflectionFieldCache.GetField(spawnData, "MasterID") == null)
            {
                return false;
            }

            if (spawnData is FixedSpawnedActorData)
            {
                return false;
            }

            int masterId = spawnData is SpawnedActorData actorData ? actorData.MasterID : 0;
            if (masterId == 0 && ReflectionFieldCache.GetField(spawnData, "MasterID")?.GetValue(spawnData) is int reflectedMasterId)
            {
                masterId = reflectedMasterId;
            }

            SpawnCategory category = SpawnCategoryLookup.GetCategory(masterId);
            float multiplier = SpawnMultiplierResolver.GetEffectiveMultiplier(category, playerCount);
            string entityName = MonsterTypeLookup.GetDisplayName(masterId);

            bool scaled = false;

            if (ReflectionFieldCache.GetField(spawnData, "StackCount") is { } stackCountField)
            {
                int stackBefore = (int)(stackCountField.GetValue(spawnData) ?? 0);
                int stackAfter = SpawnMultiplierResolver.ScaleCountWithImplicitBase(stackBefore, multiplier, implicitWhenZero: 1);
                stackCountField.SetValue(spawnData, stackAfter);
                SpawnScalingLog.DebugFieldScaled(
                    $"spawnPoint[{masterId}].stackCount ({entityName})",
                    stackBefore,
                    stackAfter,
                    multiplier);
                scaled = true;
            }

            if (ReflectionFieldCache.GetField(spawnData, "MaxRespawnCount") is { } maxRespawnField)
            {
                int respawnBefore = (int)(maxRespawnField.GetValue(spawnData) ?? 0);
                if (respawnBefore > 0)
                {
                    int respawnAfter = SpawnMultiplierResolver.ScaleCount(respawnBefore, multiplier);
                    maxRespawnField.SetValue(spawnData, respawnAfter);
                    SpawnScalingLog.DebugFieldScaled(
                        $"spawnPoint[{masterId}].maxRespawn ({entityName})",
                        respawnBefore,
                        respawnAfter,
                        multiplier);
                    scaled = true;
                }
            }

            if (scaled)
            {
                ModLog.Debug(
                    Feature,
                    $"Spawn point configured — category={SpawnCategoryLookup.Format(category)}, name={entityName}, " +
                    $"master={masterId}, {multiplier:0.##}×");
            }

            return scaled;
        }

        private static int ScaleField(object target, System.Reflection.FieldInfo field, float multiplier, string label)
        {
            int before = (int)(field.GetValue(target) ?? 0);
            int after = SpawnMultiplierResolver.ScaleCount(before, multiplier);
            field.SetValue(target, after);
            SpawnScalingLog.DebugFieldScaled(label, before, after, multiplier);
            return after;
        }
    }
}
