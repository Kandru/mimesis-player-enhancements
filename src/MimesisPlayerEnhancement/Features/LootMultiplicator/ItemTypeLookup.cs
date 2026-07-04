using System.Collections.Immutable;
using System.Reflection;
using Bifrost.ConstEnum;
using Bifrost.Cooked;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    internal static class ItemTypeLookup
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly PropertyInfo? HubDatamanProperty =
            typeof(Hub).GetProperty("dataman", InstanceFlags);

        internal static bool TryGetItem(int masterId, out ItemMasterInfo info)
        {
            info = null!;
            if (masterId <= 0 || Hub.s == null)
            {
                return false;
            }

            if (HubDatamanProperty?.GetValue(Hub.s) is not DataManager dataman)
            {
                return false;
            }

            ItemMasterInfo? found = dataman.ExcelDataManager.GetItemInfo(masterId);
            if (found == null)
            {
                return false;
            }

            info = found;
            return true;
        }

        internal static ItemType GetItemType(int masterId)
        {
            return !TryGetItem(masterId, out ItemMasterInfo info) ? ItemType.Miscellany : NormalizeItemType(info.ItemType);
        }

        internal static ItemType GetDominantItemType(ImmutableDictionary<int, (int, int)>? candidates)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return ItemType.Miscellany;
            }

            int bestMasterId = 0;
            int bestWeight = -1;

            foreach (KeyValuePair<int, (int, int)> entry in candidates)
            {
                int weight = entry.Value.Item1;
                if (weight > bestWeight)
                {
                    bestWeight = weight;
                    bestMasterId = entry.Key;
                }
            }

            return bestMasterId > 0 ? GetItemType(bestMasterId) : ItemType.Miscellany;
        }

        internal static string GetDisplayName(int masterId, ItemMasterInfo? info = null)
        {
            return info == null && !TryGetItem(masterId, out info)
                ? masterId.ToString()
                : string.IsNullOrWhiteSpace(info.Name) ? masterId.ToString() : info.Name;
        }

        internal static ItemType NormalizeItemType(ItemType itemType)
        {
            return itemType.Equals(ItemType.Consumable) ? ItemType.Consumable
            : itemType.Equals(ItemType.Equipment) ? ItemType.Equipment
            : ItemType.Miscellany;
        }
    }
}
