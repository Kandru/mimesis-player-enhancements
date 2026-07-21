using System.Reflection;
using MimesisPlayerEnhancement.Tests.Infrastructure;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Persistence
{
    public sealed class PersistencePatchContractTests
    {
        private const BindingFlags InstanceMember =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private const BindingFlags StaticMember =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        [Fact]
        public void PlatformMgr_Delete_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("PlatformMgr");

            MethodInfo? method = type.GetMethod("Delete", InstanceMember | StaticMember);

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
            Assert.Single(method.GetParameters());
            Assert.Equal("String", method.GetParameters()[0].ParameterType.Name);
        }

        [Fact]
        public void MMSaveGameData_CheckSaveSlotID_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("MMSaveGameData");

            MethodInfo? method = type.GetMethod("CheckSaveSlotID", StaticMember);

            Assert.NotNull(method);
            Assert.True(method.IsStatic);
            ParameterInfo[] parameters = method.GetParameters();
            Assert.Equal(2, parameters.Length);
            Assert.Equal("Int32", parameters[0].ParameterType.Name);
            Assert.Equal("Boolean", parameters[1].ParameterType.Name);
        }

        [Fact]
        public void SpeechEventArchive_OnStartClient_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SpeechEventArchive");

            MethodInfo? method = type.GetMethod("OnStartClient", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Fact]
        public void SpeechEventArchive_OnStopClient_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SpeechEventArchive");

            MethodInfo? method = type.GetMethod("OnStopClient", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Fact]
        public void VoiceManager_GetRandomOtherSpeechEventArchive_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type voiceManager = context.RequireType("VoiceManager");
            Type speechEventArchive = context.RequireType("SpeechEventArchive");

            MethodInfo? method = voiceManager.GetMethod("GetRandomOtherSpeechEventArchive", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal(speechEventArchive.Name, method.ReturnType.Name);
        }

        [Theory]
        [InlineData("RecordedTime")]
        [InlineData("LastPlayedTime")]
        public void SpeechEvent_timing_fields_exist(string fieldName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SpeechEvent");

            FieldInfo? field = type.GetField(fieldName, InstanceMember);

            Assert.NotNull(field);
        }

        [Fact]
        public void SpeechEvent_CompressedAudioData_field_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SpeechEvent");

            FieldInfo? field = type.GetField("CompressedAudioData", InstanceMember);

            Assert.NotNull(field);
        }

        [Fact]
        public void SpeechEventArchive_events_field_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SpeechEventArchive");

            FieldInfo? field = type.GetField("events", InstanceMember);

            Assert.NotNull(field);
        }

        [Fact]
        public void SpeechEventArchive_WarmedUpCount_property_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SpeechEventArchive");

            PropertyInfo? property = type.GetProperty("WarmedUpCount", InstanceMember);

            Assert.NotNull(property);
            Assert.Equal("Int32", property.PropertyType.Name);
        }

        [Fact]
        public void SpeechEventArchive_ServerRpcRequestInitialSync_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SpeechEventArchive");

            MethodInfo? method = type.GetMethod("ServerRpcRequestInitialSync", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Fact]
        public void ReplayableSndEvent_GetDataFromSndEvent_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type replayable = RequireReplayType(context, "ReplayableSndEvent");
            Type speechEvent = context.RequireType("SpeechEvent");

            MethodInfo? method = replayable.GetMethod("GetDataFromSndEvent", StaticMember);

            Assert.NotNull(method);
            Assert.True(method.IsStatic);
            Assert.Single(method.GetParameters());
            Assert.Equal(speechEvent.Name, method.GetParameters()[0].ParameterType.Name);
        }

        [Fact]
        public void SerializerUtil_Deserialize_generic_method_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type serializerUtil = RequireReplayType(context, "SerializerUtil");

            MethodInfo? method = serializerUtil.GetMethods(StaticMember)
                .FirstOrDefault(candidate =>
                    candidate.Name == "Deserialize"
                    && candidate.IsGenericMethodDefinition
                    && candidate.GetParameters().Length == 1
                    && candidate.GetParameters()[0].ParameterType.Name == "Byte[]");

            Assert.NotNull(method);
        }

        private static Type RequireReplayType(MimesisMetadataContext context, string typeName)
        {
            Type? type = context.FindType(typeName);
            return type ?? throw new InvalidOperationException($"Type not found: {typeName}");
        }

        private static MimesisMetadataContext CreateContext()
        {
            string managedPath = ManagedAssemblyPaths.Resolve();
            return new MimesisMetadataContext(managedPath);
        }
    }
}
