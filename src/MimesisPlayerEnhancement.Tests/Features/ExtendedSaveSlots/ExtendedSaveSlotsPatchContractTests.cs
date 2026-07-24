using System.Reflection;
using MimesisPlayerEnhancement.Tests.Infrastructure;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.ExtendedSaveSlots
{
    public sealed class ExtendedSaveSlotsPatchContractTests
    {
        private const BindingFlags InstanceMember =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private const BindingFlags StaticMember =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

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
        public void MainMenu_Start_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("MainMenu");

            MethodInfo? method = type.GetMethod("Start", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
        }

        [Fact]
        public void MainMenu_ui_mainmenu_field_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("MainMenu");

            FieldInfo? field = type.GetField("ui_mainmenu", InstanceMember);

            Assert.NotNull(field);
        }

        [Fact]
        public void MainMenu_TryDeleteSaveGameData_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("MainMenu");

            MethodInfo? method = type.GetMethod("TryDeleteSaveGameData", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Boolean", method.ReturnType.Name);
            Assert.Single(method.GetParameters());
            Assert.Equal("Int32", method.GetParameters()[0].ParameterType.Name);
        }

        [Fact]
        public void MainMenu_TryLoadSaveAndCreateRoom_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("MainMenu");

            MethodInfo? method = type.GetMethods(InstanceMember)
                .FirstOrDefault(candidate =>
                    candidate.Name == "TryLoadSaveAndCreateRoom"
                    && candidate.GetParameters().Length == 2
                    && candidate.GetParameters()[0].ParameterType.Name == "UIPrefab_LoadTram"
                    && candidate.GetParameters()[1].ParameterType.Name == "Int32");

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Fact]
        public void MainMenu_HandleNewGameSlotSelection_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("MainMenu");

            MethodInfo? method = type.GetMethods(InstanceMember)
                .FirstOrDefault(candidate =>
                    candidate.Name == "HandleNewGameSlotSelection"
                    && candidate.GetParameters().Length == 3
                    && candidate.GetParameters()[0].ParameterType.Name == "UIPrefab_NewTram"
                    && candidate.GetParameters()[1].ParameterType.Name == "UIPrefab_NewTramPopUp"
                    && candidate.GetParameters()[2].ParameterType.Name == "Int32");

            Assert.NotNull(method);
            Assert.Equal("Void", method.ReturnType.Name);
        }

        [Fact]
        public void UIManager_Update_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIManager");

            MethodInfo? method = type.GetMethod("Update", InstanceMember);

            Assert.NotNull(method);
        }

        [Fact]
        public void UIPrefab_LoadTram_GetLoadedSaveData_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefab_LoadTram");

            MethodInfo? method = type.GetMethod("GetLoadedSaveData", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("MMSaveGameData", method.ReturnType.Name);
            Assert.Single(method.GetParameters());
            Assert.Equal("Int32", method.GetParameters()[0].ParameterType.Name);
        }

        [Fact]
        public void UIPrefab_LoadTram_IsSlotVersionCompatible_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefab_LoadTram");

            MethodInfo? method = type.GetMethod("IsSlotVersionCompatible", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("Boolean", method.ReturnType.Name);
            Assert.Single(method.GetParameters());
            Assert.Equal("Int32", method.GetParameters()[0].ParameterType.Name);
        }

        [Fact]
        public void UIPrefab_MainMenu_OnEnable_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefab_MainMenu");

            MethodInfo? method = type.GetMethod("OnEnable", InstanceMember);

            Assert.NotNull(method);
        }

        [Fact]
        public void UIPrefab_MainMenu_OnHostButton_property_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefab_MainMenu");

            PropertyInfo? property = type.GetProperty("OnHostButton", InstanceMember);

            Assert.NotNull(property);
            Assert.NotNull(property.GetSetMethod(nonPublic: true));
        }

        [Fact]
        public void UIPrefabScript_OnButtonClick_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefabScript");

            MethodInfo? method = type.GetMethods(InstanceMember)
                .FirstOrDefault(candidate =>
                    candidate.Name == "OnButtonClick"
                    && candidate.GetParameters().Length == 1
                    && candidate.GetParameters()[0].ParameterType.Name == "String");

            Assert.NotNull(method);
        }

        [Theory]
        [InlineData("dictElements")]
        [InlineData("onButtonClick")]
        public void UIPrefabScript_wiring_fields_exist(string fieldName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefabScript");

            FieldInfo? field = type.GetField(fieldName, InstanceMember);

            Assert.NotNull(field);
        }

        [Fact]
        public void UIPrefabScript_SetOnButtonClick_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("UIPrefabScript");

            MethodInfo? method = type.GetMethods(InstanceMember)
                .FirstOrDefault(candidate =>
                    candidate.Name == "SetOnButtonClick"
                    && candidate.GetParameters().Length == 2
                    && candidate.GetParameters()[0].ParameterType.Name == "String");

            Assert.NotNull(method);
        }

        [Fact]
        public void PlatformMgr_Load_generic_method_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("PlatformMgr");

            MethodInfo? method = type.GetMethods(InstanceMember | BindingFlags.Public)
                .FirstOrDefault(candidate =>
                    candidate.Name == "Load"
                    && candidate.IsGenericMethodDefinition
                    && candidate.GetParameters().Length == 1
                    && candidate.GetParameters()[0].ParameterType.Name == "String");

            Assert.NotNull(method);
        }

        [Fact]
        public void Mimic_InputSystem_InputAction_type_exists()
        {
            using MimesisMetadataContext context = CreateContext();

            Type? inputAction = context.FindType("Mimic.InputSystem.InputAction");

            Assert.NotNull(inputAction);
            Assert.Equal("InputAction", inputAction.Name);
        }

        [Fact]
        public void UIElementMarker_button_and_name_members_exist()
        {
            using MimesisMetadataContext context = CreateContext();
            Type marker = context.RequireType("UIElementMarker");

            FieldInfo? buttonField = marker.GetField("asButton", InstanceMember);
            PropertyInfo? nameProperty = marker.GetProperty("name", InstanceMember);

            Assert.NotNull(buttonField);
            Assert.NotNull(nameProperty);
        }

        [Fact]
        public void Hub_inputman_field_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type hub = context.RequireType("Hub");

            FieldInfo? field = hub.GetField("inputman", InstanceMember)
                ?? hub.GetField("<inputman>k__BackingField", InstanceMember);

            Assert.NotNull(field);
        }

        [Fact]
        public void Input_manager_wasPressedThisFrame_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type hub = context.RequireType("Hub");

            FieldInfo? field = hub.GetField("inputman", InstanceMember)
                ?? hub.GetField("<inputman>k__BackingField", InstanceMember);
            Assert.NotNull(field);

            Type inputManagerType = field.FieldType;
            Type inputAction = context.RequireType("Mimic.InputSystem.InputAction");

            MethodInfo? method = inputManagerType.GetMethods(InstanceMember)
                .FirstOrDefault(candidate =>
                    candidate.Name == "wasPressedThisFrame"
                    && candidate.GetParameters().Length == 1
                    && candidate.GetParameters()[0].ParameterType.Name == inputAction.Name);

            Assert.NotNull(method);
            Assert.Equal("Boolean", method.ReturnType.Name);
        }

        private static MimesisMetadataContext CreateContext()
        {
            string managedPath = ManagedAssemblyPaths.Resolve();
            return new MimesisMetadataContext(managedPath);
        }
    }
}
