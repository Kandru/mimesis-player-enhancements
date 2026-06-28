using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Bifrost.ConstEnum;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.SpawnScaling;

internal static class SpawnScalingApplier
{
    private const string Feature = "SpawnScaling";

    private const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly Type SpecialMonsterSpawnGroupType =
        typeof(DungeonRoom).GetNestedType("SpecialMonsterSpawnGroup", InstanceFlags)
        ?? throw new InvalidOperationException("DungeonRoom.SpecialMonsterSpawnGroup not found");

    private static readonly FieldInfo MimicSpawnCountMaxField =
        AccessToolsField(typeof(DungeonRoom), "_mimicSpawnCountMax");

    private static readonly FieldInfo MimicSpawnCountRemainField =
        AccessToolsField(typeof(DungeonRoom), "_mimicSpawnCountRemain");

    private static readonly FieldInfo NormalMonsterThreatLimitField =
        AccessToolsField(typeof(DungeonRoom), "_normalMonsterThreatLimit");

    private static readonly FieldInfo NormalMonsterThreatRemainField =
        AccessToolsField(typeof(DungeonRoom), "_normalMonsterThreatRemain");

    private static readonly FieldInfo NormalMonsterSpawnThreatMinThresholdField =
        AccessToolsField(typeof(DungeonRoom), "_normalMonsterSpawnThreatMinThreshold");

    private static readonly FieldInfo SpecialMonsterSpawnGroupsField =
        AccessToolsField(typeof(DungeonRoom), "_specialMonsterSpawnGroups");

    private static readonly FieldInfo SpawnedActorDatasField =
        AccessToolsField(typeof(DungeonRoom), "_spawnedActorDatas");

    private static readonly FieldInfo GroupSpawnDatasField =
        AccessToolsField(typeof(DungeonRoom), "_groupSpawnDatas");

    private static readonly FieldInfo SpecialGroupInfoField =
        AccessToolsField(SpecialMonsterSpawnGroupType, "Info");

    private static readonly FieldInfo SpecialGroupSpawnCountMaxField =
        AccessToolsField(SpecialMonsterSpawnGroupType, "SpawnCountMax");

    private static readonly FieldInfo SpecialGroupSpawnCountRemainField =
        AccessToolsField(SpecialMonsterSpawnGroupType, "SpawnCountRemain");

    private static readonly FieldInfo SpecialSpawnInfoSpawnCountMinField =
        AccessToolsField(typeof(SpecialMonsterSpawnInfo), "SpawnCountMin");

    private static readonly FieldInfo SpecialSpawnInfoSpawnCountMaxField =
        AccessToolsField(typeof(SpecialMonsterSpawnInfo), "SpawnCountMax");

    private static readonly FieldInfo GroupSpawnCountBackingField =
        AccessToolsField(typeof(GroupSpawnData), "<GroupSpawnCount>k__BackingField");

    private static readonly HashSet<DungeonRoom> AppliedRooms = new();
    private static readonly HashSet<DungeonRoom> SkippedClientRooms = new();

    internal static bool IsApplied(DungeonRoom room) => AppliedRooms.Contains(room);

    internal static void EnsureApplied(DungeonRoom room)
    {
        if (AppliedRooms.Contains(room))
            return;

        if (HostApplyGate.IsParticipantClient())
        {
            if (SkippedClientRooms.Add(room))
                ModLog.Debug(Feature, "Spawn scaling skipped — participant client");
            return;
        }

        if (!HostApplyGate.ShouldApplyHostOnlyFeature())
        {
            ModLog.Debug(Feature, "Spawn scaling deferred — waiting for host session");
            return;
        }

        Apply(room);
        AppliedRooms.Add(room);
    }

    internal static void Apply(DungeonRoom room)
    {
        if (!ModConfig.EnableSpawnScaling.Value)
        {
            ModLog.Debug(Feature, "Spawn scaling skipped — EnableSpawnScaling is off");
            return;
        }

        if (!HostApplyGate.ShouldApplyHostOnlyFeature())
        {
            ModLog.Debug(Feature, "Spawn scaling skipped — not host");
            return;
        }

        int playerCount = room.GetMemberCount();
        SpawnScalingLog.InfoScalingApplied(playerCount);

        float mimicMultiplier = SpawnMultiplierResolver.GetEffectiveMultiplier(SpawnCategory.Mimic, playerCount);
        float jakoMultiplier = SpawnMultiplierResolver.GetEffectiveMultiplier(SpawnCategory.Jako, playerCount);

        int mimicMax = ScaleField(room, MimicSpawnCountMaxField, mimicMultiplier, "mimicSpawnCountMax");
        int mimicRemain = ScaleField(room, MimicSpawnCountRemainField, mimicMultiplier, "mimicSpawnCountRemain");
        int threatLimit = ScaleField(room, NormalMonsterThreatLimitField, jakoMultiplier, "normalMonsterThreatLimit");
        int threatRemain = ScaleField(room, NormalMonsterThreatRemainField, jakoMultiplier, "normalMonsterThreatRemain");
        int threatMin = ScaleField(
            room,
            NormalMonsterSpawnThreatMinThresholdField,
            jakoMultiplier,
            "normalMonsterSpawnThreatMinThreshold");

        int specialGroups = ScaleSpecialGroups(room, playerCount);
        int spawnPoints = ScaleSpawnPointDatas(room, playerCount);
        int groupSpawns = ScaleGroupSpawnDatas(room, playerCount);

        ModLog.Info(
            Feature,
            $"Spawn budgets updated — mimic {mimicMultiplier:0.##}× (max={mimicMax}, remain={mimicRemain}), " +
            $"jako {jakoMultiplier:0.##}× (limit={threatLimit}, remain={threatRemain}, min={threatMin}), " +
            $"specialGroups={specialGroups}, spawnPoints={spawnPoints}, groupSpawns={groupSpawns}");
    }

