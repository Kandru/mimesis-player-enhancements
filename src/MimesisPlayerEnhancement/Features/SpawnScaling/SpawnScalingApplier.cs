using System.Collections;

namespace MimesisPlayerEnhancement.Features.SpawnScaling
{
    internal static class SpawnScalingApplier
    {
        private const string Feature = "SpawnScaling";

        internal static bool IsApplied(DungeonRoom room)
        {
            return DungeonRoomAppliedSet.IsApplied(room, DungeonRoomApplyKind.SpawnScaling);
        }

        internal static void EnsureApplied(DungeonRoom room)
        {
            if (DungeonRoomAppliedSet.IsApplied(room, DungeonRoomApplyKind.SpawnScaling))
            {
                return;
            }

            if (HostApplyGate.IsParticipantClient())
            {
                if (DungeonRoomAppliedSet.MarkSkippedOnce(room, DungeonRoomApplyKind.SpawnScalingSkippedOnce))
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
            DungeonRoomAppliedSet.MarkApplied(room, DungeonRoomApplyKind.SpawnScaling);
        }

        internal static void Apply(DungeonRoom room)
        {
            SpawnScalingSceneConfig config = SceneScopedConfigGate.Spawn;
            if (!config.EnableSpawnScaling)
            {
                ModLog.Debug(Feature, "Spawn scaling skipped — EnableSpawnScaling is off");
                return;
            }

            int playerCount = room.GetMemberCount();
            SpawnScalingLog.InfoScalingApplied(playerCount);

            float mimicMultiplier = SpawnMultiplierResolver.GetEffectiveMultiplier(SpawnCategory.Mimic, playerCount, config);
            float jakoMultiplier = SpawnMultiplierResolver.GetEffectiveMultiplier(SpawnCategory.Jako, playerCount, config);

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
            state.SetSnapshot(config);
            if (SpawnScalingFields.DungeonMasterInfoField.GetValue(room) is DungeonMasterInfo dungeonInfo)
            {
                PeriodicSpawnWaitApplier.ApplyInitialWait(room, state);
                SpawnTimingOverrideApplier.ConfigureTimingOverrides(room, state, dungeonInfo, jakoMultiplier, mimicMultiplier);
            }

            ModLog.Info(Feature, $"Spawn budgets updated — mimic {mimicMultiplier:0.##}× (max={mimicMax}, remain={mimicRemain}), " +
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
                if (multiplier <= FeatureToggleGate.NeutralMultiplier)
                {
                    continue;
                }

                SpawnCategory category = SpawnCategoryLookup.GetCategory(spawnInfo.MasterID);
                string entityName = MonsterTypeLookup.GetDisplayName(spawnInfo.MasterID);

                int max = ScaleField(group, SpawnScalingFields.SpecialGroupSpawnCountMaxField, multiplier, $"specialGroup[{spawnInfo.MasterID}].spawnCountMax");
                int remain = ScaleField(group, SpawnScalingFields.SpecialGroupSpawnCountRemainField, multiplier, $"specialGroup[{spawnInfo.MasterID}].spawnCountRemain");
                _ = ScaleField(spawnInfo, SpawnScalingFields.SpecialSpawnInfoSpawnCountMinField, multiplier, $"specialGroup[{spawnInfo.MasterID}].spawnCountMin");
                _ = ScaleField(spawnInfo, SpawnScalingFields.SpecialSpawnInfoSpawnCountMaxField, multiplier, $"specialGroup[{spawnInfo.MasterID}].spawnCountMaxInfo");

                scaled++;
                ModLog.Debug(Feature, $"Special group configured — category={SpawnCategoryLookup.Format(category)}, name={entityName}, " +
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

                if (groupMultiplier == null || groupMultiplier.Value <= FeatureToggleGate.NeutralMultiplier)
                {
                    continue;
                }

                int bonusWaves = Math.Max(0, ScalingMath.ScaleCount(1, groupMultiplier.Value) - 1);
                state.SetBonusGroupWaves(groupData.GroupID, bonusWaves);
                configured++;

                ModLog.Debug(Feature, $"Group spawn configured — category={SpawnCategoryLookup.Format(category)}, name={entityName ?? "unknown"}, " +
                    $"id={groupData.GroupID}, {groupMultiplier.Value:0.##}× (bonusWaves={bonusWaves})");
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
            if (multiplier <= FeatureToggleGate.NeutralMultiplier)
            {
                return false;
            }

            string entityName = MonsterTypeLookup.GetDisplayName(masterId);

            bool scaled = false;

            if (SpawnDataFieldScaler.TryScaleStackCount(spawnData, multiplier, out int stackBefore, out int stackAfter))
            {
                SpawnScalingLog.DebugFieldScaled(
                    $"spawnPoint[{masterId}].stackCount ({entityName})",
                    stackBefore,
                    stackAfter,
                    multiplier);
                scaled = true;
            }

            if (SpawnDataFieldScaler.TryScaleMaxRespawnCount(spawnData, multiplier, out int respawnBefore, out int respawnAfter))
            {
                SpawnScalingLog.DebugFieldScaled(
                    $"spawnPoint[{masterId}].maxRespawn ({entityName})",
                    respawnBefore,
                    respawnAfter,
                    multiplier);
                scaled = true;
            }

            if (scaled)
            {
                ModLog.Debug(Feature, $"Spawn point configured — category={SpawnCategoryLookup.Format(category)}, name={entityName}, " +
                    $"master={masterId}, {multiplier:0.##}×");
            }

            return scaled;
        }

        private static int ScaleField(object target, System.Reflection.FieldInfo field, float multiplier, string label)
        {
            int before = (int)(field.GetValue(target) ?? 0);
            int after = ScalingMath.ScaleCount(before, multiplier);
            field.SetValue(target, after);
            SpawnScalingLog.DebugFieldScaled(label, before, after, multiplier);
            return after;
        }
    }
}
