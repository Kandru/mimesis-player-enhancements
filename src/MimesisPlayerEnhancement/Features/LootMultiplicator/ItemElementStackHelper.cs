using System.Reflection;
using Bifrost.ConstEnum;
using ReluProtocol;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator;

internal static class ItemElementStackHelper
{
    private const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly FieldInfo? ConsumableRemainCountField =
        typeof(ConsumableItemElement).GetField("<RemainCount>k__BackingField", InstanceFlags);

    internal static int GetStackCount(ItemElement element)
    {
        if (element == null)
            return 1;

        if (element is ConsumableItemElement && ConsumableRemainCountField != null)
            return (int)(ConsumableRemainCountField.GetValue(element) ?? 1);

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
        if (element == null)
            return;

        if (element is ConsumableItemElement && ConsumableRemainCountField != null)
        {
            ConsumableRemainCountField.SetValue(element, stackCount);
            return;
        }

        try
        {
            ItemInfo info = element.toItemInfo();
            info.stackCount = stackCount;
        }
        catch
        {
            // Best effort only.
        }
    }

    internal static ItemType GetItemType(ItemElement element)
    {
        if (element == null)
            return ItemType.Miscellany;

        if (element.ItemMasterID > 0)
            return ItemTypeLookup.GetItemType(element.ItemMasterID);

        return ItemTypeLookup.NormalizeItemType(element.ItemType);
    }
}
