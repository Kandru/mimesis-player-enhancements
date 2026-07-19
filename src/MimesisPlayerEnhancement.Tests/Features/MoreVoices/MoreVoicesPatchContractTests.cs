using System.Reflection;
using MimesisPlayerEnhancement.Tests.Infrastructure;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.MoreVoices
{
    public sealed class MoreVoicesPatchContractTests
    {
        private const BindingFlags InstanceMember =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private const BindingFlags StaticMember =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        [Theory]
        [InlineData("OnStartClient")]
        [InlineData("OnStopClient")]
        [InlineData("RemoveLowerValueEventsIfExceeded")]
        [InlineData("AddEvent")]
        [InlineData("OnSpeechEventRecorded")]
        [InlineData("GetWarmedUpSpeechEvents")]
        [InlineData("CreateAudioClip")]
        [InlineData("EvaluateValue")]
        [InlineData("ServerRpcBroadcastNewEventWithRemoval")]
        public void SpeechEventArchive_instance_methods_exist(string methodName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SpeechEventArchive");

            MethodInfo? method = type.GetMethod(methodName, InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
        }

        [Fact]
        public void SpeechEventArchive_RemoveLowerValueEventsIfExceeded_returns_removed_id_list()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SpeechEventArchive");

            MethodInfo? method = type.GetMethod("RemoveLowerValueEventsIfExceeded", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("List`1", method.ReturnType.Name);
            Assert.Empty(method.GetParameters());
        }

        [Theory]
        [InlineData("RpcLogic___ObserverRpcPlayOnActor_1543699021")]
        [InlineData("RpcLogic___ObserverRpcPlayOnNonMimicMonster_1543699021")]
        public void SpeechEventArchive_rpc_logic_methods_exist(string methodName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SpeechEventArchive");

            MethodInfo? method = type.GetMethod(methodName, InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
        }

        [Fact]
        public void SpeechEventArchive_WarmedUpCount_property_has_getter()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SpeechEventArchive");

            PropertyInfo? property = type.GetProperty("WarmedUpCount", InstanceMember);

            Assert.NotNull(property);
            Assert.NotNull(property.GetGetMethod(nonPublic: true));
            Assert.Equal("Int32", property.PropertyType.Name);
        }

        [Theory]
        [InlineData("events")]
        [InlineData("maxEvents")]
        [InlineData("maxDeathMatchEvents")]
        [InlineData("maxOutDoorEvents")]
        [InlineData("warmUpDuration")]
        public void SpeechEventArchive_voice_pool_fields_exist(string fieldName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SpeechEventArchive");

            FieldInfo? field = type.GetField(fieldName, InstanceMember);

            Assert.NotNull(field);
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
            ParameterInfo[] parameters = method.GetParameters();
            Assert.Equal(9, parameters.Length);
            Assert.Equal("MimicContext", parameters[0].ParameterType.Name);
            Assert.Equal("List`1", parameters[1].ParameterType.Name);
            Assert.Equal("SpeechEventAdditionalGameData", parameters[2].ParameterType.Name);
            Assert.Equal("Boolean", parameters[3].ParameterType.Name);
            Assert.Equal("Int32", parameters[4].ParameterType.Name);
            Assert.Equal("Single", parameters[5].ParameterType.Name);
            Assert.True(parameters[6].ParameterType.IsByRef);
            Assert.True(parameters[7].ParameterType.IsByRef);
            Assert.True(parameters[8].ParameterType.IsByRef);
        }

        [Fact]
        public void SpeechEventAdditionalGameData_IsOneHandScrap_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SpeechEventAdditionalGameData");

            MethodInfo? method = type.GetMethod("IsOneHandScrap", StaticMember);

            Assert.NotNull(method);
            Assert.True(method.IsStatic);
            Assert.Equal("Boolean", method.ReturnType.Name);
            Assert.Single(method.GetParameters());
            Assert.Equal("Int32", method.GetParameters()[0].ParameterType.Name);
        }

        [Theory]
        [InlineData("SetVoiceMode")]
        [InlineData("EndPossessionToMimic")]
        public void VoiceManager_voice_methods_exist(string methodName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VoiceManager");

            MethodInfo? method = type.GetMethod(methodName, InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
        }

        [Theory]
        [InlineData("speechEventRecorder")]
        [InlineData("voiceMode")]
        public void VoiceManager_voice_properties_exist(string propertyName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VoiceManager");

            PropertyInfo? property = type.GetProperty(propertyName, InstanceMember);

            Assert.NotNull(property);
            Assert.NotNull(property.GetGetMethod(nonPublic: true));
        }

        [Fact]
        public void VRoomManager_EnterWaitingRoom_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VRoomManager");

            MethodInfo? method = type.GetMethod("EnterWaitingRoom", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
        }

        [Fact]
        public void VPlayer_HandleLevelLoadComplete_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VPlayer");

            MethodInfo? method = type.GetMethod("HandleLevelLoadComplete", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
        }

        [Theory]
        [InlineData("GetAllDissonancePlayers")]
        [InlineData("GetAllMimicActors")]
        public void MimicVoiceSpawner_cache_methods_exist(string methodName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("MimicVoiceSpawner");

            MethodInfo? method = type.GetMethod(methodName, InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
        }

        [Fact]
        public void CameraManager_Mode_property_has_getter()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("CameraManager");

            PropertyInfo? property = type.GetProperty("Mode", InstanceMember);

            Assert.NotNull(property);
            Assert.NotNull(property.GetGetMethod(nonPublic: true));
        }

        private static MimesisMetadataContext CreateContext()
        {
            string managedPath = ManagedAssemblyPaths.Resolve();
            return new MimesisMetadataContext(managedPath);
        }
    }
}
