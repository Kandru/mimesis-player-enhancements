using System.Collections;
using System.Reflection;

namespace MimesisPlayerEnhancement.Features.Economy
{
    internal static class MaintenanceRoomAccess
    {
        private static readonly FieldInfo PriceForItemsField =
            AccessTools.Field(typeof(MaintenanceRoom), "_priceForItems")
            ?? throw new InvalidOperationException("MaintenanceRoom._priceForItems not found");

        private static readonly FieldInfo LevelObjectsField =
            AccessTools.Field(typeof(IVroom), "_levelObjects")
            ?? throw new InvalidOperationException("IVroom._levelObjects not found");

        private static readonly MethodInfo? CurrencySetter =
            AccessTools.PropertySetter(typeof(IVroom), nameof(IVroom.Currency));

        internal static void SetCurrency(IVroom room, int value)
        {
            CurrencySetter?.Invoke(room, [value]);
        }

        internal static Dictionary<int, ShopItemPriceInfo>? GetPriceForItems(MaintenanceRoom room)
        {
            return PriceForItemsField.GetValue(room) as Dictionary<int, ShopItemPriceInfo>;
        }

        internal static void SyncVendingMachines(
            MaintenanceRoom room,
            Dictionary<int, ShopItemPriceInfo> priceForItems)
        {
            if (LevelObjectsField.GetValue(room) is not IDictionary levelObjects)
            {
                return;
            }

            foreach (DictionaryEntry entry in levelObjects)
            {
                if (entry.Value is not InsertLevelObjectInfo insertLevelObjectInfo)
                {
                    continue;
                }

                if (insertLevelObjectInfo.InsertLevelObjectType != InsertLevelObjectType.VendingMachine)
                {
                    continue;
                }

                if (!priceForItems.TryGetValue(insertLevelObjectInfo.OutputItemMasterID, out ShopItemPriceInfo? shopInfo)
                    || shopInfo == null)
                {
                    continue;
                }

                insertLevelObjectInfo.InputAmount = shopInfo.Price;
                insertLevelObjectInfo.DiscountRate = shopInfo.DiscountRate;
            }
        }
    }
}
