using System;
using System.Reflection;

namespace MimesisPlayerEnhancement.Features.SpawnScaling
{
    internal static class SpawnScalingFields
    {
        internal const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        internal static readonly Type SpecialMonsterSpawnGroupType =
            typeof(DungeonRoom).GetNestedType("SpecialMonsterSpawnGroup", InstanceFlags)
            ?? throw new InvalidOperationException("DungeonRoom.SpecialMonsterSpawnGroup not found");

        internal static readonly FieldInfo MimicSpawnCountMaxField =
            Field(typeof(DungeonRoom), "_mimicSpawnCountMax");

        internal static readonly FieldInfo MimicSpawnCountRemainField =
            Field(typeof(DungeonRoom), "_mimicSpawnCountRemain");

        internal static readonly FieldInfo NormalMonsterThreatLimitField =
            Field(typeof(DungeonRoom), "_normalMonsterThreatLimit");

        internal static readonly FieldInfo NormalMonsterThreatRemainField =
            Field(typeof(DungeonRoom), "_normalMonsterThreatRemain");

        internal static readonly FieldInfo NormalMonsterSpawnThreatMinThresholdField =
            Field(typeof(DungeonRoom), "_normalMonsterSpawnThreatMinThreshold");

        internal static readonly FieldInfo SpecialMonsterSpawnGroupsField =
            Field(typeof(DungeonRoom), "_specialMonsterSpawnGroups");

        internal static readonly FieldInfo SpawnedActorDatasField =
            Field(typeof(DungeonRoom), "_spawnedActorDatas");

        internal static readonly FieldInfo GroupSpawnDatasField =
            Field(typeof(DungeonRoom), "_groupSpawnDatas");

        internal static readonly FieldInfo DungeonMasterInfoField =
            Field(typeof(DungeonRoom), "_dungeonMasterInfo");

        internal static readonly FieldInfo LastNormalMonsterSpawnTimeField =
            Field(typeof(DungeonRoom), "_lastNormalMonsterSpawnTime");

        internal static readonly FieldInfo LastMimicSpawnTimeField =
            Field(typeof(DungeonRoom), "_lastMimicSpawnTime");

        internal static readonly FieldInfo SpecialGroupInfoField =
            Field(SpecialMonsterSpawnGroupType, "Info");

        internal static readonly FieldInfo SpecialGroupSpawnCountMaxField =
            Field(SpecialMonsterSpawnGroupType, "SpawnCountMax");

        internal static readonly FieldInfo SpecialGroupSpawnCountRemainField =
            Field(SpecialMonsterSpawnGroupType, "SpawnCountRemain");

        internal static readonly FieldInfo SpecialSpawnInfoSpawnCountMinField =
            Field(typeof(SpecialMonsterSpawnInfo), "SpawnCountMin");

        internal static readonly FieldInfo SpecialSpawnInfoSpawnCountMaxField =
            Field(typeof(SpecialMonsterSpawnInfo), "SpawnCountMax");

        internal static readonly FieldInfo GroupSpawnCountBackingField =
            Field(typeof(GroupSpawnData), "<GroupSpawnCount>k__BackingField");

        internal static readonly FieldInfo GroupDeathTimeBackingField =
            Field(typeof(GroupSpawnData), "<GroupDeathTime>k__BackingField");

        internal static readonly FieldInfo LastGroupSpawnTimeBackingField =
            Field(typeof(GroupSpawnData), "<LastGroupSpawnTime>k__BackingField");

        internal static readonly FieldInfo CurrentSpawnCountBackingField =
            Field(typeof(SpawnedActorData), "<CurrentSpawnCount>k__BackingField");

        internal static readonly FieldInfo SpawnDataIndexField =
            Field(typeof(SpawnedActorData), "Index");

        internal static FieldInfo Field(Type type, string name)
        {
            FieldInfo? field = type.GetField(name, InstanceFlags) ?? AccessTools.Field(type, name);
            return field ?? throw new InvalidOperationException($"{type.Name}.{name} not found");
        }
    }
}
