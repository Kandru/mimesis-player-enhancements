using System.Reflection;
using Bifrost.ConstEnum;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    internal static class ItemElementStackHelper
    {
        private static readonly MethodInfo? SetConsumableRemainCountMethod =
            AccessTools.PropertySetter(typeof(ConsumableItemElement), "RemainCount");

        internal static int GetStackCount(ItemElement element)
        {
            if (element == null)
            {
                return 1;
            }

            if (element is ConsumableItemElement consumable)
            {
                return consumable.RemainCount > 0 ? consumable.RemainCount : 1;
            }

            try
            {
                ItemInfo info = element.toItemInfo();
                return info.stackCount > 0 ? info.stackCount : 1;
            }
            catch
            {
                return 1;
            }
        }

        internal static void SetStackCount(ItemElement element, int stackCount)
        {
            if (element == null || stackCount <= 0)
            {
                return;
            }

            if (element is ConsumableItemElement consumable && SetConsumableRemainCountMethod != null)
            {
                _ = SetConsumableRemainCountMethod.Invoke(consumable, [stackCount]);
                return;
            }

            // Equipment and miscellany do not support stack counts on map loot.
        }

        internal static ItemType GetItemType(ItemElement element)
        {
            return element == null
                ? ItemType.Miscellany
                : element.ItemMasterID > 0
                ? ItemTypeLookup.GetItemType(element.ItemMasterID)
                : ItemTypeLookup.NormalizeItemType(element.ItemType);
        }
    }
}
