using System.Reflection;
using MimesisPlayerEnhancement.Tests.Infrastructure;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Economy
{
    public sealed class EconomyPatchContractTests
    {
        private const BindingFlags InstanceMember =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [Fact]
        public void IVroom_Currency_property_has_setter()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("IVroom");

            PropertyInfo? property = type.GetProperty("Currency", InstanceMember);

            Assert.NotNull(property);
            Assert.NotNull(property.GetSetMethod(nonPublic: true));
            Assert.Equal("Int32", property.PropertyType.Name);
        }

        [Fact]
        public void VRoomManager_InitMaintenenceRoom_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("VRoomManager");

            MethodInfo? method = type.GetMethod("InitMaintenenceRoom", InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
            ParameterInfo[] parameters = method.GetParameters();
            Assert.Equal(3, parameters.Length);
            Assert.Equal("String", parameters[0].ParameterType.Name);
            Assert.Equal("Int32", parameters[1].ParameterType.Name);
            Assert.Equal("String", parameters[2].ParameterType.Name);
        }

        [Fact]
        public void ItemElement_FinalPrice_property_has_getter()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("ItemElement");

            PropertyInfo? property = type.GetProperty("FinalPrice", InstanceMember);

            Assert.NotNull(property);
            Assert.NotNull(property.GetGetMethod(nonPublic: true));
            Assert.Equal("Int32", property.PropertyType.Name);
        }

        [Fact]
        public void ConsumableItemElement_toItemInfo_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("ConsumableItemElement");

            MethodInfo? method = type.GetMethod("toItemInfo", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("ItemInfo", method.ReturnType.Name);
        }

        [Fact]
        public void MiscellanyItemElement_toItemInfo_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("MiscellanyItemElement");

            MethodInfo? method = type.GetMethod("toItemInfo", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("ItemInfo", method.ReturnType.Name);
        }

        [Fact]
        public void IVroom_GetNewItemElement_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("IVroom");

            MethodInfo? method = type.GetMethod("GetNewItemElement", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("ItemElement", method.ReturnType.Name);
        }

        [Fact]
        public void InventoryItem_ReinforceCost_property_has_getter()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("InventoryItem");

            PropertyInfo? property = type.GetProperty("ReinforceCost", InstanceMember);

            Assert.NotNull(property);
            Assert.NotNull(property.GetGetMethod(nonPublic: true));
            Assert.Equal("Int32", property.PropertyType.Name);
        }

        [Theory]
        [InlineData("TryGetShopItemPrice")]
        [InlineData("InitShopItems")]
        [InlineData("ApplyLoadedGameData")]
        [InlineData("OnEnterChannel")]
        [InlineData("OnRequestStartSession")]
        public void MaintenanceRoom_shop_and_session_methods_exist(string methodName)
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("MaintenanceRoom");

            MethodInfo? method = type.GetMethod(methodName, InstanceMember);

            Assert.NotNull(method);
            Assert.False(method.IsStatic);
        }

        [Fact]
        public void VPlayer_HandleReinforceItem_exists_with_MaintenanceRoom_local()
        {
            using MimesisMetadataContext context = CreateContext();
            Type vPlayer = context.RequireType("VPlayer");
            Type maintenanceRoom = context.RequireType("MaintenanceRoom");

            MethodInfo? method = vPlayer.GetMethod("HandleReinforceItem", InstanceMember);

            Assert.NotNull(method);
            Assert.Equal("MsgErrorCode", method.ReturnType.Name);
            Assert.Single(method.GetParameters());
            Assert.Equal("Int32", method.GetParameters()[0].ParameterType.Name);

            MethodBody? body = method.GetMethodBody();
            Assert.NotNull(body);
            Assert.Contains(
                body.LocalVariables,
                local => local.LocalType.Name == maintenanceRoom.Name);
        }

        [Fact]
        public void MaintenanceRoom_priceForItems_field_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("MaintenanceRoom");

            FieldInfo? field = type.GetField("_priceForItems", InstanceMember);

            Assert.NotNull(field);
        }

        [Fact]
        public void IVroom_levelObjects_field_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("IVroom");

            FieldInfo? field = type.GetField("_levelObjects", InstanceMember);

            Assert.NotNull(field);
        }

        [Fact]
        public void ItemEquipmentInfo_UpgradeCost_field_exists()
        {
            using MimesisMetadataContext context = CreateContext();
            Type type = context.RequireType("ItemEquipmentInfo");

            FieldInfo? field = type.GetField("UpgradeCost", InstanceMember);

            Assert.NotNull(field);
            Assert.Equal("Int32", field.FieldType.Name);
        }

        private static MimesisMetadataContext CreateContext()
        {
            string managedPath = ManagedAssemblyPaths.Resolve();
            return new MimesisMetadataContext(managedPath);
        }
    }
}