    private static int ScaleSpecialGroups(DungeonRoom room, int playerCount)
    {
        if (SpecialMonsterSpawnGroupsField.GetValue(room) is not IList groups)
            return 0;

        int scaled = 0;
        foreach (object group in groups)
        {
            if (group == null)
                continue;

            object? info = SpecialGroupInfoField.GetValue(group);
            if (info is not SpecialMonsterSpawnInfo spawnInfo)
                continue;

            float multiplier = SpawnMultiplierResolver.GetEffectiveMultiplier(spawnInfo.MasterID, playerCount);
            SpawnCategory category = SpawnCategoryLookup.GetCategory(spawnInfo.MasterID);
            string entityName = MonsterTypeLookup.GetDisplayName(spawnInfo.MasterID);

            int max = ScaleField(group, SpecialGroupSpawnCountMaxField, multiplier, $"specialGroup[{spawnInfo.MasterID}].spawnCountMax");
            int remain = ScaleField(group, SpecialGroupSpawnCountRemainField, multiplier, $"specialGroup[{spawnInfo.MasterID}].spawnCountRemain");
            ScaleField(spawnInfo, SpecialSpawnInfoSpawnCountMinField, multiplier, $"specialGroup[{spawnInfo.MasterID}].spawnCountMin");
            ScaleField(spawnInfo, SpecialSpawnInfoSpawnCountMaxField, multiplier, $"specialGroup[{spawnInfo.MasterID}].spawnCountMaxInfo");

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
        if (SpawnedActorDatasField.GetValue(room) is not IDictionary datas)
            return 0;

        int scaled = 0;
        foreach (DictionaryEntry entry in datas)
        {
            if (entry.Value == null)
                continue;

            scaled += ScaleSpawnDataObject(entry.Value, playerCount) ? 1 : 0;
        }

        return scaled;
    }

    private static int ScaleGroupSpawnDatas(DungeonRoom room, int playerCount)
    {
        if (GroupSpawnDatasField.GetValue(room) is not IDictionary datas)
            return 0;

        int scaled = 0;
        foreach (DictionaryEntry entry in datas)
        {
            if (entry.Value is not GroupSpawnData groupData)
                continue;

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
                continue;

            int before = (int)(GroupSpawnCountBackingField.GetValue(groupData) ?? 0);
            int after = SpawnMultiplierResolver.ScaleCount(before, groupMultiplier.Value);
            GroupSpawnCountBackingField.SetValue(groupData, after);
            scaled++;

            ModLog.Debug(
                Feature,
                $"Group spawn configured — category={SpawnCategoryLookup.Format(category)}, name={entityName ?? "unknown"}, " +
                $"id={groupData.GroupID}, {groupMultiplier.Value:0.##}× (count {before} -> {after})");
        }

        return scaled;
    }

    private static bool ScaleSpawnDataObject(object spawnData, int playerCount)
    {
        FieldInfo? masterIdField = spawnData.GetType().GetField("MasterID", InstanceFlags);
        if (masterIdField == null)
            return false;

        if (spawnData is FixedSpawnedActorData)
            return false;

        int masterId = (int)(masterIdField.GetValue(spawnData) ?? 0);
        SpawnCategory category = SpawnCategoryLookup.GetCategory(masterId);
        float multiplier = SpawnMultiplierResolver.GetEffectiveMultiplier(category, playerCount);
        string entityName = MonsterTypeLookup.GetDisplayName(masterId);

        bool scaled = false;

        FieldInfo? stackCountField = spawnData.GetType().GetField("StackCount", InstanceFlags);
        if (stackCountField != null)
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

        FieldInfo? maxRespawnField = spawnData.GetType().GetField("MaxRespawnCount", InstanceFlags);
        if (maxRespawnField != null)
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

    private static int ScaleField(object target, FieldInfo field, float multiplier, string label)
    {
        int before = (int)(field.GetValue(target) ?? 0);
        int after = SpawnMultiplierResolver.ScaleCount(before, multiplier);
        field.SetValue(target, after);
        SpawnScalingLog.DebugFieldScaled(label, before, after, multiplier);
        return after;
    }

    private static FieldInfo AccessToolsField(Type type, string name)
    {
        var field = type.GetField(name, InstanceFlags);
        if (field == null)
            throw new InvalidOperationException($"{type.Name}.{name} not found");

        return field;
    }
}
