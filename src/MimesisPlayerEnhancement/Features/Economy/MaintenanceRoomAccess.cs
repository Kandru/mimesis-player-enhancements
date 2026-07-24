using System.Collections;
using System.Reflection;

namespace MimesisPlayerEnhancement.Features.Economy
{
    internal static class MaintenanceRoomAccess
    {
        // game@0.3.1 Assembly-CSharp/MaintenanceRoom.cs:L24
        private static readonly FieldInfo PriceForItemsField =
            AccessTools.Field(typeof(MaintenanceRoom), "_priceForItems")
            ?? throw new InvalidOperationException("MaintenanceRoom._priceForItems not found");

        // game@0.3.1 Assembly-CSharp/IVroom.cs:L71
        private static readonly FieldInfo LevelObjectsField =
            AccessTools.Field(typeof(IVroom), "_levelObjects")
            ?? throw new InvalidOperationException("IVroom._levelObjects not found");

        // game@0.3.1 Assembly-CSharp/IVroom.cs:L125
        private static readonly MethodInfo? CurrencySetter =
            AccessTools.PropertySetter(typeof(IVroom), nameof(IVroom.Currency));

        /// <summary>
        /// Sets currency while suppressing startup-money scaling on the Currency setter.
        /// Used for cycle retention restore so a retained amount equal to C_InitialMoney is not re-scaled.
        /// </summary>
        internal static void SetCurrency(IVroom room, int value)
        {
            StartupMoneyLoadGuard.EnterSuppressStartupScale();
            try
            {
                CurrencySetter?.Invoke(room, [value]);
            }
            finally
            {
                StartupMoneyLoadGuard.ExitSuppressStartupScale();
            }
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
