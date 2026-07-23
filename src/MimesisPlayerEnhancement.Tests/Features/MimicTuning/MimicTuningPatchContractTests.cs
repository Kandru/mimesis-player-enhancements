using System.Reflection;
using MimesisPlayerEnhancement.Tests.Infrastructure;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.MimicTuning
{
    public sealed class MimicTuningPatchContractTests
    {
        private const BindingFlags InstanceMember =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private const BindingFlags StaticMember =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        [Fact]
        public void VoiceManager_TrySpawnMimicReply_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VoiceManager");

            MethodInfo? method = type.GetMethod("TrySpawnMimicReply", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Boolean", method.ReturnType.Name);
            Assert.Single(method.GetParameters());
        }

        [Fact]
        public void VoiceManager_SpawnMimicVoiceAfterDelay_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VoiceManager");

            MethodInfo? method = type.GetMethod("SpawnMimicVoiceAfterDelay", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("IEnumerator", method.ReturnType.Name);
        }

        [Fact]
        public void VoiceManager_GetActorByPlayerUID_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VoiceManager");

            MethodInfo? method = type.GetMethod("GetActorByPlayerUID", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("ProtoActor", method.ReturnType.Name);
            Assert.Single(method.GetParameters());
        }

        [Theory]
        [InlineData("_asServer")]
        [InlineData("_lastMimicVoiceTime")]
        public void VoiceManager_voice_tuning_fields_exist(string fieldName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VoiceManager");

            FieldInfo? field = type.GetField(fieldName, InstanceMember);

            Assert.NotNull(field);
        }

        [Fact]
        public void MimicVoiceSpawner_PrepareNearbyMimicReplies_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("MimicVoiceSpawner");

            MethodInfo? method = type.GetMethod("PrepareNearbyMimicReplies", InstanceMember);

            Assert.NotNull(method);
            Assert.Contains("List`1", method.ReturnType.Name, StringComparison.Ordinal);
            Assert.Equal(2, method.GetParameters().Length);
        }

        [Fact]
        public void MimicVoiceSpawner_SpawnPreparedMimicVoice_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("MimicVoiceSpawner");

            MethodInfo? method = type.GetMethod("SpawnPreparedMimicVoice", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Boolean", method.ReturnType.Name);
            Assert.Single(method.GetParameters());
        }

        [Fact]
        public void MimicVoiceSpawner_PickRandomInterval_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("MimicVoiceSpawner");

            MethodInfo? method = type.GetMethod("PickRandomInterval", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Single", method.ReturnType.Name);
        }

        [Fact]
        public void MimicVoiceSpawner_TrySpawnVoiceByContext_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("MimicVoiceSpawner");

            MethodInfo? method = type.GetMethod("TrySpawnVoiceByContext", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Boolean", method.ReturnType.Name);
        }

        [Fact]
        public void SpeechEventAdditionalGameData_PickBestMatch_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SpeechEventAdditionalGameData");

            MethodInfo? method = type.GetMethod("PickBestMatch", StaticMember);

            Assert.NotNull(method);
            Assert.True(method.IsStatic);
            Assert.Equal("Boolean", method.ReturnType.Name);
        }

        [Fact]
        public void PossessionController_HandleStartPossessing_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("PossessionController");

            MethodInfo? method = type.GetMethod("HandleStartPossessing", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("MsgErrorCode", method.ReturnType.Name);
        }

        [Fact]
        public void PossessionController_ClearPossessingStateInternal_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("PossessionController");

            MethodInfo? method = type.GetMethod("ClearPossessingStateInternal", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Theory]
        [InlineData("UpdatePossessionProgressbar")]
        [InlineData("OnEndPossession")]
        [InlineData("SetPossession")]
        [InlineData("PlayVoiceOnActor")]
        public void ProtoActor_possession_and_voice_methods_exist(string methodName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("ProtoActor");

            MethodInfo? method = type.GetMethod(methodName, InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
        }

        [Fact]
        public void ProtoActor_PossessionProgressbarCo_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("ProtoActor");

            MethodInfo? method = type.GetMethod("PossessionProgressbarCo", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("IEnumerator", method.ReturnType.Name);
        }

        [Fact]
        public void UIPrefab_Spectator_Start_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefab_Spectator");

            MethodInfo? method = type.GetMethod("Start", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Fact]
        public void UIPrefab_Spectator_UpdatePossessionCooltime_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefab_Spectator");

            MethodInfo? method = type.GetMethod("UpdatePossessionCooltime", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
            Assert.Single(method.GetParameters());
        }

        [Fact]
        public void AIController_CopyInventory_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("AIController");

            MethodInfo? method = type.GetMethod("CopyInventory", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Boolean", method.ReturnType.Name);
        }

        [Fact]
        public void AIController_self_field_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("AIController");

            FieldInfo? field = type.GetField("_self", InstanceMember);

            Assert.NotNull(field);
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
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Theory]
        [InlineData("C_PossessionDuration")]
        [InlineData("C_PossessionCooltime")]
        public void DataConsts_possession_fields_exist(string fieldName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("DataConsts");

            FieldInfo? field = type.GetField(fieldName, InstanceMember);

            Assert.NotNull(field);
            Assert.Equal("Int32", field.FieldType.Name);
        }

        private static MimesisMetadataContext CreateContext()
        {
            string managedPath = ManagedAssemblyPaths.Resolve();
            return new MimesisMetadataContext(managedPath);
        }
    }
}
