using System.Collections.Immutable;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    internal static class ItemTypeLookup
    {
        internal static bool TryGetItem(int masterId, out ItemMasterInfo info)
        {
            info = null!;
            if (masterId <= 0)
            {
                return false;
            }

            ItemMasterInfo? found = HubGameDataAccess.Excel?.GetItemInfo(masterId);
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
