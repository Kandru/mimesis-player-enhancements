using System.Reflection;
using MimesisPlayerEnhancement.Tests.Infrastructure;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Weather
{
    public sealed class WeatherPatchContractTests
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
            Type type = context.RequireType("DungeonRoom");

            MethodInfo? method = type.GetMethod("OnAllMemberEntered", InstanceMember);

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
            Type type = context.RequireType("DungeonRoom");

            MethodInfo? method = type.GetMethod("OnUpdate", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
            ParameterInfo[] parameters = method.GetParameters();
            Assert.Single(parameters);
            Assert.Equal("Int64", parameters[0].ParameterType.Name);
        }

        [Fact]
        public void DungeonRoom_SetDungeonState_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type dungeonRoom = context.RequireType("DungeonRoom");
            Type dungeonState = context.RequireType("DungeonState");

            MethodInfo? method = dungeonRoom.GetMethod("SetDungeonState", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            Assert.Equal("Void", method.ReturnType.Name);
            ParameterInfo[] parameters = method.GetParameters();
            Assert.Single(parameters);
            Assert.Equal(dungeonState.Name, parameters[0].ParameterType.Name);
        }

        [Fact]
        public void DungeonWeather_constructor_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("DungeonWeather");

            string[] expectedParameterNames = ["Int32", "Int32", "Int32"];

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

        [Theory]
        [InlineData("_weather")]
        [InlineData("_prevSyncTime")]
        [InlineData("_elapsedTime")]
        [InlineData("_dungeonMasterInfo")]
        [InlineData("_state")]
        public void DungeonRoom_weather_access_fields_exist(string fieldName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("DungeonRoom");

            FieldInfo? field = type.GetField(fieldName, InstanceMember);

            Assert.NotNull(field);
        }

        [Theory]
        [InlineData("_weatherByHour", "List`1")]
        [InlineData("_weatherForecastByHour", "List`1")]
        [InlineData("_isRandomOccured", "Boolean")]
        public void DungeonWeather_schedule_fields_exist(string fieldName, string expectedTypeName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("DungeonWeather");

            FieldInfo? field = type.GetField(fieldName, InstanceMember);

            Assert.NotNull(field);
            Assert.Equal(expectedTypeName, field.FieldType.Name);
        }

        private static MimesisMetadataContext CreateContext()
        {
            string managedPath = ManagedAssemblyPaths.Resolve();
            return new MimesisMetadataContext(managedPath);
        }
    }
}
