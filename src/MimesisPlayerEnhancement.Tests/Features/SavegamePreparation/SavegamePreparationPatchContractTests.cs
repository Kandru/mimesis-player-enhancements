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

        [Fact]
        public void GameSessionInfo_TramUpgradeList_is_readable_List_int()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("GameSessionInfo");

            PropertyInfo? property = type.GetProperty("TramUpgradeList", InstanceMember);

            Assert.NotNull(property);
            Assert.True(property.CanRead);
            Assert.Equal("List`1", property.PropertyType.Name);
            Assert.Equal("Int32", property.PropertyType.GetGenericArguments()[0].Name);
        }

        [Fact]
        public void IVroom_TramUpgradeList_is_readable_List_int()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("IVroom");

            PropertyInfo? property = type.GetProperty("TramUpgradeList", InstanceMember);

            Assert.NotNull(property);
            Assert.True(property.CanRead);
            Assert.Equal("List`1", property.PropertyType.Name);
            Assert.Equal("Int32", property.PropertyType.GetGenericArguments()[0].Name);
        }

        [Fact]
        public void ExcelDataManager_IsTramUpgradeUsable_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("ExcelDataManager");

            MethodInfo? method = type.GetMethods(InstanceMember)
                .FirstOrDefault(candidate =>
                    candidate.Name == "IsTramUpgradeUsable"
                    && candidate.GetParameters().Length == 1
                    && candidate.GetParameters()[0].ParameterType.Name == "Int32");

            Assert.NotNull(method);
            Assert.Equal("Boolean", method.ReturnType.Name);
        }

        [Fact]
        public void ExcelDataManager_GetUsableTramUpgradeMasterIDs_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("ExcelDataManager");

            MethodInfo? method = type.GetMethod("GetUsableTramUpgradeMasterIDs", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("List`1", method.ReturnType.Name);
        }

        [Fact]
        public void TramupgradeData_MasterData_has_id_and_use_tram_upgrade()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("TramupgradeData_MasterData");

            FieldInfo? idField = type.GetField("id", InstanceMember);
            FieldInfo? useField = type.GetField("use_tram_upgrade", InstanceMember);

            Assert.NotNull(idField);
            Assert.Equal("Int32", idField.FieldType.Name);
            Assert.NotNull(useField);
            Assert.Equal("Boolean", useField.FieldType.Name);
        }

        [Fact]
        public void Hub_PersistentData_TramUpgradeIDs_is_List_int()
        {
            using MimesisMetadataContext context = CreateContext();
            Type hubType = context.RequireType("Hub");
            Type? pdataType = hubType.GetNestedType("PersistentData", BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(pdataType);

            FieldInfo? field = pdataType.GetField("TramUpgradeIDs", InstanceMember);
            Assert.NotNull(field);
            Assert.Equal("List`1", field.FieldType.Name);
            Assert.Equal("Int32", field.FieldType.GetGenericArguments()[0].Name);
        }

        private static MimesisMetadataContext CreateContext()
        {
            string managedPath = ManagedAssemblyPaths.Resolve();
            return new MimesisMetadataContext(managedPath);
        }
    }
}
