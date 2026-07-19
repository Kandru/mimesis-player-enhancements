using System.Reflection;
using MimesisPlayerEnhancement.Tests.Infrastructure;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.DungeonTime
{
    public sealed class DungeonTimePatchContractTests
    {
        private const BindingFlags InstanceMember =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [Fact]
        public void DungeonRoom_OnAllMemberEntered_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type dungeonRoom = context.RequireType("DungeonRoom");

            MethodInfo? method = dungeonRoom.GetMethod("OnAllMemberEntered", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
            Assert.Empty(method.GetParameters());
        }

        [Fact]
        public void DungeonRoom_GetMemberCount_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type dungeonRoom = context.RequireType("DungeonRoom");

            MethodInfo? method = dungeonRoom.GetMethod("GetMemberCount", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Int32", method.ReturnType.Name);
            Assert.Empty(method.GetParameters());
        }

        [Theory]
        [InlineData("_sessionEndTime")]
        [InlineData("_currentTime")]
        public void DungeonRoom_session_time_fields_are_Int64(string fieldName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type dungeonRoom = context.RequireType("DungeonRoom");

            FieldInfo? field = dungeonRoom.GetField(fieldName, InstanceMember);

            Assert.NotNull(field);
            Assert.Equal("Int64", field.FieldType.Name);
        }

        private static MimesisMetadataContext CreateContext()
        {
            string managedPath = ManagedAssemblyPaths.Resolve();
            return new MimesisMetadataContext(managedPath);
        }
    }
}
