using System.Reflection;
using MimesisPlayerEnhancement.Tests.Infrastructure;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.MorePlayers
{
    public sealed class MorePlayersPatchContractTests
    {
        private const BindingFlags InstanceMember =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private const BindingFlags StaticMember =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        [Fact]
        public void IVroom_CanEnterChannel_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("IVroom");

            MethodInfo? method = type.GetMethod("CanEnterChannel", InstanceMember);

            Assert.NotNull(method);
        }

        [Fact]
        public void IVroom_constructor_exists_with_expected_signature()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("IVroom");

            string[] expectedParameterNames =
            [
                "VRoomManager",
                "Int64",
                "IVRoomProperty",
                "OnCreateRoomDelegate",
            ];

            ConstructorInfo? ctor = type
                .GetConstructors(InstanceMember | BindingFlags.Public)
                .FirstOrDefault(candidate =>
                {
                    ParameterInfo[] parameters = candidate.GetParameters();
                    if (parameters.Length != expectedParameterNames.Length)
                    {
                        return false;
                    }

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if (!string.Equals(parameters[i].ParameterType.Name, expectedParameterNames[i], StringComparison.Ordinal))
                        {
                            return false;
                        }
                    }

                    return true;
                });

            Assert.NotNull(ctor);
        }

        [Fact]
        public void IVroom_vPlayerDict_field_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("IVroom");

            FieldInfo? field = type.GetField("_vPlayerDict", InstanceMember);

            Assert.NotNull(field);
        }

        [Fact]
        public void GameSessionInfo_AddPlayerSteamID_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("GameSessionInfo");

            MethodInfo? method = type.GetMethod("AddPlayerSteamID", InstanceMember);

            Assert.NotNull(method);
        }

        [Theory]
        [InlineData("RefreshTargetCurrency")]
        [InlineData("ClampTargetCurrencyToMin")]
        public void GameSessionInfo_round_goal_methods_exist(string methodName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("GameSessionInfo");

            MethodInfo? method = type.GetMethod(methodName, InstanceMember);

            Assert.NotNull(method);
            Assert.Single(method.GetParameters());
            Assert.Equal("Int32", method.GetParameters()[0].ParameterType.Name);
        }

        [Fact]
        public void GameSessionInfo_targetCurrency_field_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("GameSessionInfo");

            FieldInfo? field = type.GetField("_targetCurrency", InstanceMember);

            Assert.NotNull(field);
        }

        [Fact]
        public void C_MaxPlayerCount_field_exists_in_game_assembly()
        {
            bool found = typeof(VRoomManager).Assembly.GetTypes()
                .Any(type =>
                {
                    try
                    {
                        return type.GetFields(InstanceMember | StaticMember)
                            .Any(field => string.Equals(field.Name, "C_MaxPlayerCount", StringComparison.Ordinal));
                    }
                    catch (ReflectionTypeLoadException)
                    {
                        return false;
                    }
                });

            Assert.True(found);
        }

        [Theory]
        [InlineData("EnterWaitingRoom")]
        [InlineData("EnterMaintenenceRoom")]
        public void VRoomManager_enter_room_methods_exist(string methodName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VRoomManager");

            MethodInfo? method = type.GetMethod(methodName, InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
        }

        [Fact]
        public void VRoomManager_has_enter_room_closure_methods()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VRoomManager");

            bool found = type.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public)
                .SelectMany(nested => nested.GetMethods(InstanceMember | BindingFlags.DeclaredOnly))
                .Any(method =>
                    method.Name.Contains("EnterWaitingRoom", StringComparison.Ordinal)
                    || method.Name.Contains("EnterMaintenenceRoom", StringComparison.Ordinal));

            Assert.True(found);
        }

        [Fact]
        public void SteamInviteDispatcher_CreateLobby_has_bool_bool_overload()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SteamInviteDispatcher");

            MethodInfo? method = type.GetMethods(InstanceMember)
                .FirstOrDefault(candidate =>
                    candidate.Name == "CreateLobby"
                    && candidate.GetParameters().Length == 2
                    && candidate.GetParameters()[0].ParameterType.Name == "Boolean"
                    && candidate.GetParameters()[1].ParameterType.Name == "Boolean");

            Assert.NotNull(method);
        }

        [Fact]
        public void SteamInviteDispatcher_UpdatePlayerGroupSize_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SteamInviteDispatcher");

            MethodInfo? method = type.GetMethod("UpdatePlayerGroupSize", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
        }

        [Fact]
        public void UIPrefab_PublicRoomList_SetRoomList_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefab_PublicRoomList");

            MethodInfo? method = type.GetMethod("SetRoomList", InstanceMember);

            Assert.NotNull(method);
        }

        [Fact]
        public void UiPrefab_RoomCard_SetRoomData_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UiPrefab_RoomCard");

            MethodInfo? method = type.GetMethod("SetRoomData", InstanceMember);

            Assert.NotNull(method);
        }

        [Theory]
        [InlineData("SetRemoteVolumeController_v2")]
        [InlineData("SetPingImage")]
        [InlineData("OnEnable")]
        public void UIPrefab_InGameMenu_more_players_methods_exist(string methodName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefab_InGameMenu");

            MethodInfo? method = type.GetMethod(methodName, InstanceMember);

            Assert.NotNull(method);
        }

        [Theory]
        [InlineData("OnClickSpeakButton")]
        [InlineData("OnClickPlayerInfoButton")]
        [InlineData("OpenKickPlayerPopup")]
        public void UIPrefab_InGameMenu_private_player_button_methods_exist(string methodName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefab_InGameMenu");

            MethodInfo? method = type.GetMethod(methodName, InstanceMember);

            Assert.NotNull(method);
        }

        [Fact]
        public void UIPrefab_InGameMenu_tempVolumeList_field_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefab_InGameMenu");

            FieldInfo? field = type.GetField("tempVolumeList", InstanceMember);

            Assert.NotNull(field);
        }

        [Fact]
        public void UIPrefab_InGameMenu_PlayerUIElement_nickNameText_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type menuType = context.RequireType("UIPrefab_InGameMenu");
            Type playerUiElement = RequireNestedType(menuType, "PlayerUIElement");

            FieldInfo? field = playerUiElement.GetField("nickNameText", InstanceMember);

            Assert.NotNull(field);
        }

        [Fact]
        public void VActorDict_m_MaxCount_field_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = RequireVActorDictType(context);

            FieldInfo? field = type.GetField("m_MaxCount", InstanceMember);

            Assert.NotNull(field);
            Assert.Equal("Int32", field.FieldType.Name);
        }

        [Fact]
        public void FishySteamworks_ServerSocket_GetMaximumClients_exists()
        {
            Type socketType = typeof(FishySteamworks.Server.ServerSocket);

            MethodInfo? method = socketType.GetMethod("GetMaximumClients", InstanceMember | BindingFlags.Public);

            Assert.NotNull(method);
            Assert.Equal("Int32", method.ReturnType.Name);
        }

        [Fact]
        public void FishySteamworks_ServerSocket_SetMaximumClients_exists()
        {
            Type socketType = typeof(FishySteamworks.Server.ServerSocket);

            MethodInfo? method = socketType.GetMethod("SetMaximumClients", InstanceMember | BindingFlags.Public);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
            Assert.Single(method.GetParameters());
            Assert.Equal("Int32", method.GetParameters()[0].ParameterType.Name);
        }

        [Fact]
        public void FishySteamworks_ServerSocket_parameterless_constructor_exists()
        {
            Type socketType = typeof(FishySteamworks.Server.ServerSocket);

            ConstructorInfo? ctor = socketType.GetConstructor(Type.EmptyTypes);

            Assert.NotNull(ctor);
        }

        private static Type RequireVActorDictType(MimesisMetadataContext context)
        {
            Type? type = context.AssemblyCSharp.GetTypes()
                .FirstOrDefault(candidate => candidate.Name == "VActorDict`2");

            return type ?? throw new InvalidOperationException("Type not found: VActorDict`2");
        }

        private static Type RequireNestedType(Type parent, string nestedName)
        {
            Type? nested = parent.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public)
                .FirstOrDefault(candidate => string.Equals(candidate.Name, nestedName, StringComparison.Ordinal));

            return nested ?? throw new InvalidOperationException($"Nested type not found: {parent.Name}+{nestedName}");
        }

        private static MimesisMetadataContext CreateContext()
        {
            string managedPath = ManagedAssemblyPaths.Resolve();
            return new MimesisMetadataContext(managedPath);
        }
    }
}
