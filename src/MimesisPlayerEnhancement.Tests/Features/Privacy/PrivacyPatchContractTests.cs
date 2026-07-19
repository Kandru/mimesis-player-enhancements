using System.Reflection;
using MimesisPlayerEnhancement.Tests.Infrastructure;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Privacy
{
    public sealed class PrivacyPatchContractTests
    {
        private const BindingFlags InstanceMember =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private const BindingFlags StaticMember =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        [Fact]
        public void APIRequestHandler_Awake_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("APIRequestHandler");

            MethodInfo? method = type.GetMethod("Awake", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Fact]
        public void APIRequestHandler_Update_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("APIRequestHandler");

            MethodInfo? method = type.GetMethod("Update", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Fact]
        public void APIRequestHandler_canSendRequest_field_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("APIRequestHandler");

            FieldInfo? field = type.GetField("_canSendRequest", InstanceMember);

            Assert.NotNull(field);
            Assert.Equal("Boolean", field.FieldType.Name);
        }

        [Fact]
        public void Hub_apihandler_property_returns_APIRequestHandler()
        {
            using MimesisMetadataContext context = CreateContext();
            Type hub = context.RequireType("Hub");
            Type apiRequestHandler = context.RequireType("APIRequestHandler");

            PropertyInfo? property = hub.GetProperty("apihandler", InstanceMember);

            Assert.NotNull(property);
            Assert.Equal(apiRequestHandler.Name, property.PropertyType.Name);
        }

        [Fact]
        public void ReplaySharedData_SetRecordMode_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("ReplaySharedData");

            MethodInfo? method = type.GetMethod("SetRecordMode", StaticMember);

            Assert.NotNull(method);
            Assert.True(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
            Assert.Empty(method.GetParameters());
        }

        [Fact]
        public void ReplayRecorder_UseRecord_property_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("ReplayRecorder");

            PropertyInfo? property = type.GetProperty("UseRecord", InstanceMember);

            Assert.NotNull(property);
            Assert.Equal("Boolean", property.PropertyType.Name);
        }

        [Theory]
        [InlineData("ReadyRecording")]
        [InlineData("ReadyRecordingForDeathMatch")]
        [InlineData("StartRecording")]
        [InlineData("CopyReplayToFeedbackFiles")]
        public void ReplayRecorder_recording_methods_exist(string methodName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("ReplayRecorder");

            MethodInfo? method = type.GetMethod(methodName, InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Theory]
        [InlineData("UploadReplayDataToStorage")]
        [InlineData("UploadReplayFiles")]
        [InlineData("UploadSavedFeedbackFiles")]
        public void ReplayRecorder_async_upload_methods_return_UniTask(string methodName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("ReplayRecorder");

            MethodInfo? method = type.GetMethod(methodName, InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("UniTask", method.ReturnType.Name);
        }

        [Fact]
        public void ReplayRecorder_UploadReplayDataToStorageSync_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("ReplayRecorder");

            MethodInfo? method = type.GetMethod("UploadReplayDataToStorageSync", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
            Assert.Equal(2, method.GetParameters().Length);
        }

        [Fact]
        public void ReplayRecorder_UploadFeedbackReplayFile_overloads_exist()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("ReplayRecorder");

            MethodInfo[] methods = type
                .GetMethods(InstanceMember)
                .Where(method => method.Name == "UploadFeedbackReplayFile")
                .ToArray();

            Assert.Equal(2, methods.Length);
            Assert.All(methods, method => Assert.Equal("UniTask", method.ReturnType.Name));

            MethodInfo? stringOverload = methods.SingleOrDefault(method =>
            {
                ParameterInfo[] parameters = method.GetParameters();
                return parameters.Length == 2
                       && parameters[0].ParameterType.Name == "String"
                       && parameters[1].ParameterType.Name == "String";
            });
            Assert.NotNull(stringOverload);

            MethodInfo? bytesOverload = methods.SingleOrDefault(method =>
            {
                ParameterInfo[] parameters = method.GetParameters();
                return parameters.Length == 2
                       && parameters[0].ParameterType.Name == "Byte[]"
                       && parameters[1].ParameterType.Name == "String";
            });
            Assert.NotNull(bytesOverload);
        }

        [Fact]
        public void ReplayManager_requireFeedbackReplayFile_field_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("ReplayManager");

            FieldInfo? field = type.GetField("_requireFeedbackReplayFile", InstanceMember);

            Assert.NotNull(field);
            Assert.Equal("Boolean", field.FieldType.Name);
        }

        [Fact]
        public void ReplayManager_SetFeedbackUploaded_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("ReplayManager");

            MethodInfo? method = type.GetMethod("SetFeedbackUploaded", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
            Assert.Empty(method.GetParameters());
        }

        [Fact]
        public void KOSManager_Initialize_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("KOSManager");

            MethodInfo? method = type.GetMethod("Initialize", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Fact]
        public void CrashReportHandler_SetUserMetadata_exists_when_module_present()
        {
            string managedPath = ManagedAssemblyPaths.Resolve();
            string modulePath = Path.Combine(managedPath, "UnityEngine.CrashReportHandlerModule.dll");
            if (!File.Exists(modulePath))
            {
                // Bootstrap reference libs omit this Unity module; verified on full game install.
                return;
            }

            List<string> assemblyPaths = [];
            HashSet<string> seenNames = new(StringComparer.OrdinalIgnoreCase);
            foreach (string path in Directory.EnumerateFiles(managedPath, "*.dll"))
            {
                string name = Path.GetFileName(path);
                if (seenNames.Add(name))
                {
                    assemblyPaths.Add(path);
                }
            }

            string runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)
                                  ?? throw new InvalidOperationException("Could not locate runtime assemblies.");
            foreach (string runtimeAssembly in Directory.EnumerateFiles(runtimeDir, "*.dll"))
            {
                string name = Path.GetFileName(runtimeAssembly);
                if (seenNames.Add(name))
                {
                    assemblyPaths.Add(runtimeAssembly);
                }
            }

            var resolver = new PathAssemblyResolver(assemblyPaths);
            using var loadContext = new MetadataLoadContext(resolver, typeof(object).Assembly.GetName().Name);
            Assembly module = loadContext.LoadFromAssemblyPath(modulePath);
            Type? crashHandler = module.GetType(
                "UnityEngine.CrashReportHandler.CrashReportHandler",
                throwOnError: false);

            Assert.NotNull(crashHandler);

            MethodInfo? method = crashHandler.GetMethod(
                "SetUserMetadata",
                StaticMember,
                binder: null,
                types: [typeof(string), typeof(string)],
                modifiers: null);

            Assert.NotNull(method);
            Assert.True(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        private static MimesisMetadataContext CreateContext()
        {
            string managedPath = ManagedAssemblyPaths.Resolve();
            return new MimesisMetadataContext(managedPath);
        }
    }
}
