using System.Reflection;
using MimesisPlayerEnhancement.Tests.Infrastructure;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.DungeonRandomizer
{
    public sealed class DungeonRandomizerPatchContractTests
    {
        private const BindingFlags InstanceMember =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [Fact]
        public void ExcelDataManager_PickDungeon_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("ExcelDataManager");

            MethodInfo? method = type.GetMethod("PickDungeon", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Int32", method.ReturnType.Name);
            ParameterInfo[] parameters = method.GetParameters();
            Assert.Equal(3, parameters.Length);
            Assert.Equal("Int32", parameters[0].ParameterType.Name);
            Assert.Equal("Int32", parameters[1].ParameterType.Name);
            Assert.Equal("List`1", parameters[2].ParameterType.Name);
        }

        [Fact]
        public void ExcelDataManager_DungeonInfoDict_property_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("ExcelDataManager");

            PropertyInfo? property = type.GetProperty("DungeonInfoDict", InstanceMember);

            Assert.NotNull(property);
            Assert.Equal("ImmutableDictionary`2", property.PropertyType.Name);
        }

        [Fact]
        public void VWaitingRoom_RollDiceDungeon_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VWaitingRoom");

            MethodInfo? method = type.GetMethod("RollDiceDungeon", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Boolean", method.ReturnType.Name);
            Assert.Single(method.GetParameters());
            Assert.Equal("Boolean", method.GetParameters()[0].ParameterType.Name);
        }

        [Fact]
        public void DungeonMasterInfo_PickMapID_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("DungeonMasterInfo");

            MethodInfo? method = type.GetMethod("PickMapID", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Int32", method.ReturnType.Name);
            Assert.Empty(method.GetParameters());
        }

        [Theory]
        [InlineData("MapIDs")]
        [InlineData("IsActive")]
        [InlineData("DungenCandidates")]
        [InlineData("MaxDungenRate")]
        public void DungeonMasterInfo_variant_fields_exist(string fieldName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("DungeonMasterInfo");

            FieldInfo? field = type.GetField(fieldName, InstanceMember);

            Assert.NotNull(field);
        }

        [Fact]
        public void DungeonMasterInfo_GetRandomDungenName_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("DungeonMasterInfo");

            MethodInfo? method = type.GetMethod("GetRandomDungenName", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("String", method.ReturnType.Name);
            Assert.Single(method.GetParameters());
            Assert.Equal("Int32", method.GetParameters()[0].ParameterType.Name);
        }

        [Fact]
        public void IVroom_SendToAllPlayers_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("IVroom");

            MethodInfo? method = type.GetMethods(InstanceMember)
                .FirstOrDefault(candidate =>
                    candidate.Name == "SendToAllPlayers"
                    && candidate.GetParameters().Length == 2
                    && candidate.GetParameters()[0].ParameterType.Name == "IMsg"
                    && candidate.GetParameters()[1].ParameterType.Name == "VActor");

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Fact]
        public void MoveToDungeonSig_seed_fields_exist()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("MoveToDungeonSig");

            PropertyInfo? seedProperty = type.GetProperty("randDungeonSeed", InstanceMember);
            PropertyInfo? dungeonProperty = type.GetProperty("selectedDungeonMasterID", InstanceMember);

            Assert.NotNull(seedProperty);
            Assert.NotNull(dungeonProperty);
            Assert.Equal("Int32", seedProperty.PropertyType.Name);
            Assert.Equal("Int32", dungeonProperty.PropertyType.Name);
        }

        [Fact]
        public void VRoomManager_PendMoveToDungeon_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VRoomManager");

            MethodInfo? method = type.GetMethods(InstanceMember)
                .FirstOrDefault(candidate =>
                    candidate.Name == "PendMoveToDungeon"
                    && candidate.GetParameters().Length == 4);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
            ParameterInfo[] parameters = method.GetParameters();
            Assert.Equal("Dictionary`2", parameters[0].ParameterType.Name);
            Assert.Equal("Int32", parameters[1].ParameterType.Name);
            Assert.Equal("Int32", parameters[2].ParameterType.Name);
            Assert.Equal("RoomDrainInfo", parameters[3].ParameterType.Name);
        }

        [Fact]
        public void VWorld_ReadyToGamePktRecording_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VWorld");

            MethodInfo? method = type.GetMethods(InstanceMember)
                .FirstOrDefault(candidate =>
                    candidate.Name == "ReadyToGamePktRecording"
                    && candidate.GetParameters().Length == 3);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
            ParameterInfo[] parameters = method.GetParameters();
            Assert.Equal("Int32", parameters[0].ParameterType.Name);
            Assert.Equal("Int32", parameters[1].ParameterType.Name);
            Assert.Equal("Int32", parameters[2].ParameterType.Name);
        }

        [Fact]
        public void RuntimeDungeon_Generate_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("RuntimeDungeon");

            MethodInfo? method = type.GetMethod("Generate", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
            Assert.Empty(method.GetParameters());
        }

        private static MimesisMetadataContext CreateContext()
        {
            string managedPath = ManagedAssemblyPaths.Resolve();
            return new MimesisMetadataContext(managedPath);
        }
    }
}
