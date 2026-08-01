using System.Reflection;
using MimesisPlayerEnhancement.Tests.Infrastructure;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.DungeonTime
{
    public sealed class DungeonTimePatchContractTests
    {
        private const BindingFlags InstanceMember =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private const BindingFlags StaticMember =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        [Fact]
        public void DungeonRoom_constructor_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("DungeonRoom");

            string[] expectedParameterNames = ["VRoomManager", "Int64", "IVRoomProperty"];

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
        public void DungeonRoom_GetCurrentTime_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("DungeonRoom");

            MethodInfo? method = type.GetMethod("GetCurrentTime", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("TimeSpan", method.ReturnType.Name);
            Assert.Empty(method.GetParameters());
        }

        [Fact]
        public void DungeonRoom_OnUpdate_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type dungeonRoom = context.RequireType("DungeonRoom");

            MethodInfo? method = dungeonRoom.GetMethod("OnUpdate", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
            ParameterInfo[] parameters = method.GetParameters();
            Assert.Single(parameters);
            Assert.Equal("Int64", parameters[0].ParameterType.Name);
        }

        [Fact]
        public void VWorldUtil_ConvertTimeToSeconds_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VWorldUtil");

            MethodInfo? method = type.GetMethods(StaticMember)
                .FirstOrDefault(candidate =>
                    candidate.Name == "ConvertTimeToSeconds"
                    && candidate.GetParameters().Length == 1
                    && candidate.GetParameters()[0].ParameterType.Name == "String");

            Assert.NotNull(method);
            Assert.True(method.IsStatic);
            Assert.Equal("Int64", method.ReturnType.Name);
            ParameterInfo[] parameters = method.GetParameters();
            Assert.Single(parameters);
            Assert.Equal("String", parameters[0].ParameterType.Name);
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
        [InlineData("_elapsedTime")]
        public void DungeonRoom_session_time_fields_are_Int64(string fieldName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type dungeonRoom = context.RequireType("DungeonRoom");

            FieldInfo? field = dungeonRoom.GetField(fieldName, InstanceMember);

            Assert.NotNull(field);
            Assert.Equal("Int64", field.FieldType.Name);
        }

        [Theory]
        [InlineData("_dungeonMasterInfo")]
        [InlineData("_state")]
        public void DungeonRoom_clock_access_fields_exist(string fieldName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type dungeonRoom = context.RequireType("DungeonRoom");

            FieldInfo? field = dungeonRoom.GetField(fieldName, InstanceMember);

            Assert.NotNull(field);
        }

        private static MimesisMetadataContext CreateContext()
        {
            string managedPath = ManagedAssemblyPaths.Resolve();
            return new MimesisMetadataContext(managedPath);
        }
    }
}
