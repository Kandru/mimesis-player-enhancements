using System.Reflection;
using MimesisPlayerEnhancement.Tests.Infrastructure;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.LootMultiplicator
{
    public sealed class LootMultiplicatorPatchContractTests
    {
        private const BindingFlags InstanceMember =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [Fact]
        public void DungeonRoom_InitSpawn_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type dungeonRoom = context.RequireType("DungeonRoom");

            MethodInfo? method = dungeonRoom.GetMethod("InitSpawn", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
            Assert.Empty(method.GetParameters());
        }

        [Fact]
        public void DungeonRoom_ManageSpawnData_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type dungeonRoom = context.RequireType("DungeonRoom");

            MethodInfo? method = dungeonRoom.GetMethod("ManageSpawnData", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Fact]
        public void ItemDropInfo_GetDropItemList_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("ItemDropInfo");

            MethodInfo? method = type.GetMethod("GetDropItemList", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
        }

        [Fact]
        public void IVroom_ExecuteLootingObjectSpawn_exists_with_SpawnedActorData_param()
        {
            using MimesisMetadataContext context = CreateContext();
            Type ivroom = context.RequireType("IVroom");
            Type spawnedActorData = context.RequireType("SpawnedActorData");

            MethodInfo? method = ivroom.GetMethods(InstanceMember)
                .FirstOrDefault(candidate =>
                    candidate.Name == "ExecuteLootingObjectSpawn"
                    && candidate.GetParameters().Length == 1
                    && candidate.GetParameters()[0].ParameterType.Name == spawnedActorData.Name);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Fact]
        public void IVroom_SpawnLootingObject_exists_with_nine_param_overload()
        {
            using MimesisMetadataContext context = CreateContext();
            Type ivroom = context.RequireType("IVroom");

            string[] expectedParamTypeNames =
            [
                "ItemElement",
                "PosWithRot",
                "Boolean",
                "ReasonOfSpawn",
                "Int32",
                "Int32",
                "Int64",
                "Boolean",
                "Boolean",
            ];

            MethodInfo? method = ivroom.GetMethods(InstanceMember)
                .FirstOrDefault(candidate =>
                {
                    if (!string.Equals(candidate.Name, "SpawnLootingObject", StringComparison.Ordinal))
                    {
                        return false;
                    }

                    ParameterInfo[] parameters = candidate.GetParameters();
                    if (parameters.Length != expectedParamTypeNames.Length)
                    {
                        return false;
                    }

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if (parameters[i].ParameterType.Name != expectedParamTypeNames[i])
                        {
                            return false;
                        }
                    }

                    return true;
                });

            Assert.NotNull(method);
        }

        [Fact]
        public void SpawnedActorData_OnActorDead_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SpawnedActorData");

            MethodInfo? method = type.GetMethod("OnActorDead", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
        }

        [Fact]
        public void InventoryController_BarterItem_exists_with_PosWithRot_param()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("InventoryController");

            MethodInfo? method = type.GetMethods(InstanceMember)
                .FirstOrDefault(candidate =>
                    candidate.Name == "BarterItem"
                    && candidate.GetParameters().Length == 1
                    && candidate.GetParameters()[0].ParameterType.Name == "PosWithRot");

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
        }

        [Fact]
        public void DeathMatchRoom_ExtractRoomInfo_exists_with_bool_param()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("DeathMatchRoom");

            MethodInfo? method = type.GetMethods(InstanceMember)
                .FirstOrDefault(candidate =>
                    candidate.Name == "ExtractRoomInfo"
                    && candidate.GetParameters().Length == 1
                    && candidate.GetParameters()[0].ParameterType.Name == "Boolean");

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
        }

        [Fact]
        public void SimpleRandUtil_Next_int_int_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SimpleRandUtil");

            MethodInfo? method = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(candidate =>
                    candidate.Name == "Next"
                    && candidate.GetParameters().Length == 2
                    && candidate.GetParameters()[0].ParameterType.Name == "Int32"
                    && candidate.GetParameters()[1].ParameterType.Name == "Int32");

            Assert.NotNull(method);
            Assert.Equal("Int32", method.ReturnType.Name);
        }

        [Theory]
        [InlineData("MiscMinVal")]
        [InlineData("MiscMaxVal")]
        public void SpawnableItemInfo_misc_budget_fields_exist(string fieldName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type spawnableItemInfo = RequireSpawnableItemInfoType(context);

            FieldInfo? field = spawnableItemInfo.GetField(fieldName, InstanceMember);

            Assert.NotNull(field);
            Assert.Equal("Int32", field.FieldType.Name);
        }

        [Fact]
        public void ManageSpawnData_misc_budget_transpiler_targets_exist()
        {
            using MimesisMetadataContext context = CreateContext();
            Type dungeonRoom = context.RequireType("DungeonRoom");
            Type spawnableItemInfo = RequireSpawnableItemInfoType(context);
            Type simpleRandUtil = context.RequireType("SimpleRandUtil");

            MethodInfo? manageSpawnData = dungeonRoom.GetMethod("ManageSpawnData", InstanceMember);
            MethodInfo? simpleRandNext = simpleRandUtil.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(candidate =>
                    candidate.Name == "Next"
                    && candidate.GetParameters().Length == 2);
            FieldInfo? miscMaxValField = spawnableItemInfo.GetField("MiscMaxVal", InstanceMember);
            FieldInfo? miscMinValField = spawnableItemInfo.GetField("MiscMinVal", InstanceMember);
            FieldInfo? dungeonMasterInfoField = dungeonRoom.GetField("_dungeonMasterInfo", InstanceMember);

            Assert.NotNull(manageSpawnData);
            Assert.NotNull(simpleRandNext);
            Assert.NotNull(miscMaxValField);
            Assert.NotNull(miscMinValField);
            Assert.NotNull(dungeonMasterInfoField);
            Assert.Equal("DungeonMasterInfo", dungeonMasterInfoField.FieldType.Name);
        }

        [Fact]
        public void DungeonRoom_spawnedActorDatas_field_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type dungeonRoom = context.RequireType("DungeonRoom");

            FieldInfo? field = dungeonRoom.GetField("_spawnedActorDatas", InstanceMember);

            Assert.NotNull(field);
        }

        [Fact]
        public void DungeonRoom_dungeonMasterInfo_field_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type dungeonRoom = context.RequireType("DungeonRoom");

            FieldInfo? field = dungeonRoom.GetField("_dungeonMasterInfo", InstanceMember);

            Assert.NotNull(field);
            Assert.Equal("DungeonMasterInfo", field.FieldType.Name);
        }

        [Fact]
        public void ConsumableItemElement_RemainCount_has_setter()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("ConsumableItemElement");

            PropertyInfo? property = type.GetProperty("RemainCount", InstanceMember);

            Assert.NotNull(property);
            Assert.NotNull(property.GetSetMethod(nonPublic: true));
            Assert.Equal("Int32", property.PropertyType.Name);
        }

        [Fact]
        public void RandomSpawnedItemActorData_Candidates_property_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("RandomSpawnedItemActorData");

            PropertyInfo? property = type.GetProperty("Candidates", InstanceMember);

            Assert.NotNull(property);
            Assert.Equal("ImmutableDictionary`2", property.PropertyType.Name);
        }

        [Fact]
        public void RandomSpawnedItemActorData_maxRate_field_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("RandomSpawnedItemActorData");

            FieldInfo? field = type.GetField("_maxRate", InstanceMember);

            Assert.NotNull(field);
            Assert.Equal("Int32", field.FieldType.Name);
        }

        private static Type RequireSpawnableItemInfoType(MimesisMetadataContext context)
        {
            foreach (Type type in context.AssemblyCSharp.GetTypes())
            {
                FieldInfo? miscMaxVal = type.GetField("MiscMaxVal", InstanceMember);
                FieldInfo? miscMinVal = type.GetField("MiscMinVal", InstanceMember);
                if (miscMaxVal != null && miscMinVal != null)
                {
                    return type;
                }
            }

            throw new InvalidOperationException("Type with MiscMinVal and MiscMaxVal fields not found.");
        }

        private static MimesisMetadataContext CreateContext()
        {
            string managedPath = ManagedAssemblyPaths.Resolve();
            return new MimesisMetadataContext(managedPath);
        }
    }
}
