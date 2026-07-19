using System.Reflection;
using MimesisPlayerEnhancement.Tests.Infrastructure;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.SpawnScaling
{
    public sealed class SpawnScalingPatchContractTests
    {
        private const BindingFlags InstanceMember =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [Fact]
        public void DungeonRoom_InitSpawn_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("DungeonRoom");

            MethodInfo? method = type.GetMethod("InitSpawn", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
            Assert.Empty(method.GetParameters());
        }

        [Fact]
        public void DungeonRoom_ManageSpawnData_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("DungeonRoom");

            MethodInfo? method = type.GetMethod("ManageSpawnData", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
            Assert.Empty(method.GetParameters());
        }

        [Fact]
        public void SpawnedActorData_OnActorDead_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SpawnedActorData");

            MethodInfo? method = type.GetMethod("OnActorDead", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
            Assert.Empty(method.GetParameters());
        }

        [Fact]
        public void GroupSpawnData_OnMemberDead_exists_with_actor_id()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("GroupSpawnData");

            MethodInfo? method = type.GetMethods(InstanceMember)
                .FirstOrDefault(candidate =>
                    candidate.Name == "OnMemberDead"
                    && candidate.GetParameters().Length == 1
                    && candidate.GetParameters()[0].ParameterType.Name == "Int32");

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Boolean", method.ReturnType.Name);
            Assert.Single(method.GetParameters());
            Assert.Equal("Int32", method.GetParameters()[0].ParameterType.Name);
        }

        [Fact]
        public void IVroom_SpawnMonster_exists_with_expected_signature()
        {
            using MimesisMetadataContext context = CreateContext();
            Type ivroom = context.RequireType("IVroom");
            Type spawnedActorData = context.RequireType("SpawnedActorData");
            Type reasonOfSpawn = context.RequireType("ReasonOfSpawn");

            MethodInfo? method = ivroom.GetMethods(InstanceMember)
                .FirstOrDefault(candidate =>
                    candidate.Name == "SpawnMonster"
                    && candidate.GetParameters().Length == 6
                    && candidate.GetParameters()[0].ParameterType.Name == "Int32"
                    && candidate.GetParameters()[1].ParameterType.Name == spawnedActorData.Name
                    && candidate.GetParameters()[2].ParameterType.Name == "Boolean"
                    && candidate.GetParameters()[3].ParameterType.Name == "String"
                    && candidate.GetParameters()[4].ParameterType.Name == "String"
                    && candidate.GetParameters()[5].ParameterType.Name == reasonOfSpawn.Name);

            Assert.NotNull(method);
            Assert.Equal("Boolean", method.ReturnType.Name);
        }

        [Fact]
        public void DungeonRoom_SpecialMonsterSpawnGroup_nested_type_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type dungeonRoom = context.RequireType("DungeonRoom");

            Type? nested = dungeonRoom.GetNestedType("SpecialMonsterSpawnGroup", InstanceMember);

            Assert.NotNull(nested);
        }

        [Theory]
        [InlineData("_mimicSpawnCountMax", "Int32")]
        [InlineData("_mimicSpawnCountRemain", "Int32")]
        [InlineData("_normalMonsterThreatLimit", "Int32")]
        [InlineData("_normalMonsterThreatRemain", "Int32")]
        [InlineData("_normalMonsterSpawnThreatMinThreshold", "Int32")]
        [InlineData("_specialMonsterSpawnGroups", "List`1")]
        [InlineData("_spawnedActorDatas", "Dictionary`2")]
        [InlineData("_groupSpawnDatas", "Dictionary`2")]
        [InlineData("_dungeonMasterInfo", "DungeonMasterInfo")]
        [InlineData("_lastNormalMonsterSpawnTime", "Int64")]
        [InlineData("_lastMimicSpawnTime", "Int64")]
        public void DungeonRoom_spawn_scaling_fields_exist(string fieldName, string expectedTypeName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("DungeonRoom");

            FieldInfo? field = type.GetField(fieldName, InstanceMember);

            Assert.NotNull(field);
            Assert.Equal(expectedTypeName, field.FieldType.Name);
        }

        [Theory]
        [InlineData("Info", "SpecialMonsterSpawnInfo")]
        [InlineData("SpawnCountMax", "Int32")]
        [InlineData("SpawnCountRemain", "Int32")]
        public void DungeonRoom_SpecialMonsterSpawnGroup_fields_exist(string fieldName, string expectedTypeName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type dungeonRoom = context.RequireType("DungeonRoom");
            Type nested = dungeonRoom.GetNestedType("SpecialMonsterSpawnGroup", InstanceMember)!;

            FieldInfo? field = nested.GetField(fieldName, InstanceMember);

            Assert.NotNull(field);
            Assert.Equal(expectedTypeName, field.FieldType.Name);
        }

        [Theory]
        [InlineData("SpawnCountMin", "Int32")]
        [InlineData("SpawnCountMax", "Int32")]
        public void SpecialMonsterSpawnInfo_spawn_count_fields_exist(string fieldName, string expectedTypeName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SpecialMonsterSpawnInfo");

            FieldInfo? field = type.GetField(fieldName, InstanceMember);

            Assert.NotNull(field);
            Assert.Equal(expectedTypeName, field.FieldType.Name);
        }

        [Theory]
        [InlineData("<GroupSpawnCount>k__BackingField", "Int32")]
        [InlineData("<GroupDeathTime>k__BackingField", "Int64")]
        [InlineData("<LastGroupSpawnTime>k__BackingField", "Int64")]
        public void GroupSpawnData_backing_fields_exist(string fieldName, string expectedTypeName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("GroupSpawnData");

            FieldInfo? field = type.GetField(fieldName, InstanceMember);

            Assert.NotNull(field);
            Assert.Equal(expectedTypeName, field.FieldType.Name);
        }

        [Fact]
        public void SpawnedActorData_CurrentSpawnCount_backing_field_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SpawnedActorData");

            FieldInfo? field = type.GetField("<CurrentSpawnCount>k__BackingField", InstanceMember);

            Assert.NotNull(field);
            Assert.Equal("Int32", field.FieldType.Name);
        }

        [Theory]
        [InlineData("_dungeonSpaceGroup")]
        [InlineData("_spaceGroup")]
        public void DungeonRoom_tile_group_fields_exist(string fieldName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("DungeonRoom");

            FieldInfo? field = type.GetField(fieldName, InstanceMember);

            Assert.NotNull(field);
            Assert.Equal("ISpaceGroup", field.FieldType.Name);
        }

        [Fact]
        public void VSpaceTileGroup_m_tiles_field_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VSpaceTileGroup");

            FieldInfo? field = type.GetField("m_tiles", InstanceMember);

            Assert.NotNull(field);
            Assert.Equal("Dictionary`2", field.FieldType.Name);
        }

        [Fact]
        public void Hub_dynamicDataMan_property_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("Hub");

            PropertyInfo? property = type.GetProperty("dynamicDataMan", InstanceMember);

            Assert.NotNull(property);
            Assert.Equal("DynamicDataManager", property.PropertyType.Name);
        }

        [Fact]
        public void DynamicDataManager_GetAllMonsterSpawnPoints_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("DynamicDataManager");

            MethodInfo? method = type.GetMethod("GetAllMonsterSpawnPoints", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Dictionary`2", method.ReturnType.Name);
            Assert.Empty(method.GetParameters());
        }

        [Fact]
        public void DynamicDataManager_GetAllSpecialMonsterSpawnPoints_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("DynamicDataManager");

            MethodInfo? method = type.GetMethod("GetAllSpecialMonsterSpawnPoints", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Dictionary`2", method.ReturnType.Name);
            Assert.Empty(method.GetParameters());
        }

        [Theory]
        [InlineData("PosVector", "Vector3")]
        [InlineData("Pos", "PosWithRot")]
        public void SpawnedActorData_position_fields_exist(string fieldName, string expectedTypeName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SpawnedActorData");

            FieldInfo? field = type.GetField(fieldName, InstanceMember);

            Assert.NotNull(field);
            Assert.Equal(expectedTypeName, field.FieldType.Name);
        }

        [Theory]
        [InlineData("NormalMonsterSpawnTryCount", "Int32")]
        [InlineData("NormalMonsterSpawnRate", "Int32")]
        [InlineData("NormalMonsterSpawnPeriod", "Int32")]
        [InlineData("MimicSpawnTryCount", "Int32")]
        [InlineData("MimicSpawnRate", "Int32")]
        [InlineData("MimicSpawnPeriod", "Int32")]
        public void DungeonMasterInfo_spawn_timing_fields_exist(string fieldName, string expectedTypeName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("DungeonMasterInfo");

            FieldInfo? field = type.GetField(fieldName, InstanceMember);

            Assert.NotNull(field);
            Assert.Equal(expectedTypeName, field.FieldType.Name);
        }

        private static MimesisMetadataContext CreateContext()
        {
            string managedPath = ManagedAssemblyPaths.Resolve();
            return new MimesisMetadataContext(managedPath);
        }
    }
}
