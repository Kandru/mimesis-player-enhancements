using System.Reflection;
using MimesisPlayerEnhancement.Tests.Infrastructure;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.JoinAnytime
{
    public sealed class JoinAnytimePatchContractTests
    {
        private const BindingFlags InstanceMember =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private const BindingFlags StaticMember =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        [Fact]
        public void GameSessionInfo_CanEnterSession_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("GameSessionInfo");

            MethodInfo? method = type.GetMethod("CanEnterSession", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Boolean", method.ReturnType.Name);
            Assert.Empty(method.GetParameters());
        }

        [Fact]
        public void SessionContext_Login_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SessionContext");

            MethodInfo? method = type.GetMethod("Login", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Fact]
        public void SessionManager_Remove_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SessionManager");

            MethodInfo? method = type.GetMethod("Remove", InstanceMember);

            Assert.NotNull(method);
            ParameterInfo[] parameters = method.GetParameters();
            Assert.Equal(2, parameters.Length);
            Assert.Equal("Int64", parameters[0].ParameterType.Name);
            Assert.Equal("DisconnectReason", parameters[1].ParameterType.Name);
        }

        [Fact]
        public void VPlayer_HandleLevelLoadComplete_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VPlayer");

            MethodInfo? method = type.GetMethod("HandleLevelLoadComplete", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
            Assert.Single(method.GetParameters());
            Assert.Equal("Int32", method.GetParameters()[0].ParameterType.Name);
        }

        [Fact]
        public void VPlayer_constructor_exists_with_expected_signature()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VPlayer");

            string[] expectedParameterNames =
            [
                "SessionContext",
                "Int32",
                "Int32",
                "Boolean",
                "String",
                "String",
                "PosWithRot",
                "Boolean",
                "IVroom",
                "ReasonOfSpawn",
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

        [Theory]
        [InlineData("PendMoveToDungeon")]
        [InlineData("BroadcastRoomReady")]
        [InlineData("InitWaitingRoom")]
        [InlineData("OnDungeonFinished")]
        [InlineData("EnterWaitingRoom")]
        [InlineData("EnterMaintenenceRoom")]
        public void VRoomManager_join_anytime_methods_exist(string methodName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VRoomManager");

            MethodInfo? method = type.GetMethod(methodName, InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
        }

        [Fact]
        public void DungeonRoom_OnAllMemberEntered_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("DungeonRoom");

            MethodInfo? method = type.GetMethod("OnAllMemberEntered", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
            Assert.Empty(method.GetParameters());
        }

        [Fact]
        public void IVroom_OnAllMemberEntered_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("IVroom");

            MethodInfo? method = type.GetMethod("OnAllMemberEntered", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
            Assert.Empty(method.GetParameters());
        }

        [Fact]
        public void IVroom_OnUpdate_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("IVroom");

            MethodInfo? method = type.GetMethod("OnUpdate", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
            ParameterInfo[] parameters = method.GetParameters();
            Assert.Single(parameters);
            Assert.Equal("Int64", parameters[0].ParameterType.Name);
        }

        [Theory]
        [InlineData("_startNotified")]
        [InlineData("_levelLoadCompleteActorIDs")]
        [InlineData("_vPlayerDict")]
        public void IVroom_loading_handshake_fields_exist(string fieldName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("IVroom");

            FieldInfo? field = type.GetField(fieldName, InstanceMember);

            Assert.NotNull(field);
        }

        [Fact]
        public void IVroom_GetMemberCount_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("IVroom");

            MethodInfo? method = type.GetMethod("GetMemberCount", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Int32", method.ReturnType.Name);
            Assert.Empty(method.GetParameters());
        }

        [Fact]
        public void VPlayer_LevelLoadCompleted_property_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VPlayer");

            PropertyInfo? property = type.GetProperty("LevelLoadCompleted", InstanceMember);

            Assert.NotNull(property);
            Assert.Equal("Boolean", property.PropertyType.Name);
        }

        [Fact]
        public void MaintenanceScene_TryInitHostMaintenenceRoom_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("MaintenanceScene");

            MethodInfo? method = type.GetMethod("TryInitHostMaintenenceRoom", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Theory]
        [InlineData("InTramWaitingScene")]
        [InlineData("GamePlayScene")]
        public void Host_scene_Start_exists(string typeName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType(typeName);

            MethodInfo? method = type.GetMethod("Start", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Fact]
        public void IVroom_RunEventActionInternal_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("IVroom");

            MethodInfo? method = type.GetMethod("RunEventActionInternal", InstanceMember);

            Assert.NotNull(method);
        }

        [Fact]
        public void IVroom_HandleLevelObject_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("IVroom");

            MethodInfo? method = type.GetMethod("HandleLevelObject", InstanceMember);

            Assert.NotNull(method);
        }

        [Fact]
        public void NewTramLeverLevelObject_IsTriggerable_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("NewTramLeverLevelObject");

            MethodInfo? method = type.GetMethods(InstanceMember)
                .FirstOrDefault(candidate =>
                    candidate.Name == "IsTriggerable"
                    && candidate.GetParameters().Length == 2
                    && candidate.GetParameters()[0].ParameterType.Name == "ProtoActor"
                    && candidate.GetParameters()[1].ParameterType.Name == "Int32");

            Assert.NotNull(method);
            Assert.Equal("Boolean", method.ReturnType.Name);
        }

        [Fact]
        public void NewTramLeverLevelObject_OnChangeLevelObjectStateSig_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("NewTramLeverLevelObject");

            MethodInfo? method = type.GetMethod("OnChangeLevelObjectStateSig", InstanceMember);

            Assert.NotNull(method);
        }

        [Theory]
        [InlineData("CreateLobby")]
        [InlineData("SetLobbyPublic")]
        [InlineData("UpdateLobbyData")]
        [InlineData("SetPresenceInLobby")]
        [InlineData("SetPresencePlaying")]
        [InlineData("UpdatePlayerGroupSize")]
        public void SteamInviteDispatcher_lobby_methods_exist(string methodName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SteamInviteDispatcher");

            MethodInfo? method = type.GetMethod(methodName, InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
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
        public void SteamInviteDispatcher_SetLobbyPublicCoroutine_MoveNext_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SteamInviteDispatcher");

            MethodInfo? moveNext = FindEnumeratorMoveNext(type, "SetLobbyPublicCoroutine");

            Assert.NotNull(moveNext);
            Assert.Equal("Boolean", moveNext.ReturnType.Name);
        }

        [Fact]
        public void SteamInviteDispatcher_SetLobbyPublicCoroutine_state_machine_has_isPublic_field()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SteamInviteDispatcher");

            FieldInfo? field = FindIteratorStateMachineField(type, "SetLobbyPublicCoroutine", "isPublic", "<>3__isPublic");

            Assert.NotNull(field);
            Assert.Equal("Boolean", field.FieldType.Name);
        }

        [Theory]
        [InlineData("OnEnable")]
        [InlineData("Start")]
        [InlineData("SetPublicRoomName")]
        public void UIPrefab_InGameMenu_methods_exist(string methodName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefab_InGameMenu");

            MethodInfo? method = type.GetMethod(methodName, InstanceMember);

            Assert.NotNull(method);
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

        [Fact]
        public void InTramWaitingScene_startGameSig_field_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("InTramWaitingScene");

            FieldInfo? field = type.GetField("startGameSig", InstanceMember);

            Assert.NotNull(field);
        }

        [Fact]
        public void SteamInviteDispatcher_lobbyName_field_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SteamInviteDispatcher");

            FieldInfo? field = type.GetField("lobbyName", InstanceMember);

            Assert.NotNull(field);
            Assert.Equal("String", field.FieldType.Name);
        }

        [Fact]
        public void Hub_GetL10NText_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("Hub");

            MethodInfo? method = type.GetMethods(StaticMember)
                .FirstOrDefault(candidate =>
                    candidate.Name == "GetL10NText"
                    && candidate.GetParameters().Length >= 1
                    && candidate.GetParameters()[0].ParameterType.Name == "String");

            Assert.NotNull(method);
            Assert.Equal("String", method.ReturnType.Name);
        }

        [Fact]
        public void UIPrefab_InGameMenu_room_name_ui_members_exist()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefab_InGameMenu");

            FieldInfo? inputFieldId = type.GetField("UEID_InputFieldRoomName", InstanceMember | StaticMember);
            PropertyInfo? changeButton = type.GetProperty("UE_ChangeRoomNameButton", InstanceMember);

            Assert.NotNull(inputFieldId);
            Assert.NotNull(changeButton);
        }

        [Fact]
        public void UIPrefab_PublicRoomList_room_list_wiring_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefab_PublicRoomList");

            FieldInfo? roomListData = type.GetField("roomListData", InstanceMember);
            MethodInfo? setRoomListUi = type.GetMethod("SetRoomListUI", InstanceMember);

            Assert.NotNull(roomListData);
            Assert.NotNull(setRoomListUi);
        }

        [Fact]
        public void UiPrefab_RoomCard_UE_PlayerCount_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UiPrefab_RoomCard");

            PropertyInfo? property = type.GetProperty("UE_PlayerCount", InstanceMember);

            Assert.NotNull(property);
        }

        [Theory]
        [InlineData("_vrooms")]
        [InlineData("_commandExecutor")]
        public void VRoomManager_reflection_fields_exist(string fieldName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VRoomManager");

            FieldInfo? field = type.GetField(fieldName, InstanceMember);

            Assert.NotNull(field);
        }

        [Fact]
        public void VWaitingRoom_player_start_spawn_points_field_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VWaitingRoom");

            FieldInfo? field = type.GetField("_playerStartSpawnPoints", InstanceMember);

            Assert.NotNull(field);
        }

        [Fact]
        public void IVroom_ResumeRoom_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("IVroom");

            MethodInfo? method = type.GetMethod("ResumeRoom", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
            Assert.Empty(method.GetParameters());
        }

        private static MethodInfo? FindEnumeratorMoveNext(Type declaringType, string methodName)
        {
            MethodInfo? iterator = declaringType.GetMethods(InstanceMember | BindingFlags.Public)
                .FirstOrDefault(candidate =>
                    candidate.Name == methodName
                    && candidate.GetParameters().Length == 1
                    && candidate.GetParameters()[0].ParameterType.Name == "Boolean");
            if (iterator == null)
            {
                return null;
            }

            string prefix = "<" + methodName + ">";
            foreach (Type nested in declaringType.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (!nested.Name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    continue;
                }

                MethodInfo? moveNext = nested.GetMethod("MoveNext", InstanceMember);
                if (moveNext != null)
                {
                    return moveNext;
                }
            }

            return null;
        }

        private static FieldInfo? FindIteratorStateMachineField(
            Type declaringType,
            string methodName,
            params string[] candidateFieldNames)
        {
            string prefix = "<" + methodName + ">";
            foreach (Type nested in declaringType.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (!nested.Name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (string fieldName in candidateFieldNames)
                {
                    FieldInfo? field = nested.GetField(fieldName, InstanceMember);
                    if (field != null)
                    {
                        return field;
                    }
                }
            }

            return null;
        }

        private static MimesisMetadataContext CreateContext()
        {
            string managedPath = ManagedAssemblyPaths.Resolve();
            return new MimesisMetadataContext(managedPath);
        }
    }
}
