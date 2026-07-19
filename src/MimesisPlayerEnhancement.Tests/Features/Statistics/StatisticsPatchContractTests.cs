using System.Reflection;
using MimesisPlayerEnhancement.Tests.Infrastructure;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Statistics
{
    public sealed class StatisticsPatchContractTests
    {
        private const BindingFlags InstanceMember =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [Fact]
        public void PlayReportManager_FlushCurrentToAccumulated_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("PlayReportManager");

            MethodInfo? method = type.GetMethod("FlushCurrentToAccumulated", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
            Assert.Empty(method.GetParameters());
        }

        [Fact]
        public void PlayReportManager_CurrentReportDict_property_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("PlayReportManager");

            PropertyInfo? property = type.GetProperty("CurrentReportDict", InstanceMember);

            Assert.NotNull(property);
            Assert.Equal("Dictionary`2", property.PropertyType.Name);
        }

        [Fact]
        public void PlayReportManager_SetDeathMatchSurvivor_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("PlayReportManager");

            MethodInfo? method = type.GetMethod("SetDeathMatchSurvivor", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
            Assert.Single(method.GetParameters());
            Assert.Equal("UInt64", method.GetParameters()[0].ParameterType.Name);
        }

        [Fact]
        public void GameSessionInfo_IncreaseStageCount_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("GameSessionInfo");

            MethodInfo? method = type.GetMethod("IncreaseStageCount", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
            Assert.Single(method.GetParameters());
            Assert.Equal("Boolean", method.GetParameters()[0].ParameterType.Name);
        }

        [Fact]
        public void GameSessionInfo_StageCount_property_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("GameSessionInfo");

            PropertyInfo? property = type.GetProperty("StageCount", InstanceMember);

            Assert.NotNull(property);
            Assert.Equal("Int32", property.PropertyType.Name);
        }

        [Fact]
        public void GameSessionInfo_Reset_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("GameSessionInfo");

            MethodInfo? method = type.GetMethod("Reset", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Fact]
        public void VRoomManager_TerminateSession_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VRoomManager");

            MethodInfo? method = type.GetMethod("TerminateSession", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Fact]
        public void VRoomManager_OnRegistPlayer_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VRoomManager");

            MethodInfo? method = type.GetMethods(InstanceMember)
                .FirstOrDefault(candidate => candidate.Name == "OnRegistPlayer");

            Assert.NotNull(method);
            Assert.Equal("MsgErrorCode", method.ReturnType.Name);
            Assert.Contains(method.GetParameters(), p => p.ParameterType.Name == "UInt64");
        }

        [Fact]
        public void VWorld_OnUnregistPlayer_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VWorld");

            MethodInfo? method = type.GetMethod("OnUnregistPlayer", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
            Assert.Single(method.GetParameters());
            Assert.Equal("UInt64", method.GetParameters()[0].ParameterType.Name);
        }

        [Fact]
        public void VPlayer_HandlePutIntoToilet_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VPlayer");

            MethodInfo? method = type.GetMethod("HandlePutIntoToilet", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("MsgErrorCode", method.ReturnType.Name);
        }

        [Fact]
        public void VPlayer_Revive_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VPlayer");

            MethodInfo? method = type.GetMethods(InstanceMember)
                .FirstOrDefault(candidate =>
                    candidate.Name == "Revive"
                    && candidate.ReturnType.Name == "Boolean");

            Assert.NotNull(method);
            Assert.Equal("Boolean", method.ReturnType.Name);
        }

        [Fact]
        public void InventoryController_OnAddItemByLooting_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("InventoryController");

            MethodInfo? method = type.GetMethods(InstanceMember)
                .FirstOrDefault(candidate =>
                    candidate.Name == "OnAddItemByLooting"
                    && candidate.GetParameters().Length == 1
                    && candidate.GetParameters()[0].ParameterType.Name == "ItemElement");

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Fact]
        public void DungeonRoom_SetDungeonState_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("DungeonRoom");

            MethodInfo? method = type.GetMethod("SetDungeonState", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
            Assert.Single(method.GetParameters());
            Assert.Equal("DungeonState", method.GetParameters()[0].ParameterType.Name);
        }

        [Theory]
        [InlineData("DungeonRoom")]
        [InlineData("DeathMatchRoom")]
        [InlineData("MaintenanceRoom")]
        public void Room_OnActorEvent_exists(string typeName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType(typeName);

            MethodInfo? method = type.GetMethod("OnActorEvent", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
            Assert.Single(method.GetParameters());
            Assert.Equal("VActorEventArgs", method.GetParameters()[0].ParameterType.Name);
        }

        [Fact]
        public void VCreature_OnDying_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VCreature");

            MethodInfo? method = type.GetMethod("OnDying", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
            Assert.Single(method.GetParameters());
            Assert.Equal("ActorDyingSig", method.GetParameters()[0].ParameterType.Name);
        }

        [Fact]
        public void SpeechEventArchive_OnStartClient_exists_when_type_present()
        {
            using MimesisMetadataContext context = CreateContext();
            Type? type = context.FindType("SpeechEventArchive");
            if (type == null)
            {
                // Bootstrap reference libs may omit voice types; verified on full game install.
                return;
            }

            MethodInfo? method = type.GetMethod("OnStartClient", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
            Assert.Empty(method.GetParameters());
        }

        [Fact]
        public void UIPrefab_PlayerEnterInfo_AddPlayerInfo_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefab_PlayerEnterInfo");

            MethodInfo? method = type.GetMethod("AddPlayerInfo", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
            ParameterInfo[] parameters = method.GetParameters();
            Assert.Equal(2, parameters.Length);
            Assert.Equal("String", parameters[0].ParameterType.Name);
            Assert.Equal("Boolean", parameters[1].ParameterType.Name);
        }

        [Fact]
        public void UIPrefab_PlayerEnterInfo_UpdatePlayerInfos_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefab_PlayerEnterInfo");

            MethodInfo? method = type.GetMethod("UpdatePlayerInfos", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("IEnumerator", method.ReturnType.Name);
            Assert.Empty(method.GetParameters());
        }

        [Theory]
        [InlineData("_vPlayerDict")]
        [InlineData("_levelObjects")]
        public void IVroom_statistics_reflection_fields_exist(string fieldName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("IVroom");

            FieldInfo? field = type.GetField(fieldName, InstanceMember);

            Assert.NotNull(field);
        }

        [Fact]
        public void InventoryController_self_field_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("InventoryController");

            FieldInfo? field = type.GetField("_self", InstanceMember);

            Assert.NotNull(field);
            Assert.Equal("VCreature", field.FieldType.Name);
        }

        private static MimesisMetadataContext CreateContext()
        {
            string managedPath = ManagedAssemblyPaths.Resolve();
            return new MimesisMetadataContext(managedPath);
        }
    }
}
