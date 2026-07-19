using System.Reflection;
using MimesisPlayerEnhancement.Tests.Infrastructure;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.DungeonTime
{
    public sealed class DungeonRoomPatchContractTests
    {
        [Fact]
        public void DungeonRoom_type_exists_in_managed_assembly()
        {
            using MimesisMetadataContext context = CreateContext();

            Type dungeonRoom = context.RequireType("DungeonRoom");

            Assert.Equal("DungeonRoom", dungeonRoom.Name);
        }

        [Fact]
        public void DungeonRoom_OnAllMemberEntered_method_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type dungeonRoom = context.RequireType("DungeonRoom");

            MethodInfo? method = dungeonRoom.GetMethod(
                "OnAllMemberEntered",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
        }

        [Theory]
        [InlineData("_sessionEndTime")]
        [InlineData("_currentTime")]
        public void DungeonRoom_session_time_fields_exist(string fieldName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type dungeonRoom = context.RequireType("DungeonRoom");

            FieldInfo? field = dungeonRoom.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(field);
        }

        private static MimesisMetadataContext CreateContext()
        {
            string managedPath = ManagedAssemblyPaths.Resolve();
            return new MimesisMetadataContext(managedPath);
        }
    }
}
