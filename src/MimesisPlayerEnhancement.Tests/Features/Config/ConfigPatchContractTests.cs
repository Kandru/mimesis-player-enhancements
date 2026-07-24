using System.Reflection;
using MimesisPlayerEnhancement.Tests.Infrastructure;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Config
{
    public sealed class ConfigPatchContractTests
    {
        private const BindingFlags InstanceMember =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [Theory]
        [InlineData("EnterWaitingRoom")]
        [InlineData("EnterMaintenenceRoom")]
        [InlineData("EnterDungeon")]
        [InlineData("EnterDeathMatchRoom")]
        [InlineData("OnDungeonFinished")]
        public void VRoomManager_scene_transition_methods_exist(string methodName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VRoomManager");

            MethodInfo? method = type.GetMethod(methodName, InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Fact]
        public void DungeonRoom_SetDungeonState_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type dungeonRoom = context.RequireType("DungeonRoom");

            MethodInfo? method = dungeonRoom.GetMethod("SetDungeonState", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
            Assert.Single(method.GetParameters());
            Assert.Equal("DungeonState", method.GetParameters()[0].ParameterType.Name);
        }

        [Fact]
        public void GameSessionInfo_ApplyLoadedGameData_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("GameSessionInfo");

            MethodInfo? method = type.GetMethod("ApplyLoadedGameData", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
            Assert.Single(method.GetParameters());
            Assert.Equal("MMSaveGameData", method.GetParameters()[0].ParameterType.Name);
        }

        [Fact]
        public void MaintenanceRoom_SaveGameData_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("MaintenanceRoom");

            MethodInfo? method = type.GetMethod("SaveGameData", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("MsgErrorCode", method.ReturnType.Name);
            ParameterInfo[] parameters = method.GetParameters();
            Assert.Equal(3, parameters.Length);
            Assert.Equal("Int32", parameters[0].ParameterType.Name);
            Assert.Equal("List`1", parameters[1].ParameterType.Name);
            Assert.Equal("Boolean", parameters[2].ParameterType.Name);
        }

        [Fact]
        public void NetworkManagerV2_OnRecvPacket_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("NetworkManagerV2");

            MethodInfo? method = type.GetMethod("OnRecvPacket", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
            Assert.Single(method.GetParameters());
            Assert.Equal("IMsg", method.GetParameters()[0].ParameterType.Name);
        }

        [Fact]
        public void VWorld_RegisterAdminProtocol_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VWorld");

            MethodInfo? method = type.GetMethod("RegisterAdminProtocol", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
            Assert.Empty(method.GetParameters());
        }

        [Fact]
        public void VRoomManager_OnRegistPlayer_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VRoomManager");

            MethodInfo? method = type.GetMethod("OnRegistPlayer", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("MsgErrorCode", method.ReturnType.Name);
            ParameterInfo[] parameters = method.GetParameters();
            Assert.Equal(2, parameters.Length);
            Assert.Equal("UInt64", parameters[0].ParameterType.Name);
            Assert.Equal("Boolean", parameters[1].ParameterType.Name);
        }

        private static MimesisMetadataContext CreateContext()
        {
            string managedPath = ManagedAssemblyPaths.Resolve();
            return new MimesisMetadataContext(managedPath);
        }
    }
}
