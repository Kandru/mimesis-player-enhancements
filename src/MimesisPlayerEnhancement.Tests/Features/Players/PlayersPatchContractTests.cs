using System.Reflection;
using MimesisPlayerEnhancement.Tests.Infrastructure;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Players
{
    public sealed class PlayersPatchContractTests
    {
        private const BindingFlags InstanceMember =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [Theory]
        [InlineData("PlayerUID", "Int64")]
        [InlineData("IsLocal", "Boolean")]
        [InlineData("PlayerId", "String")]
        public void SpeechEventArchive_identity_members_exist(string memberName, string expectedTypeName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SpeechEventArchive");

            PropertyInfo? property = type.GetProperty(memberName, InstanceMember);
            Assert.NotNull(property);
            Assert.Equal(expectedTypeName, property.PropertyType.Name);
        }

        [Fact]
        public void SpeechEventArchive_events_field_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SpeechEventArchive");

            FieldInfo? field = type.GetField("events", InstanceMember);

            Assert.NotNull(field);
            Assert.Equal("SyncList`1", field.FieldType.Name);
        }

        [Fact]
        public void SpeechEventArchive_Player_property_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SpeechEventArchive");

            PropertyInfo? property = type.GetProperty("Player", InstanceMember);

            Assert.NotNull(property);
            Assert.Equal("FishNetDissonancePlayer", property.PropertyType.Name);
        }

        [Fact]
        public void SessionContext_GetPlayerUID_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SessionContext");

            MethodInfo? method = type.GetMethod("GetPlayerUID", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Int64", method.ReturnType.Name);
            Assert.Empty(method.GetParameters());
        }

        [Fact]
        public void SessionContext_SteamID_property_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SessionContext");

            PropertyInfo? property = type.GetProperty("SteamID", InstanceMember);

            Assert.NotNull(property);
            Assert.Equal("UInt64", property.PropertyType.Name);
        }

        [Fact]
        public void SessionContext_Session_property_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SessionContext");

            PropertyInfo? property = type.GetProperty("Session", InstanceMember);

            Assert.NotNull(property);
            Assert.Equal("ISession", property.PropertyType.Name);
        }

        [Fact]
        public void SessionContext_IsSDRLink_property_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("SessionContext");

            PropertyInfo? property = type.GetProperty("IsSDRLink", InstanceMember);

            Assert.NotNull(property);
            Assert.Equal("Boolean", property.PropertyType.Name);
        }

        [Theory]
        [InlineData("main")]
        [InlineData("actorUIDToSteamID")]
        public void Hub_PersistentData_fields_exist(string fieldName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type hub = context.RequireType("Hub");
            Type persistentData = RequireNestedType(hub, "PersistentData");

            FieldInfo? field = persistentData.GetField(fieldName, InstanceMember);

            Assert.NotNull(field);
        }

        [Fact]
        public void Hub_PersistentData_main_has_GetProtoActorMap()
        {
            using MimesisMetadataContext context = CreateContext();
            Type hub = context.RequireType("Hub");
            Type persistentData = RequireNestedType(hub, "PersistentData");

            FieldInfo? mainField = persistentData.GetField("main", InstanceMember);
            Assert.NotNull(mainField);

            MethodInfo? method = mainField.FieldType.GetMethod("GetProtoActorMap", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Dictionary`2", method.ReturnType.Name);
            Assert.Empty(method.GetParameters());
        }

        [Fact]
        public void ISession_GetRemoteEndPoint_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type sessionContext = context.RequireType("SessionContext");

            PropertyInfo? sessionProperty = sessionContext.GetProperty("Session", InstanceMember);
            Assert.NotNull(sessionProperty);

            MethodInfo? method = sessionProperty.PropertyType.GetMethod("GetRemoteEndPoint", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("IPEndPoint", method.ReturnType.Name);
            Assert.Empty(method.GetParameters());
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
