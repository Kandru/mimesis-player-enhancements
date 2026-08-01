using System.Reflection;
using MimesisPlayerEnhancement.Tests.Infrastructure;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.SavegamePreparation
{
    public sealed class SavegamePreparationPatchContractTests
    {
        private const BindingFlags InstanceMember =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [Fact]
        public void MainMenu_CreateNewGameInSlot_overloads_exist()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("MainMenu");

            MethodInfo[] overloads = type.GetMethods(InstanceMember)
                .Where(candidate =>
                    candidate.Name == "CreateNewGameInSlot"
                    && candidate.GetParameters().Length == 2
                    && candidate.GetParameters()[1].ParameterType.Name == "Int32")
                .ToArray();

            Assert.Contains(overloads, method => method.GetParameters()[0].ParameterType.Name == "UIPrefab_NewTram");
            Assert.Contains(overloads, method => method.GetParameters()[0].ParameterType.Name == "UIPrefab_LoadTram");
        }

        [Fact]
        public void IVroom_ApplyBaseGameSessionInfo_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("IVroom");

            MethodInfo? method = type.GetMethod("ApplyBaseGameSessionInfo", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("GameSessionInfo", method.GetParameters()[0].ParameterType.Name);
        }

        [Fact]
        public void GameSessionInfo_StageCount_property_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("GameSessionInfo");

            PropertyInfo? property = type.GetProperty("StageCount", InstanceMember);

            Assert.NotNull(property);
            Assert.True(property.CanRead);
        }

        [Fact]
        public void MaintenanceRoom_SaveGameData_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("MaintenanceRoom");

            MethodInfo? method = type.GetMethod("SaveGameData", InstanceMember);

            Assert.NotNull(method);
        }

        private static MimesisMetadataContext CreateContext()
        {
            string managedPath = ManagedAssemblyPaths.Resolve();
            return new MimesisMetadataContext(managedPath);
        }
    }
}
