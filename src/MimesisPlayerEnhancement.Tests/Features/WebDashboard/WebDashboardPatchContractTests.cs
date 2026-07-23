using System.Reflection;
using MimesisPlayerEnhancement.Tests.Infrastructure;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.WebDashboard
{
    public sealed class WebDashboardPatchContractTests
    {
        private const BindingFlags InstanceMember =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [Fact]
        public void GameMainBase_OnPacket_NetworkGradeSig_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("GameMainBase");
            Type sigType = context.RequireType("NetworkGradeSig");

            MethodInfo? method = FindMethodWithSingleParameter(type, "OnPacket", sigType.Name);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Fact]
        public void NetworkGradeSig_grades_member_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("NetworkGradeSig");

            MemberInfo? member = type.GetField("grades", InstanceMember) as MemberInfo
                ?? type.GetProperty("grades", InstanceMember);

            Assert.NotNull(member);
        }

        [Theory]
        [InlineData("OnPlayerDeath", "ProtoActor", "ActorDeathInfo&")]
        [InlineData("OnPlayerRevive", "ProtoActor")]
        public void GameMainBase_lifecycle_methods_exist(
            string methodName,
            params string[] expectedParameterTypeNames)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("GameMainBase");

            MethodInfo? method = type.GetMethods(InstanceMember)
                .FirstOrDefault(candidate =>
                {
                    if (!string.Equals(candidate.Name, methodName, StringComparison.Ordinal))
                    {
                        return false;
                    }

                    ParameterInfo[] parameters = candidate.GetParameters();
                    if (parameters.Length != expectedParameterTypeNames.Length)
                    {
                        return false;
                    }

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if (!string.Equals(parameters[i].ParameterType.Name, expectedParameterTypeNames[i], StringComparison.Ordinal))
                        {
                            return false;
                        }
                    }

                    return true;
                });

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Theory]
        [InlineData("GetSteamAvatar")]
        [InlineData("SetRemoteVolumeController_v2")]
        public void UIPrefab_InGameMenu_methods_exist(string methodName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefab_InGameMenu");

            MethodInfo? method = type.GetMethod(methodName, InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
        }

        [Theory]
        [InlineData("SetLobbyName")]
        [InlineData("LeaveLobby")]
        public void SteamInviteDispatcher_methods_exist(string methodName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SteamInviteDispatcher");

            MethodInfo? method = type.GetMethod(methodName, InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Fact]
        public void DungeonGenerator_ChangeStatus_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("DungeonGenerator");

            MethodInfo? method = type.GetMethod("ChangeStatus", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Fact]
        public void RuntimeDungeon_BuildDungeonInfo_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("RuntimeDungeon");

            MethodInfo? method = type.GetMethod("BuildDungeonInfo", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
        }

        [Fact]
        public void DungeonRoom_InitSpawn_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("DungeonRoom");

            MethodInfo? method = type.GetMethod("InitSpawn", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Fact]
        public void StatManager_AddMutableStat_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("StatManager");

            MethodInfo? method = type.GetMethod("AddMutableStat", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Boolean", method.ReturnType.Name);
        }

        [Fact]
        public void StatManager_SetMutableStat_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("StatManager");

            MethodInfo? method = type.GetMethod("SetMutableStat", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Fact]
        public void StatManager_self_field_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("StatManager");

            FieldInfo? field = type.GetField("_self", InstanceMember);

            Assert.NotNull(field);
            Assert.Equal("VCreature", field.FieldType.Name);
        }

        [Fact]
        public void VCreature_ForcedDying_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VCreature");

            MethodInfo? method = type.GetMethod("ForcedDying", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Fact]
        public void IVroom_ValidPosition_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("IVroom");

            MethodInfo? method = type.GetMethod("ValidPosition", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Boolean", method.ReturnType.Name);
        }

        [Fact]
        public void NetworkManagerV2_HandleGlobalPacket_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("NetworkManagerV2");

            MethodInfo? method = type.GetMethod("HandleGlobalPacket", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Boolean", method.ReturnType.Name);
        }

        [Fact]
        public void ProtoActor_UpdateControl_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("ProtoActor");

            MethodInfo? method = type.GetMethod("UpdateControl", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Theory]
        [InlineData("PendMoveToDungeon", "Void")]
        [InlineData("PendMoveToWaitingRoom", "Void")]
        [InlineData("PendMoveToMaintenance", "Boolean")]
        [InlineData("PendMoveToDeathMatch", "Void")]
        public void VRoomManager_pend_move_methods_exist(string methodName, string expectedReturnTypeName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VRoomManager");

            MethodInfo? method = type.GetMethod(methodName, InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal(expectedReturnTypeName, method.ReturnType.Name);
        }

        [Fact]
        public void InTramWaitingScene_Start_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("InTramWaitingScene");

            MethodInfo? method = type.GetMethod("Start", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Fact]
        public void MaintenanceScene_TryInitHostMaintenenceRoom_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("MaintenanceScene");

            MethodInfo? method = type.GetMethod("TryInitHostMaintenenceRoom", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Theory]
        [InlineData("OnEnterDungeon")]
        [InlineData("OnEndDungeon")]
        public void CameraManager_dungeon_lifecycle_methods_exist(string methodName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("CameraManager");

            MethodInfo? method = type.GetMethod(methodName, InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Theory]
        [InlineData("CheckFallDamage")]
        [InlineData("DirectMoveStart")]
        [InlineData("DirectMoveStop")]
        public void MovementController_move_methods_exist(string methodName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("MovementController");

            MethodInfo? method = type.GetMethod(methodName, InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
        }

        [Fact]
        public void MovementController_creature_field_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("MovementController");

            FieldInfo? field = type.GetField("_creature", InstanceMember);

            Assert.NotNull(field);
            Assert.Equal("VCreature", field.FieldType.Name);
        }

        [Theory]
        [InlineData("ValidateMoveSpped")]
        [InlineData("StartMove")]
        [InlineData("OnMoveStopReq")]
        public void MovementController_DirectMoveContext_methods_exist(string methodName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type parent = context.RequireType("MovementController");
            Type nested = RequireNestedType(parent, "DirectMoveContext");

            MethodInfo? method = nested.GetMethod(methodName, InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
        }

        [Fact]
        public void MovementController_IMoveContext_owner_field_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type parent = context.RequireType("MovementController");
            Type nested = RequireNestedType(parent, "IMoveContext");

            FieldInfo? field = nested.GetField("m_Owner", InstanceMember);

            Assert.NotNull(field);
            Assert.Equal("VCreature", field.FieldType.Name);
        }

        [Theory]
        [InlineData("_dungeonSpaceGroup")]
        public void DungeonRoom_space_group_fields_exist(string fieldName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("DungeonRoom");

            FieldInfo? field = type.GetField(fieldName, InstanceMember);

            Assert.NotNull(field);
        }

        [Fact]
        public void DungeonRoom_legacy_space_group_field_is_optional()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("DungeonRoom");

            FieldInfo? field = type.GetField("_spaceGroup", InstanceMember);

            // Removed in 0.3.1; older game builds may still expose it.
            _ = field;
        }

        [Theory]
        [InlineData("m_tiles")]
        [InlineData("m_adjacency")]
        public void VSpaceTileGroup_fields_exist(string fieldName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VSpaceTileGroup");

            FieldInfo? field = type.GetField(fieldName, InstanceMember);

            Assert.NotNull(field);
        }

        [Fact]
        public void VSpaceGridGroup_size_field_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VSpaceGridGroup");

            FieldInfo? field = type.GetField("m_VSpaceSize", InstanceMember);

            Assert.NotNull(field);
        }

        [Fact]
        public void GamePlayScene_runtimeDungeon_field_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("GamePlayScene");

            FieldInfo? field = type.GetField("runtimeDungeon", InstanceMember);

            Assert.NotNull(field);
            Assert.Equal("RuntimeDungeon", field.FieldType.Name);
        }

        [Theory]
        [InlineData("GetAllIDToTile")]
        [InlineData("GetAllAdjacency")]
        public void RuntimeDungeon_graph_methods_exist(string methodName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("RuntimeDungeon");

            MethodInfo? method = type.GetMethod(methodName, InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
        }

        [Fact]
        public void IVroom_SpawnLootingObject_nine_parameter_overload_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("IVroom");

            string[] expectedParameterNames =
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

            MethodInfo? method = type.GetMethods(InstanceMember)
                .FirstOrDefault(candidate =>
                {
                    if (!string.Equals(candidate.Name, "SpawnLootingObject", StringComparison.Ordinal))
                    {
                        return false;
                    }

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

            Assert.NotNull(method);
            Assert.Equal("Int32", method.ReturnType.Name);
        }

        [Theory]
        [InlineData("camRoot")]
        [InlineData("_cc")]
        [InlineData("falling")]
        [InlineData("puppet")]
        public void ProtoActor_no_clip_fields_exist(string fieldName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("ProtoActor");

            FieldInfo? field = type.GetField(fieldName, InstanceMember);

            Assert.NotNull(field);
        }

        [Theory]
        [InlineData("RotateByInput")]
        [InlineData("GetMovementInput")]
        [InlineData("CaculateSpeed")]
        [InlineData("EnableCCWithSafeSpawnIfAvatar")]
        public void ProtoActor_no_clip_methods_exist(string methodName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("ProtoActor");

            MethodInfo? method = type.GetMethod(methodName, InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
        }

        [Fact]
        public void ProtoActor_ProcessSprintKey_bool_overload_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("ProtoActor");

            MethodInfo? method = type.GetMethods(InstanceMember)
                .FirstOrDefault(candidate =>
                    string.Equals(candidate.Name, "ProcessSprintKey", StringComparison.Ordinal)
                    && candidate.GetParameters().Length == 1
                    && string.Equals(candidate.GetParameters()[0].ParameterType.Name, "Boolean", StringComparison.Ordinal));

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
        }

        [Theory]
        [InlineData("walkSpeed")]
        [InlineData("runSpeed")]
        public void ProtoActor_speed_properties_exist(string propertyName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("ProtoActor");

            PropertyInfo? property = type.GetProperty(propertyName, InstanceMember);

            Assert.NotNull(property);
            Assert.Equal("Single", property.PropertyType.Name);
        }

        [Theory]
        [InlineData("inputman", "InputManager")]
        [InlineData("console", "DebugConsole")]
        public void Hub_no_clip_properties_exist(string propertyName, string expectedTypeName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("Hub");

            PropertyInfo? property = type.GetProperty(propertyName, InstanceMember);

            Assert.NotNull(property);
            Assert.Equal(expectedTypeName, property.PropertyType.Name);
        }

        private static MethodInfo? FindMethodWithSingleParameter(Type type, string methodName, string parameterTypeName)
        {
            return type.GetMethods(InstanceMember)
                .FirstOrDefault(candidate =>
                    string.Equals(candidate.Name, methodName, StringComparison.Ordinal)
                    && candidate.GetParameters().Length == 1
                    && string.Equals(candidate.GetParameters()[0].ParameterType.Name, parameterTypeName, StringComparison.Ordinal));
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
